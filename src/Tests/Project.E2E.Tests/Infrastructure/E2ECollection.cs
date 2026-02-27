using Xunit;

namespace Project.E2E.Tests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public class E2ECollection : ICollectionFixture<E2EEnvironmentFixture>
{
    public const string Name = "e2e-tests";
}

