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

        public static void Run(this Assembly assembly)
        {
            AssemblyContext assemblyContext = assembly.GetAssemblyContext();
            Logger logger = assemblyContext.Logger;

            try
            {
                assembly.InvokeInteraction<IRunHandler>();
                logger.Log($"Module {assembly.GetName().Name} has been successfully started.");
            }
            catch (Exception exception)
            {
                logger.Log($"Error occurred while starting the module: {exception.Message}");
            }
        }

        public static void Stop(this Assembly assembly)
        {
            AssemblyContext assemblyContext = assembly.GetAssemblyContext();
            Logger logger = assemblyContext.Logger;

            try
            {
                assembly.InvokeInteraction<IStopHandler>();
                logger.Log($"Module {assembly.GetName().Name} has been successfully stopped.");
            }
            catch (Exception exception)
            {
                logger.Log($"Error occurred while stopping the module: {exception.Message}");
            }
        }
    }
}