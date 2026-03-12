namespace Project.Integration.Tests.Infrastructure;
using Xunit;

[CollectionDefinition(Name)]
public class IntegrationCollection : ICollectionFixture<IntegrationDatabaseFixture>
{
    public const string Name = "integration-tests";
}

