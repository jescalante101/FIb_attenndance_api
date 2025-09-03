using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Text.RegularExpressions;

public class TriggerTableInterceptor : DbCommandInterceptor
{
    private static readonly HashSet<string> TriggeredTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppUser", "AppUserSite", "att_attshift", "att_breaktime", "att_manuallog",
        "att_shiftdetail", "att_timeinterval", "att_timeinterval_break_time",
        "EmployeeShiftAssignments", "HLD1", "OHLD", "Permissions",
        "SiteAreaCostCenter", "SiteCostCenter", "UserPermissions"
    };

    // Patrón para cualquier OUTPUT sin INTO
    private static readonly Regex OutputClauseRegex = new(
        @"OUTPUT\s+(?:INSERTED|DELETED|\$action)\.[\w\[\]]+(?:\s*,\s*(?:INSERTED|DELETED|\$action)\.[\w\[\]]+)*(?!\s+INTO)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline
    );

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        ProcessCommand(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ProcessCommand(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    {
        ProcessCommand(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ProcessCommand(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    {
        ProcessCommand(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        ProcessCommand(command);
        return ValueTask.FromResult(result);
    }

    private void ProcessCommand(DbCommand command)
    {
        if (string.IsNullOrEmpty(command.CommandText))
            return;

        // Verificar si afecta tablas con triggers
        if (!HasTriggeredTable(command.CommandText))
            return;

        var originalSql = command.CommandText;

        // Remover OUTPUT sin INTO
        var modifiedSql = OutputClauseRegex.Replace(originalSql, "");

        // Limpiar espacios sobrantes
        modifiedSql = Regex.Replace(modifiedSql, @"\s+", " ").Trim();

        if (originalSql != modifiedSql)
        {
            command.CommandText = modifiedSql;
            Console.WriteLine($"[INTERCEPTOR] ✅ OUTPUT clause removed from SQL");
            Console.WriteLine($"Table affected: {GetTableName(originalSql)}");
        }
    }

    private static bool HasTriggeredTable(string sql)
    {
        return TriggeredTables.Any(table =>
            sql.Contains($"[{table}]", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains($" {table} ", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains($" {table}\r", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains($" {table}\n", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains($"UPDATE [{table}]", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains($"INSERT INTO [{table}]", StringComparison.OrdinalIgnoreCase) ||
            sql.Contains($"DELETE FROM [{table}]", StringComparison.OrdinalIgnoreCase)
        );
    }

    private static string GetTableName(string sql)
    {
        var match = Regex.Match(sql, @"(?:UPDATE|INSERT INTO|DELETE FROM)\s+\[?(\w+)\]?", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : "Unknown";
    }
}