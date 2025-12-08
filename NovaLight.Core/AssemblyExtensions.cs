using NovaLight.Core.Interaction;
using System.Reflection;
using System.Runtime.Loader;

namespace NovaLight.Core
{
    internal static class AssemblyExtensions
    {
        public static AssemblyContext GetAssemblyContext(this Assembly assembly)
        {
            AssemblyLoadContext? assemblyLoadContext = AssemblyLoadContext.GetLoadContext(assembly);
            if (assemblyLoadContext == AssemblyLoadContext.Default)
                throw new InvalidOperationException();

            AssemblyContext assemblyContext = AssemblyContextContainer.AssemblyContexts
                .FirstOrDefault(x => x.AssemblyLoadContext.Equals(assemblyLoadContext))
                ?? throw new InvalidOperationException();

            return assemblyContext;
        }

        internal static Type[] GetModuleControllerTypes(this Assembly assembly)
        {
            Type[] moduleControllerTypes = [.. assembly.GetTypes()
                    .Where(x => !x.IsAbstract && typeof(ModuleController).IsAssignableFrom(x))];
            return moduleControllerTypes;
        }
        internal static void InvokeRunHandle(this Assembly assembly)
        {
            AssemblyContext assemblyContext = assembly.GetAssemblyContext();
            Logger logger = assemblyContext.Logger;

            Type[] controllerTypes = assembly.GetModuleControllerTypes();
            foreach (Type type in controllerTypes)
                try
                {
                    ModuleController controller = (ModuleController)Activator.CreateInstance(type)!;
                    controller.OnModuleRun();
                }
                catch (Exception exception)
                {
                    logger.Log($"Error occurred while starting the module: {exception.Message}");
                }

            logger.Log($"Module {assembly.GetName().Name} has been started.");
        }
        internal static void InvokeStopHandle(this Assembly assembly)
        {
            AssemblyContext assemblyContext = assembly.GetAssemblyContext();
            Logger logger = assemblyContext.Logger;

            Type[] controllerTypes = assembly.GetModuleControllerTypes();
            foreach (Type type in controllerTypes)
                try
                {
                    ModuleController controller = (ModuleController)Activator.CreateInstance(type)!;
                    controller.OnModuleStop();
                }
                catch (Exception exception)
                {
                    logger.Log($"Error occurred while stopping the module: {exception.Message}");
                }

            logger.Log($"Module {assembly.GetName().Name} has been stopped.");
        }
    }
}