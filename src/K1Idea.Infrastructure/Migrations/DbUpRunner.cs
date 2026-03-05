using DbUp;

namespace K1Idea.Infrastructure.Migrations;

public static class DbUpRunner
{
    public static void Run(string connectionString)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DbUpRunner).Assembly,
                s => s.Contains("Scripts"))
            .WithTransactionPerScript()
            .WithVariablesDisabled()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
            throw new Exception("DbUp migration failed.", result.Error);
    }
}
