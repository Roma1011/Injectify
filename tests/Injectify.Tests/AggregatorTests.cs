using System.Reflection;
using Injectify.Aggregator;
using Injectify.Exceptions;
using Injectify.Tests.Fixtures;
using Injectify.Tests.Fixtures.Isolated;
using Microsoft.Extensions.DependencyInjection;

namespace Injectify.Tests;

public class AggregatorTests
{
    // Every fixture except the isolated ones, which are invalid on purpose and scanned one at a time.
    private static readonly Assembly ValidFixtures = new TypeListAssembly(
        typeof(AggregatorTests).Assembly.GetTypes()
            .Where(type => type.Namespace != typeof(DualLifetime).Namespace)
            .ToArray());

    private static IServiceCollection Scan() => new ServiceCollection().AddInjectify(ValidFixtures);

    private static IServiceCollection ScanOnly(Type type) => new ServiceCollection().AddInjectify(new TypeListAssembly(type));

    private static ServiceProvider BuildProvider() =>
        Scan().BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    [Theory]
    [InlineData(typeof(SelfSingleton), ServiceLifetime.Singleton)]
    [InlineData(typeof(SelfScoped), ServiceLifetime.Scoped)]
    [InlineData(typeof(SelfTransient), ServiceLifetime.Transient)]
    public void Registers_class_as_itself_with_attribute_lifetime(Type type, ServiceLifetime lifetime)
    {
        ServiceDescriptor descriptor = Assert.Single(Scan(), d => d.ServiceType == type);

        Assert.Equal(type, descriptor.ImplementationType);
        Assert.Equal(lifetime, descriptor.Lifetime);
    }

    [Fact]
    public void Binds_class_to_interface_with_same_lifetime_attribute()
    {
        ServiceDescriptor descriptor = Assert.Single(Scan(), d => d.ImplementationType == typeof(Greeter));

        Assert.Equal(typeof(IGreeter), descriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Does_not_bind_to_interface_without_lifetime_attribute()
    {
        IServiceCollection services = Scan();

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IUnmarked));
        Assert.Single(services, d => d.ServiceType == typeof(UnmarkedImplementation));
    }

    [Fact]
    public void Registers_open_generic_class_as_open_generic_service()
    {
        ServiceDescriptor descriptor = Assert.Single(Scan(), d => d.ImplementationType == typeof(Repository<>));

        Assert.Equal(typeof(IRepository<>), descriptor.ServiceType);
    }

    // https://github.com/Roma1011/Injectify/issues/1
    [Fact]
    public void Registers_closed_generic_class_as_closed_generic_service()
    {
        ServiceDescriptor descriptor = Assert.Single(Scan(), d => d.ImplementationType == typeof(UserRepository));

        Assert.Equal(typeof(IRepository<User>), descriptor.ServiceType);
    }

    [Fact]
    public void Resolves_closed_and_open_generic_registrations_side_by_side()
    {
        using ServiceProvider provider = BuildProvider();

        Assert.IsType<UserRepository>(provider.GetRequiredService<IRepository<User>>());
        Assert.IsType<Repository<Order>>(provider.GetRequiredService<IRepository<Order>>());
    }

    [Fact]
    public void Registers_class_as_service_type_passed_to_attribute()
    {
        IServiceCollection services = Scan();

        ServiceDescriptor descriptor = Assert.Single(services, d => d.ImplementationType == typeof(CardPaymentGateway));
        Assert.Equal(typeof(IPaymentGateway), descriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Service_type_passed_to_attribute_wins_over_marked_interface()
    {
        ServiceDescriptor descriptor = Assert.Single(Scan(), d => d.ImplementationType == typeof(EmailNotifier));

        Assert.Equal(typeof(INotifier), descriptor.ServiceType);
    }

    [Fact]
    public void Registers_open_generic_class_as_open_service_type_passed_to_attribute()
    {
        ServiceDescriptor descriptor = Assert.Single(Scan(), d => d.ImplementationType == typeof(ReadStore<>));

        Assert.Equal(typeof(IReadStore<>), descriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void Registers_closed_class_as_closed_service_type_passed_to_attribute()
    {
        ServiceDescriptor descriptor = Assert.Single(Scan(), d => d.ImplementationType == typeof(OrderReadStore));

        Assert.Equal(typeof(IReadStore<Order>), descriptor.ServiceType);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void Resolves_services_registered_by_service_type_passed_to_attribute()
    {
        using ServiceProvider provider = BuildProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<CardPaymentGateway>(scope.ServiceProvider.GetRequiredService<IPaymentGateway>());
        Assert.IsType<EmailNotifier>(scope.ServiceProvider.GetRequiredService<INotifier>());
        Assert.IsType<OrderReadStore>(provider.GetRequiredService<IReadStore<Order>>());
        Assert.IsType<ReadStore<User>>(provider.GetRequiredService<IReadStore<User>>());
    }

    [Fact]
    public void Skips_abstract_and_static_classes()
    {
        IServiceCollection services = Scan();

        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(AbstractService));
        Assert.DoesNotContain(services, d => d.ImplementationType == typeof(StaticHelper));
        Assert.Single(services, d => d.ImplementationType == typeof(ConcreteService));
    }

    [Fact]
    public void Scanned_registrations_pass_container_validation()
    {
        Exception? exception = Record.Exception(() => BuildProvider().Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Throws_when_class_has_multiple_lifetime_attributes()
    {
        MultipleLifetimeException exception = Assert.Throws<MultipleLifetimeException>(() => ScanOnly(typeof(DualLifetime)));

        Assert.Equal(typeof(DualLifetime), exception.ImplementationType);
        Assert.Contains(typeof(DualLifetime).FullName!, exception.Message);
    }

    [Fact]
    public void Throws_when_marked_interface_has_a_different_lifetime()
    {
        LifetimeMismatchException exception =
            Assert.Throws<LifetimeMismatchException>(() => ScanOnly(typeof(TransientOnScopedContract)));

        Assert.Equal(typeof(TransientOnScopedContract), exception.ImplementationType);
        Assert.Equal(typeof(IScopedContract), exception.ServiceType);
        Assert.Contains("[Transient]", exception.Message);
        Assert.Contains("[Scoped]", exception.Message);
    }

    [Fact]
    public void Throws_when_service_type_passed_to_attribute_has_a_different_lifetime()
    {
        LifetimeMismatchException exception =
            Assert.Throws<LifetimeMismatchException>(() => ScanOnly(typeof(SingletonAsScopedContract)));

        Assert.Equal(typeof(IScopedContract), exception.ServiceType);
    }

    [Fact]
    public void Binds_to_marked_interface_with_same_lifetime_when_another_one_differs()
    {
        ServiceDescriptor descriptor = Assert.Single(ScanOnly(typeof(MatchesOneOfTwoMarkedInterfaces)));

        Assert.Equal(typeof(IScopedContract), descriptor.ServiceType);
    }

    [Theory]
    [InlineData(typeof(NotDisposable))]
    [InlineData(typeof(OpenStoreAsClosedService<>))]
    [InlineData(typeof(ClosedStoreAsOpenService))]
    public void Throws_when_service_type_passed_to_attribute_cannot_be_used(Type type)
    {
        InvalidServiceTypeException exception = Assert.Throws<InvalidServiceTypeException>(() => ScanOnly(type));

        Assert.Equal(type, exception.ImplementationType);
    }

    [Fact]
    public void Error_messages_show_generic_types_as_written_in_csharp()
    {
        InvalidServiceTypeException exception =
            Assert.Throws<InvalidServiceTypeException>(() => ScanOnly(typeof(OpenStoreAsClosedService<>)));

        Assert.Contains("Injectify.Tests.Fixtures.Isolated.OpenStoreAsClosedService<T>", exception.Message);
        Assert.Contains("Injectify.Tests.Fixtures.IRepository<Injectify.Tests.Fixtures.User>", exception.Message);
    }

    [Fact]
    public void All_errors_derive_from_InjectifyException()
    {
        Assert.IsAssignableFrom<InjectifyException>(Record.Exception(() => ScanOnly(typeof(DualLifetime))));
        Assert.IsAssignableFrom<InjectifyException>(Record.Exception(() => ScanOnly(typeof(TransientOnScopedContract))));
        Assert.IsAssignableFrom<InjectifyException>(Record.Exception(() => ScanOnly(typeof(NotDisposable))));
    }

    [Fact]
    public void Skips_types_that_fail_to_load()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddInjectify(new PartiallyLoadableAssembly(typeof(SelfSingleton)));

        ServiceDescriptor descriptor = Assert.Single(services);
        Assert.Equal(typeof(SelfSingleton), descriptor.ServiceType);
    }

    [Fact]
    public void Throws_on_null_arguments()
    {
        IServiceCollection services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddInjectify(ValidFixtures));
        Assert.Throws<ArgumentNullException>(() => services.AddInjectify(null!));
    }

    [Fact]
    public void AggregateLifeTime_still_registers_the_same_services()
    {
#pragma warning disable CS0618 // AggregateLifeTime is obsolete; it must keep working until it is removed.
        IServiceCollection legacy = new ServiceCollection().AggregateLifeTime(ValidFixtures);
#pragma warning restore CS0618

        Assert.Equal(
            Scan().Select(d => (d.ServiceType, d.ImplementationType, d.Lifetime)),
            legacy.Select(d => (d.ServiceType, d.ImplementationType, d.Lifetime)));
    }
}
