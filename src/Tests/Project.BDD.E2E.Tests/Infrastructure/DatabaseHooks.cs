using Reqnroll;

namespace Project.BDD.E2E.Tests.Infrastructure;

[Binding]
public sealed class DatabaseHooks
{
    private static readonly SemaphoreSlim DatabaseGate = new(1, 1);
    internal const string ScenarioStateKey = "bdd-scenario-state";

    [BeforeScenario(Order = -1000)]
    public static async Task BeforeScenarioAsync(ScenarioContext scenarioContext)
    {
        await DatabaseGate.WaitAsync();

        try
        {
            var env = BddEnvironmentHost.Instance;
            await env.EnsureInitializedAsync();
            await env.ResetDatabaseAsync();

            var state = new BddScenarioState
            {
                Client = env.CreateApiClient()
            };
            scenarioContext[ScenarioStateKey] = state;
        }
        catch
        {
            DatabaseGate.Release();
            throw;
        }
    }

    [AfterScenario(Order = 1000)]
    public static async Task AfterScenarioAsync(ScenarioContext scenarioContext)
    {
        try
        {
            await BddEnvironmentHost.Instance.ResetDatabaseAsync();
        }
        finally
        {
            if (scenarioContext.TryGetValue(ScenarioStateKey, out var raw) && raw is BddScenarioState state)
            {
                state.LastResponse?.Dispose();
                state.Client?.Dispose();
            }

            DatabaseGate.Release();
        }
    }
}
