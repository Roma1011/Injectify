using Injectify.Attributes.@base;

namespace Injectify.Attributes;

/// <summary>Registers the class with a singleton lifetime.</summary>
public class Singleton : BaseLifetimeAttribute
{
    public Singleton()
    {
    }

    /// <param name="serviceType">The service type to register the class as, for example <c>typeof(IClock)</c>.</param>
    public Singleton(Type serviceType) : base(serviceType)
    {
    }
}
