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

// Types the container cannot instantiate must be skipped.
[Scoped]
public abstract class AbstractService;

[Scoped]
public class ConcreteService : AbstractService;

[Singleton]
public static class StaticHelper;
