using System.Reflection;
using System.Text;
using Injectify.Attributes;
using Injectify.Attributes.@base;
using Injectify.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace Injectify.Aggregator;

public static class Aggregator
{
    private static readonly HashSet<Type> LifetimeAttributes = new()
    {
        typeof(Singleton), typeof(Scoped), typeof(Transient)
    };

//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Registers the classes annotated with lifetime attributes in the provided assemblies.
    /// Kept for compatibility: use <see cref="InjectifyServiceCollectionExtensions.AddInjectify"/> instead.
    /// </summary>
    /// <param name="collection">The <see cref="IServiceCollection"/> to which the services will be added.</param>
    /// <param name="assemblies">The assemblies to scan for types with lifetime attributes.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> with all discovered and registered services.</returns>
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    [Obsolete("Use services.AddInjectify(...) instead.")]
    public static IServiceCollection AggregateLifeTime(this IServiceCollection collection, params Assembly[] assemblies)
    {
        return Register(collection, assemblies);
    }

//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Scans the provided assemblies for concrete (non-abstract, non-static) classes annotated with
    /// lifetime attributes (<see cref="Singleton"/>, <see cref="Scoped"/>, or <see cref="Transient"/>),
    /// and registers them in the <see cref="IServiceCollection"/> with the corresponding service lifetime.
    ///
    /// A class is registered as the service type passed to its attribute (<c>[Scoped(typeof(IOrderService))]</c>);
    /// otherwise as the first interface annotated with the same attribute; otherwise as itself.
    /// </summary>
    /// <exception cref="MultipleLifetimeException">A class has more than one lifetime attribute.</exception>
    /// <exception cref="LifetimeMismatchException">A class and its service type have different lifetime attributes.</exception>
    /// <exception cref="InvalidServiceTypeException">The service type passed to an attribute cannot be used for its class.</exception>
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    internal static IServiceCollection Register(IServiceCollection services, Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);
        foreach (Assembly assembly in assemblies)
        {
            IEnumerable<TypeInfo> typesWithLifetimeAttributes = assembly.GetLoadableTypes()
                .Select(t => t.GetTypeInfo())
                .Where(t => t.IsClass && !t.IsAbstract &&
                            t.CustomAttributes.Any(attr => LifetimeAttributes.Contains(attr.AttributeType)));

            foreach (TypeInfo typeInfo in typesWithLifetimeAttributes)
            {
                if (typeInfo.CustomAttributes.Count(x => x.AttributeType.BaseType == typeof(BaseLifetimeAttribute)) > 1)
                    throw new MultipleLifetimeException(typeInfo);

                Type lifetimeAttributeType = GetLifetimeAttributeType(typeInfo)!;
                BaseLifetimeAttribute lifetimeAttribute =
                    (BaseLifetimeAttribute)typeInfo.GetCustomAttributes(lifetimeAttributeType, inherit: false).First();

                ServiceDescriptor descriptor = LifeSeeker(typeInfo, lifetimeAttributeType, lifetimeAttribute.ServiceType);
                services.Add(descriptor);
            }
        }

        return services;
    }

//----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- 
    /// <summary>
    /// Creates a <see cref="ServiceDescriptor"/> based on the specified implementation type and its associated lifetime attribute.
    ///
    /// The service type is, in order: the type passed to the attribute; the first implemented interface annotated
    /// with the same lifetime attribute; the implementation type itself.
    /// </summary>
    /// <param name="implementationType">The concrete implementation type to be registered.</param>
    /// <param name="lifetimeAttributeType">The type of the lifetime attribute (e.g., <see cref="Scoped"/>, <see cref="Transient"/>, or <see cref="Singleton"/>).</param>
    /// <param name="explicitServiceType">The service type passed to the attribute, if any.</param>
    /// <returns>A <see cref="ServiceDescriptor"/> representing the service registration.</returns>
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private static ServiceDescriptor LifeSeeker(TypeInfo implementationType, Type lifetimeAttributeType, Type? explicitServiceType)
    {
        Type serviceType = explicitServiceType is not null
            ? ValidateServiceType(implementationType, explicitServiceType, lifetimeAttributeType)
            : FindMarkedInterface(implementationType, lifetimeAttributeType) ?? implementationType;

        return new ServiceDescriptor(serviceType, implementationType, GetLifetimeFromAttribute(lifetimeAttributeType));
    }

//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Finds the first implemented interface annotated with the same lifetime attribute as the class.
    /// Throws when no such interface exists but another one is annotated with a different lifetime:
    /// registering the class as itself would silently leave that interface unregistered.
    /// </summary>
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private static Type? FindMarkedInterface(TypeInfo implementationType, Type lifetimeAttributeType)
    {
        (Type Interface, Type Attribute)? mismatch = null;

        foreach (Type serviceInterface in implementationType.ImplementedInterfaces)
        {
            Type? interfaceAttributeType = GetLifetimeAttributeType(serviceInterface);
            if (interfaceAttributeType is null)
                continue;

            if (interfaceAttributeType == lifetimeAttributeType)
                return ToServiceType(serviceInterface);

            mismatch ??= (serviceInterface, interfaceAttributeType);
        }

        if (mismatch is { } found)
            throw new LifetimeMismatchException(implementationType, lifetimeAttributeType, ToServiceType(found.Interface), found.Attribute);

        return null;
    }

    // Open generic implementations (Repository<T> : IRepository<T>) expose the interface
    // built from their own type parameters; register it as the open IRepository<>.
    // Closed interfaces (UserRepository : IRepository<User>) must stay closed.
    private static Type ToServiceType(Type serviceInterface) =>
        serviceInterface.ContainsGenericParameters ? serviceInterface.GetGenericTypeDefinition() : serviceInterface;

//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Checks that the service type passed to the attribute can be used for the class, and that it does not
    /// carry a different lifetime attribute.
    /// </summary>
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private static Type ValidateServiceType(TypeInfo implementationType, Type serviceType, Type lifetimeAttributeType)
    {
        if (!CanBeRegisteredAs(implementationType, serviceType))
            throw new InvalidServiceTypeException(implementationType, serviceType);

        Type? serviceAttributeType = GetLifetimeAttributeType(serviceType);
        if (serviceAttributeType is not null && serviceAttributeType != lifetimeAttributeType)
            throw new LifetimeMismatchException(implementationType, lifetimeAttributeType, serviceType, serviceAttributeType);

        return serviceType;
    }

    private static bool CanBeRegisteredAs(Type implementationType, Type serviceType)
    {
        // The container only accepts open generic services for open generic implementations, and vice versa.
        if (implementationType.IsGenericTypeDefinition != serviceType.IsGenericTypeDefinition)
            return false;

        if (!serviceType.IsGenericTypeDefinition)
            return serviceType.IsAssignableFrom(implementationType);

        // Repository<T> can be registered as IRepository<> (or as an open generic base class).
        return implementationType.GetInterfaces()
            .Concat(GetSelfAndBaseTypes(implementationType))
            .Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == serviceType);
    }

    private static IEnumerable<Type> GetSelfAndBaseTypes(Type type)
    {
        for (Type? current = type; current is not null; current = current.BaseType)
            yield return current;
    }

    private static Type? GetLifetimeAttributeType(Type type) =>
        type.CustomAttributes.FirstOrDefault(attr => LifetimeAttributes.Contains(attr.AttributeType))?.AttributeType;
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Maps a lifetime attribute type to a corresponding <see cref="ServiceLifetime"/> enum value.
    /// </summary>
    /// <param name="attributeType">The type of the attribute (must be <see cref="Scoped"/>, <see cref="Transient"/>, or <see cref="Singleton"/>).</param>
    /// <returns>The corresponding <see cref="ServiceLifetime"/>.</returns>
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    private static ServiceLifetime GetLifetimeFromAttribute(Type attributeType)
    {
        return attributeType == typeof(Scoped) ? ServiceLifetime.Scoped :
            attributeType == typeof(Transient) ? ServiceLifetime.Transient :
            ServiceLifetime.Singleton;
    }
//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
}