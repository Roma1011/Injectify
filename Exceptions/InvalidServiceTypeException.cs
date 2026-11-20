namespace Injectify.Exceptions;

/// <summary>
/// Thrown when the service type passed to a lifetime attribute, as in <c>[Scoped(typeof(IOrderService))]</c>,
/// cannot be used for the class it is placed on.
/// </summary>
public sealed class InvalidServiceTypeException : InjectifyException
{
    internal InvalidServiceTypeException(Type implementationType, Type serviceType)
        : base($"'{implementationType.GetDisplayName()}' cannot be registered as '{serviceType.GetDisplayName()}'. " +
               Reason(implementationType, serviceType))
    {
        ImplementationType = implementationType;
        ServiceType = serviceType;
    }

    /// <summary>The class being registered.</summary>
    public Type ImplementationType { get; }

    /// <summary>The service type passed to the lifetime attribute.</summary>
    public Type ServiceType { get; }

    private static string Reason(Type implementationType, Type serviceType)
    {
        if (implementationType.IsGenericTypeDefinition && !serviceType.IsGenericTypeDefinition)
            return "An open generic class must use an open generic service type, such as typeof(IRepository<>).";

        if (!implementationType.IsGenericTypeDefinition && serviceType.IsGenericTypeDefinition)
            return "Only open generic classes can use an open generic service type; use a closed one, such as typeof(IRepository<User>).";

        return "The class must implement or inherit the service type.";
    }
}
