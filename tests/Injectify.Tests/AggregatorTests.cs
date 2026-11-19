using System.Reflection;
using System.Reflection.Emit;
using Injectify.Aggregator;
using Injectify.Attributes;
using Injectify.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Injectify.Tests;

public class AggregatorTests
{
    private static IServiceCollection Scan()
    {
        IServiceCollection services = new ServiceCollection();
        return services.AggregateLifeTime(typeof(AggregatorTests).Assembly);
    }

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
        Assembly assembly = CreateAssemblyWithType("DualLifetime", typeof(Scoped), typeof(Singleton));
        IServiceCollection services = new ServiceCollection();

        Exception exception = Assert.ThrowsAny<Exception>(() => services.AggregateLifeTime(assembly));

        Assert.Equal("MultipleLifetimeException", exception.GetType().Name);
    }

    [Fact]
    public void Skips_types_that_fail_to_load()
    {
        IServiceCollection services = new ServiceCollection();

        services.AggregateLifeTime(new PartiallyLoadableAssembly(typeof(SelfSingleton)));

        ServiceDescriptor descriptor = Assert.Single(services);
        Assert.Equal(typeof(SelfSingleton), descriptor.ServiceType);
    }

    [Fact]
    public void Throws_on_null_arguments()
    {
        IServiceCollection services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AggregateLifeTime(typeof(AggregatorTests).Assembly));
        Assert.Throws<ArgumentNullException>(() => services.AggregateLifeTime(null!));
    }

    // A type with two lifetime attributes would break every other scan of this assembly,
    // so it is emitted into its own in-memory assembly.
    private static Assembly CreateAssemblyWithType(string typeName, params Type[] attributeTypes)
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"Injectify.Tests.Dynamic.{Guid.NewGuid():N}"), AssemblyBuilderAccess.Run);
        TypeBuilder type = assembly.DefineDynamicModule("Main")
            .DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class);

        foreach (Type attributeType in attributeTypes)
            type.SetCustomAttribute(new CustomAttributeBuilder(attributeType.GetConstructor(Type.EmptyTypes)!, []));

        type.CreateType();
        return assembly;
    }
}
