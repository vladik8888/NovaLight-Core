using Mono.Cecil;
using NovaLight.Core.Extensions;
using NovaLight.Core.Service;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using IServiceProvider = NovaLight.Core.Service.IServiceProvider;

namespace NovaLight.Core
{
    public class AssemblyContext
    {
        private readonly static List<AssemblyContext> _assemblyContexts = [];
        public static AssemblyContext[] AllAssemblyContexts => [.. _assemblyContexts];

        internal AssemblyLoadContext AssemblyLoadContext { get; }
        public Assembly[] Assemblies => [.. AssemblyLoadContext.Assemblies];
        public bool IsActive { get; private set; }
        public Logger Logger { get; }
        private readonly List<ServiceController> _serviceControllers = [];
        public IServiceProvider ServiceProvider { get; }

        public static AssemblyContext Current
        {
            get
            {
                StackTrace stackTrace = new(fNeedFileInfo: false);

                foreach (StackFrame frame in stackTrace.GetFrames())
                {
                    MethodBase? method = frame.GetMethod();
                    if (method == null)
                        continue;

                    Assembly assembly = method.Module.Assembly;
                    AssemblyLoadContext? assemblyLoadContext = AssemblyLoadContext.GetLoadContext(assembly);
                    if (assemblyLoadContext == AssemblyLoadContext.Default)
                        continue;

                    AssemblyContext? assemblyContext = AllAssemblyContexts.ToList()
                        .FirstOrDefault(x => x.AssemblyLoadContext.Equals(assemblyLoadContext));

                    if (assemblyContext == null)
                        continue;
                    return assemblyContext;
                }

                throw new InvalidOperationException();
            }
        }

        public AssemblyContext(Logger? logger = null, IServiceProvider? serviceProvider = null)
        {
            AssemblyLoadContext = new($"AssemblyContext", isCollectible: true);

            AssemblyLoadContext.Unloading += OnUnloading;
            _assemblyContexts.Add(this);

            Logger = logger ?? new();
            ServiceProvider = serviceProvider ?? new BaseServiceProvider();
        }

        public Assembly LoadAssemblyFromFile(FileInfo file)
        {
            using FileStream fileStream = file.OpenRead();
            AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(fileStream);

            string missingDependency = "";
            foreach (AssemblyNameReference reference in assemblyDefinition.MainModule.AssemblyReferences)
            {
                bool isLoadedInCurrentContext = Assemblies.Any(x => x.GetName().Name == reference.Name);
                bool isLoadedInDefaultContext = AssemblyLoadContext.Default.Assemblies.Any(x => x.GetName().Name == reference.Name);

                if (!isLoadedInCurrentContext && !isLoadedInDefaultContext)
                    missingDependency += $"{reference.Name} ";
            }
            if (!string.IsNullOrEmpty(missingDependency))
            {
                Logger.Log($"Missing dependency: {missingDependency}");
                throw new InvalidOperationException($"Missing dependency: {missingDependency}");
            }

            string serviceControllerFullName = typeof(ServiceController).FullName!;
            List<TypeDefinition> serviceTypes = [.. assemblyDefinition.MainModule.Types
                .Where(x => !x.IsAbstract && !x.IsInterface)
                .Where(x =>
                {
                    TypeReference? current = x.BaseType;
                    while (current != null)
                    {
                        if (current.FullName == serviceControllerFullName)
                            return true;
                        current = current.Resolve()?.BaseType;
                    }
                    return false;
                })];
            List<TypeDefinition> loadedServiceTypes = [];

            int attemps = 1000;
            while (serviceTypes.Count > 0)
            {
                bool progress = false;
                for (int i = 0; i < serviceTypes.Count; i++)
                {
                    TypeDefinition serviceType = serviceTypes[i];

                    MethodDefinition constructor = serviceType.Methods
                        .Where(x => x.IsConstructor && x.IsPublic)
                        .OrderByDescending(x => x.Parameters.Count)
                        .First();

                    foreach (ParameterDefinition parameter in constructor.Parameters)
                    {
                        TypeDefinition parameterTypeDefinition = parameter.ParameterType.Resolve();
                        if (loadedServiceTypes.Any(parameterTypeDefinition.InheritsFrom))
                            continue;

                        Type? parameterType = ResolveTypeByName(parameterTypeDefinition.FullName)!;
                        object? service = ServiceProvider.GetService(parameterType);
                        if (service == null) goto Skip;
                    }

                    serviceTypes.RemoveAt(i);
                    loadedServiceTypes.Add(serviceType);
                    progress = true;
                    i--;

                    Skip:;
                }

                if (!progress)
                {
                    attemps--;
                    if (attemps <= 0)
                        throw new InvalidOperationException("Missing dependency in DI-services.");
                }
            }

            fileStream.Seek(0, SeekOrigin.Begin);
            Assembly assembly = AssemblyLoadContext.LoadFromStream(fileStream);
            Logger.Log($"The assembly {assembly.GetName().Name} has been loaded.");
            foreach (Type serviceType in loadedServiceTypes.Select(x => ResolveTypeByName(x.FullName)!))
            {
                ConstructorInfo constructor = serviceType.GetConstructors()
                    .OrderByDescending(x => x.GetParameters().Length)
                    .First();

                ParameterInfo[] parameters = constructor.GetParameters();
                object?[] parameterInstances = [.. parameters.Select(x => ServiceProvider.GetService(x.ParameterType))];

                ServiceController service = (ServiceController)Activator.CreateInstance(serviceType, parameterInstances)!;
                _serviceControllers.Add(service);
                ServiceProvider.RegisterService(serviceType, service);
                Logger.Log($"The service {service.GetType().Name} has been registered.");

                if (IsActive) RunService(service);
            }
            return assembly;
        }

        private Type? ResolveTypeByName(string fullName)
        {
            Type? type = Assemblies
                .SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.FullName == fullName);
            if (type != null) return type;

            type = AssemblyLoadContext.Default.Assemblies
                .SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.FullName == fullName);
            if (type != null) return type;

            return null;
        }

        public void RunService(ServiceController service)
        {
            try
            {
                service.OnServiceRun();
                Logger.Log($"The service {service.GetType().Name} has been started.");
            }
            catch (Exception exception)
            {
                Logger.Log($"Error occurred while starting the service {service.GetType().Name}: {exception.Message}.");
            }
        }
        public void StopService(ServiceController service)
        {
            try
            {
                service.OnServiceStop();
                Logger.Log($"The service {service.GetType().Name} has been stopped.");
            }
            catch (Exception exception)
            {
                Logger.Log($"Error occurred while stopping the service {service.GetType().Name}: {exception.Message}.");
            }
        }

        public void Run()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsActive) return;

            IsActive = true;
            foreach (ServiceController service in _serviceControllers)
                RunService(service);

            Logger.Log("AssemblyContext has been started.");
        }
        public void Stop()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!IsActive) return;

            IsActive = false;
            ServiceController[] reversedServices = [.. _serviceControllers.AsEnumerable().Reverse()];
            foreach (ServiceController service in reversedServices)
                StopService(service);

            Logger.Log("AssemblyContext has been stopped.");
        }

        private void OnUnloading(AssemblyLoadContext assemblyLoadContext)
        {
            AssemblyLoadContext.Unloading -= OnUnloading;
            _assemblyContexts.Remove(this);
        }

        private bool _disposed = false;
        public void Unload()
        {
            if (_disposed) return;
            IsActive = false;
            _disposed = true;

            ServiceController[] reversedServices = [.. _serviceControllers.AsEnumerable().Reverse()];
            foreach (ServiceController service in reversedServices)
            {
                try
                {
                    service.OnServiceUnload();
                    ServiceProvider.UnregisterService(service);
                    Logger.Log($"The service {service.GetType().Name} has been unloaded.");
                }
                catch (Exception exception)
                {
                    Logger.Log($"Error occurred while unloading the service {service.GetType().Name}: {exception.Message}.");
                }
            }

            Logger.Log("AssemblyContext has been unloaded.");

            AssemblyLoadContext.Unload();
        }
    }
}