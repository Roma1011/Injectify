using System.Reflection;
using Injectify.Attributes;
using Injectify.Exceptions;

// Same namespace as AddScoped/AddSingleton, so AddInjectify shows up without an extra using.
namespace Microsoft.Extensions.DependencyInjection;

public static class InjectifyServiceCollectionExtensions
{
    /// <summary>
    /// Scans <paramref name="assemblies"/> for concrete classes marked with <see cref="Singleton"/>,
    /// <see cref="Scoped"/> or <see cref="Transient"/> and registers them with that lifetime.
    /// </summary>
    /// <remarks>
    /// A class is registered as the service type passed to its attribute (<c>[Scoped(typeof(IOrderService))]</c>);
    /// otherwise as the first interface marked with the same attribute; otherwise as itself.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="assemblies">The assemblies to scan, for example <c>typeof(Program).Assembly</c>.</param>
    /// <returns>The same <see cref="IServiceCollection"/>, for chaining.</returns>
    /// <exception cref="MultipleLifetimeException">A class has more than one lifetime attribute.</exception>
    /// <exception cref="LifetimeMismatchException">A class and its service type have different lifetime attributes.</exception>
    /// <exception cref="InvalidServiceTypeException">The service type passed to an attribute cannot be used for its class.</exception>
    public static IServiceCollection AddInjectify(this IServiceCollection services, params Assembly[] assemblies)
    {
        return global::Injectify.Aggregator.Aggregator.Register(services, assemblies);
    }
}
