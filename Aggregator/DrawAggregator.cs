using System.Reflection;
using System.Text;
using Injectify.Attributes.@base;

namespace Injectify.Aggregator;

public static class DrawAggregator
{
    public static string GenerateMermaidGraph(params Assembly[] assemblies)
    {
        StringBuilder sb = new StringBuilder();
    
        sb.AppendLine("graph TD");

        foreach (Assembly assembly in assemblies)
        {
            IEnumerable<Type> services = assembly.GetTypes()
                .Where(t => t.GetCustomAttributes(typeof(BaseLifetimeAttribute), true).Any());

            foreach (Type service in services)
            {
                ConstructorInfo? constructor = service.GetConstructors().FirstOrDefault();
                if (constructor != null)
                {
                    foreach (ParameterInfo param in constructor.GetParameters())
                    {
                        string cleanServiceName = CleanTypeName(service.Name);
                        string cleanParamTypeName = CleanTypeName(param.ParameterType.Name);
                        string paramName = param.Name;

                        sb.AppendLine($"    {cleanServiceName} -->|{paramName}| {cleanParamTypeName}");
                    }
                }
            }
        }
        return sb.ToString();
    }

    private static string CleanTypeName(string typeName)
    {
        if (typeName.StartsWith("<") && typeName.Contains("__"))
        {
            int index = typeName.LastIndexOf("__", StringComparison.Ordinal);
            if (index != -1)
            {
                typeName = typeName.Substring(index + 2);
            }
        }

        typeName = typeName.Replace("`1", "")
            .Replace("`2", "")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace("'", "")
            .Replace("`", "");

        return typeName;
    }
}
