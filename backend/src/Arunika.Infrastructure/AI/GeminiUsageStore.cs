using Npgsql;

namespace Arunika.Infrastructure.AI;

/// <summary>
/// Durable per-model daily call counter for the Gemini free tier.
///
/// The free-tier RPD allowance is billed per API key against a calendar day in
/// US/Pacific, so an in-process counter is not enough to respect it: a redeploy
/// reset the pipeline's view of the day to zero while Google kept counting,
/// which is how the console ended up reporting 501/500 on a model this app
/// believed still had budget. Holding the count in Postgres survives restarts
/// and is shared by every instance pointed at the same database.
/// </summary>
public sealed class GeminiUsageStore(string connectionString)
{
    /// <summary>
    /// Reserves one call against <paramref name="model"/>'s allowance for
    /// <paramref name="date"/> and returns the resulting count, or <c>null</c>
    /// when the allowance was already spent and nothing was reserved.
    /// </summary>
    /// <remarks>
    /// The limit check lives in the conditional UPDATE rather than in a separate
    /// SELECT so that check-and-spend is one atomic statement: concurrent
    /// enrichment workers cannot both clear a check that only one of them has
    /// budget for.
    /// </remarks>
    public async Task<int?> TryReserveAsync(
        string model,
        DateOnly date,
        int dailyLimit,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO gemini_model_usage (model, usage_date, call_count)
            VALUES (@model, @usage_date, 1)
            ON CONFLICT (model, usage_date) DO UPDATE
                SET call_count = gemini_model_usage.call_count + 1
                WHERE gemini_model_usage.call_count < @daily_limit
            RETURNING call_count;
            """;

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("model", model);
        command.Parameters.AddWithValue("usage_date", date);
        command.Parameters.AddWithValue("daily_limit", dailyLimit);

        var reserved = await command.ExecuteScalarAsync(cancellationToken);
        return reserved as int?;
    }

    /// <summary>Calls already spent today, for diagnostics and cache warm-up.</summary>
    public async Task<int> GetCountAsync(string model, DateOnly date, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT call_count FROM gemini_model_usage WHERE model = @model AND usage_date = @usage_date;";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("model", model);
        command.Parameters.AddWithValue("usage_date", date);

        return await command.ExecuteScalarAsync(cancellationToken) as int? ?? 0;
    }
}
