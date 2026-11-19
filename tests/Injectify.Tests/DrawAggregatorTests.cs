using Injectify.Aggregator;
using Injectify.Tests.Fixtures;

namespace Injectify.Tests;

public class DrawAggregatorTests
{
    [Fact]
    public void Draws_edges_for_the_constructor_with_the_most_parameters()
    {
        string graph = DrawAggregator.GenerateMermaidGraph(typeof(DrawAggregatorTests).Assembly);

        Assert.Contains("MultiConstructorService -->|singleton| SelfSingleton", graph);
        Assert.Contains("MultiConstructorService -->|scoped| SelfScoped", graph);
    }

    [Fact]
    public void Skips_types_that_fail_to_load()
    {
        PartiallyLoadableAssembly assembly = new(typeof(MultiConstructorService));

        string graph = DrawAggregator.GenerateMermaidGraph(assembly);

        Assert.StartsWith("graph TD", graph);
        Assert.Contains("MultiConstructorService -->|singleton| SelfSingleton", graph);
    }
}
