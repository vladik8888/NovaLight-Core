using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace NovaLight.Core.Extensions
{
    public static class AssemblyExtension
    {
        public static AssemblyContext? GetAssemblyContext(this Assembly assembly)
        {
            AssemblyLoadContext? assemblyLoadContext = AssemblyLoadContext.GetLoadContext(assembly);
            if (assemblyLoadContext == null || assemblyLoadContext == AssemblyLoadContext.Default)
                return null;

            AssemblyContext? assemblyContext = AssemblyContext.AllAssemblyContexts.ToList()
                .FirstOrDefault(x => x.AssemblyLoadContext.Equals(assemblyLoadContext));
            return assemblyContext;
        }
    }
}