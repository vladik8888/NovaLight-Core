using Mono.Cecil;

namespace NovaLight.Core.Extensions
{
    public static class TypeDefinitionExtension
    {
        public static bool InheritsFrom(this TypeDefinition type, TypeDefinition baseType)
        {
            TypeDefinition? current = type;
            while (current != null)
            {
                if (baseType.FullName == current.FullName)
                    return true;
                current = current.BaseType.Resolve();
            }
            return false;
        }
    }
}