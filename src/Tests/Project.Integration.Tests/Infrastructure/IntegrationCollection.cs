namespace Project.Integration.Tests.Infrastructure;
using Xunit;

[CollectionDefinition(Name, DisableParallelization = true)]
public class IntegrationCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "integration-tests";
}

