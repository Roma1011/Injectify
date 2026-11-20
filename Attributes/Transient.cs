using Injectify.Attributes.@base;

namespace Injectify.Attributes;

/// <summary>Registers the class with a transient lifetime.</summary>
public class Transient : BaseLifetimeAttribute
{
    public Transient()
    {
    }

    /// <param name="serviceType">The service type to register the class as, for example <c>typeof(IRepository&lt;&gt;)</c>.</param>
    public Transient(Type serviceType) : base(serviceType)
    {
    }
}
