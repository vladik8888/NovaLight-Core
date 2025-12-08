using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace NovaLight.Core
{
    public class AssemblyContext : IDisposable
    {
        internal AssemblyLoadContext AssemblyLoadContext { get; }
        public Assembly[] Assemblies => [.. AssemblyLoadContext.Assemblies];

        public bool IsActive { get; private set; }
        public Logger Logger { get; } = new();

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
                    AssemblyContext? assemblyContext = assembly.GetAssemblyContext();

                    return assemblyContext;
                }

                throw new InvalidOperationException();
            }
        }

        public AssemblyContext()
        {
            AssemblyLoadContext = new($"AssemblyContext", isCollectible: true);

            AssemblyLoadContext.Unloading += OnUnloading;
            AssemblyContextContainer.Add(this);
        }

        public void LoadModule(string path)
        {
            FileInfo file = new(path);
            LoadModule(file);
        }
        public void LoadModule(FileInfo file)
        {
            using FileStream fileStream = file.OpenRead();

            Assembly assembly = AssemblyLoadContext.LoadFromStream(fileStream);

            int lenWithoutExtension = file.Name.Length - 3;
            Logger.Log($"Module {file.Name[..(lenWithoutExtension - 1)]} loaded.");

            if (IsActive) assembly.InvokeRunHandle();
        }

        private Assembly[] ArrangeModulesByDependency()
        {
            List<Assembly> modules = [.. Assemblies];
            List<Assembly> arrangedModules = [];

            int attemps = 1000;
            while (modules.Count > 0)
            {
                for (int i = 0; i < modules.Count; i++)
                {
                    Assembly module = modules[i];

                    AssemblyName[] references = module.GetReferencedAssemblies();
                    AssemblyName[] assemblies = [.. modules.Select(x => x.GetName())];

                    foreach (AssemblyName assembly in assemblies)
                        foreach (AssemblyName reference in references)
                            if (assembly.FullName == reference.FullName)
                                goto Skip;

                    arrangedModules.Add(module);
                    modules.Remove(module);
                    i--;

                    Skip:;
                }

                attemps--;
                if (attemps <= 0)
                    throw new Exception("The maximum number of attempts to arrange the modules has been reached.");
            }

            return [.. arrangedModules];
        }

        public void Run()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (IsActive) return;

            IsActive = true;
            Assembly[] modules = ArrangeModulesByDependency();
            foreach (Assembly assembly in modules)
                assembly.InvokeRunHandle();
        }
        public void Stop()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!IsActive) return;

            IsActive = false;
            Assembly[] reversedModules = [.. ArrangeModulesByDependency().Reverse()];
            foreach (Assembly assembly in reversedModules)
                assembly.InvokeStopHandle();
        }

        private void OnUnloading(AssemblyLoadContext assemblyLoadContext)
        {
            AssemblyLoadContext.Unloading -= OnUnloading;
            AssemblyContextContainer.Remove(this);
        }

        private bool _disposed = false;
        public void Dispose()
        {
            if (_disposed) return;
            IsActive = false;
            _disposed = true;

            Logger.Log("AssemblyContext has been successfully unloaded.");

            AssemblyLoadContext.Unload();
            GC.SuppressFinalize(this);
        }
    }
}