namespace Injectify.Exceptions;

/// <summary>Base class for the configuration errors Injectify reports while scanning assemblies.</summary>
public abstract class InjectifyException : Exception
{
    private protected InjectifyException(string message) : base(message)
    {
    }
}
