namespace Project.BDD.E2E.Tests.Infrastructure;

internal static class BddEnvironmentHost
{
    public static BddEnvironment Instance { get; } = new();
}
