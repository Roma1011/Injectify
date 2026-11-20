using Injectify.Attributes;

namespace Injectify.Tests.Fixtures;

// Every type here is picked up when the tests scan this assembly,
// so all of them must form a valid container together.

[Singleton]
public class SelfSingleton;

[Scoped]
public class SelfScoped;

[Transient]
public class SelfTransient;

// Interface binding: both sides carry the same lifetime attribute.
[Scoped]
public interface IGreeter;

[Scoped]
public class Greeter : IGreeter;

// Interface without a lifetime attribute: the class is registered as itself only.
public interface IUnmarked;

[Singleton]
public class UnmarkedImplementation : IUnmarked;

// Generics.
public class User;

public class Order;

[Transient]
public interface IRepository<T>;

[Transient]
public class Repository<T> : IRepository<T>;

[Transient]
public class UserRepository : IRepository<User>;

// Service type passed to the attribute: only the class is marked, the interface stays plain.
public interface IPaymentGateway;

[Scoped(typeof(IPaymentGateway))]
public class CardPaymentGateway : IPaymentGateway, IDisposable
{
    public void Dispose()
    {
    }
}

// The service type passed to the attribute wins over an interface marked with the same attribute.
public interface INotifier;

[Scoped(typeof(INotifier))]
public class EmailNotifier : IGreeter, INotifier;

public interface IReadStore<T>;

[Transient(typeof(IReadStore<>))]
public class ReadStore<T> : IReadStore<T>;

[Singleton(typeof(IReadStore<Order>))]
public class OrderReadStore : IReadStore<Order>;

// Types the container cannot instantiate must be skipped.
[Scoped]
public abstract class AbstractService;

[Scoped]
public class ConcreteService : AbstractService;

[Singleton]
public static class StaticHelper;

// Two constructors: DI uses the one with the most parameters, and so must the Mermaid graph.
[Transient]
public class MultiConstructorService
{
    public MultiConstructorService()
    {
    }

    public MultiConstructorService(SelfSingleton singleton, SelfScoped scoped)
    {
    }
}
