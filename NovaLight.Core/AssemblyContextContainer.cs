namespace NovaLight.Core
{
    public class AssemblyContextContainer
    {
        private static readonly List<AssemblyContext> _assemblyContexts = [];
        public static AssemblyContext[] AssemblyContexts => [.. _assemblyContexts];

        internal static void Add(AssemblyContext assemblyContext) => _assemblyContexts.Add(assemblyContext);
        internal static void Remove(AssemblyContext assemblyContext) => _assemblyContexts.Remove(assemblyContext);
    }
}