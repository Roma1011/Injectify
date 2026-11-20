namespace Injectify.Exceptions;

/// <summary>Thrown when a class has more than one lifetime attribute.</summary>
public sealed class MultipleLifetimeException : InjectifyException
{
    internal MultipleLifetimeException(Type implementationType)
        : base($"'{implementationType.GetDisplayName()}' has multiple lifetime attributes. " +
               "A service must have only one lifetime declaration (Singleton, Scoped, or Transient).")
    {
        ImplementationType = implementationType;
    }

    /// <summary>The class that has more than one lifetime attribute.</summary>
    public Type ImplementationType { get; }
}
