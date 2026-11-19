using System.Reflection;

namespace Injectify.Aggregator;

internal static class AssemblyExtensions
{
    /// <summary>
    /// Returns the types of <paramref name="assembly"/> that the runtime could load.
    /// When a referenced assembly is missing, <see cref="Assembly.GetTypes"/> throws
    /// <see cref="ReflectionTypeLoadException"/>; the types that did load are taken from it
    /// instead of failing the whole scan.
    /// </summary>
    internal static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
