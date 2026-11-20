using Injectify.Attributes.@base;

namespace Injectify.Attributes;

/// <summary>Registers the class with a scoped lifetime.</summary>
public class Scoped : BaseLifetimeAttribute
{
    public Scoped()
    {
    }

    /// <param name="serviceType">The service type to register the class as, for example <c>typeof(IOrderService)</c>.</param>
    public Scoped(Type serviceType) : base(serviceType)
    {
    }
}
