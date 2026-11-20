namespace Injectify.Attributes.@base;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public abstract class BaseLifetimeAttribute : Attribute
{
    protected BaseLifetimeAttribute()
    {
    }

    protected BaseLifetimeAttribute(Type serviceType)
    {
        ServiceType = serviceType;
    }

    /// <summary>
    /// The service type to register the class as, for example <c>typeof(IOrderService)</c>,
    /// or <c>typeof(IRepository&lt;&gt;)</c> for an open generic class.
    /// When not set, the class is registered as the first interface marked with the same lifetime
    /// attribute, or as itself. Ignored when the attribute is placed on an interface.
    /// </summary>
    public Type? ServiceType { get; }
}
