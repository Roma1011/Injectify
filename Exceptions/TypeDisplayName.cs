namespace Injectify.Exceptions;

internal static class TypeDisplayName
{
    /// <summary>Formats a type the way it is written in C#: <c>MyApp.IRepository&lt;T&gt;</c> instead of <c>MyApp.IRepository`1</c>.</summary>
    internal static string GetDisplayName(this Type type)
    {
        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        string name = type.Name;
        int arityIndex = name.IndexOf('`');
        if (arityIndex >= 0)
            name = name[..arityIndex];

        string arguments = string.Join(", ", type.GetGenericArguments()
            .Select(argument => argument.IsGenericParameter ? argument.Name : argument.GetDisplayName()));

        return string.IsNullOrEmpty(type.Namespace)
            ? $"{name}<{arguments}>"
            : $"{type.Namespace}.{name}<{arguments}>";
    }
}
