using System.Reflection;

namespace Injectify.Tests;

/// <summary>
/// An assembly whose <see cref="GetTypes"/> fails the way it does when a dependency is missing:
/// it throws <see cref="ReflectionTypeLoadException"/> that still carries the types that did load.
/// </summary>
internal sealed class PartiallyLoadableAssembly(params Type[] loadableTypes) : Assembly
{
    public override Type[] GetTypes()
    {
        Type?[] types = [.. loadableTypes, null];
        Exception?[] exceptions = [.. new Exception?[loadableTypes.Length], new TypeLoadException("Simulated load failure.")];

        throw new ReflectionTypeLoadException(types, exceptions);
    }
}
