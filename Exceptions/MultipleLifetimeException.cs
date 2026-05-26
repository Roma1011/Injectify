namespace Injectify.Exceptions;

internal class MultipleLifetimeException:Exception
{
    private const string DefaultMessage = 
        "Multiple lifetime attributes have been applied to the same service. " +
        "A service must have only one lifetime declaration (e.g., Singleton, Scoped, or Transient).";

    public MultipleLifetimeException() : base(DefaultMessage){}
}