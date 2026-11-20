using Injectify.Attributes;

namespace Injectify.Tests.Fixtures.Isolated;

// Each test scans one of these types on its own (see TypeListAssembly).
// Most of them are invalid on purpose, so the assembly-wide scans skip this namespace.

[Scoped]
[Singleton]
public class DualLifetime;

[Scoped]
public interface IScopedContract;

[Transient]
public class TransientOnScopedContract : IScopedContract;

[Singleton(typeof(IScopedContract))]
public class SingletonAsScopedContract : IScopedContract;

[Singleton]
public interface ISingletonContract;

// Valid: one of the marked interfaces has the class's lifetime, so the other one is not a mistake.
[Scoped]
public class MatchesOneOfTwoMarkedInterfaces : ISingletonContract, IScopedContract;

[Scoped(typeof(IDisposable))]
public class NotDisposable;

[Transient(typeof(IRepository<User>))]
public class OpenStoreAsClosedService<T> : IRepository<T>;

[Transient(typeof(IRepository<>))]
public class ClosedStoreAsOpenService : IRepository<Order>;
