![ezgif-4d080faf903e07](https://github.com/user-attachments/assets/d0f022ea-e649-4ed0-b985-5ca98bd9d408)



# Injectify

## ⚡ Overview
This project provides a utility for automatically registering services in a .NET application using custom attributes (`Singleton`, `Scoped`, and `Transient`). It simplifies the process of configuring dependency injection by scanning assemblies and registering services based on their lifetime.


## 🚀 Features

- 🛠️ **Custom Attributes**: Use `[Singleton]`, `[Scoped]`, and `[Transient]` to define the lifetime of your services.
- 🔄 **Automatic Registration**: Automatically scans assemblies and registers services with the appropriate lifetime in the `IServiceCollection`.
- 🏷️ **Interface Binding**: Automatically binds implementations to their interfaces if defined.


### 📦 Installation
Just clone or include the Injectify class in your project. No external dependencies are required.

## 📖 Usage
### 🔷 1. Define your services and annotate them with the appropriate lifetime attribute:

### Example 1: Scoped
```
using Injectify.Attributes;

[Scoped]
public interface IScopedLogger
{
    void Log(string message);
}

[Scoped]
public class ScopedLogger : IScopedLogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[Scoped] {message}");
    }
}
```
### Example 2: Singleton
```
using Injectify.Attributes;

using Injectify.Attributes;

[Singleton]
public class GlobalConfiguration
{
    public string Environment { get; set; } = "Production";
}
```

### Example 3: Transient
```
using Injectify.Attributes;

[Scoped]
public interface IUserContext
{
    string GetUserId();
}

[Scoped]
public class UserContext : IUserContext
{
    public string GetUserId() => "user-123";
}

[Transient]
public class AuditService
{
    private readonly IUserContext _userContext;

    public AuditService(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public void Audit(string action)
    {
        Console.WriteLine($"User {_userContext.GetUserId()} performed: {action}");
    }
}
```
### Example 4: Generic
```
using Injectify.Attributes;

[Transient]
public interface IRepository<T>
{
    void Add(T item);
}

[Transient]
public class InMemoryRepository<T> : IRepository<T>
{
    private readonly List<T> _items = new();

    public void Add(T item)
    {
        _items.Add(item);
    }
}
```
### 🔷 2. Use the Aggregator class to register services in your Startup or Program class:
```
using DiÆon.Aggregator;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

IServiceCollection services=new ServiceCollection();
services.AggregateLifeTime([typeof(Program).Assembly]);

```
### Attributes
```
[Singleton]: Registers the service with a singleton lifetime.
[Scoped]: Registers the service with a scoped lifetime.
[Transient]: Registers the service with a transient lifetime.
```

### ⚠️ Error Handling
```
MultipleLifetimeException

```
### 👨‍💻 Contributing
Feel free to submit issues or create pull requests to enhance Injectify.


