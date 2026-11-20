namespace Injectify.Exceptions;

/// <summary>
/// Thrown when a class and the service type it would be registered as carry different lifetime attributes,
/// for example a <c>[Transient]</c> class implementing a <c>[Scoped]</c> interface.
/// </summary>
public sealed class LifetimeMismatchException : InjectifyException
{
    internal LifetimeMismatchException(Type implementationType, Type implementationAttribute, Type serviceType, Type serviceAttribute)
        : base($"'{implementationType.GetDisplayName()}' is marked [{implementationAttribute.Name}], " +
               $"but its service type '{serviceType.GetDisplayName()}' is marked [{serviceAttribute.Name}]. " +
               "Use the same lifetime on both, or remove the attribute from the service type and pass it " +
               "to the class attribute instead.")
    {
        ImplementationType = implementationType;
        ServiceType = serviceType;
    }

    /// <summary>The class being registered.</summary>
    public Type ImplementationType { get; }

    /// <summary>The interface or service type whose lifetime attribute differs from the class.</summary>
    public Type ServiceType { get; }
}
