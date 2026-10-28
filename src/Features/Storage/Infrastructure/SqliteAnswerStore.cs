using Microsoft.Data.Sqlite;
using Shared.Contracts;

namespace Storage.Infrastructure;

public sealed class SqliteAnswerStore(IConnectionFactory connections) : IAnswerStore
{
    public Task AddAttemptAsync(int year, int day, int part, AnswerAttempt attempt, CancellationToken cancellationToken) =>
        ExecuteAsync(
            "INSERT INTO answer_attempts (year, day, part, answer, outcome, at) VALUES ($y, $d, $p, $a, $o, $at)",
            cancellationToken,
            ("$y", year), ("$d", day), ("$p", part), ("$a", attempt.Answer), ("$o", attempt.Outcome), ("$at", UnixMs.From(attempt.At)));

    public async Task<IReadOnlyList<AnswerAttempt>> AttemptsAsync(int year, int day, int part, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT answer, outcome, at FROM answer_attempts WHERE year = $y AND day = $d AND part = $p ORDER BY id";
        Bind(cmd, ("$y", year), ("$d", day), ("$p", part));
        var attempts = new List<AnswerAttempt>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            attempts.Add(new AnswerAttempt(reader.GetString(0), reader.GetString(1), UnixMs.To(reader.GetInt64(2))));
        }

        return attempts;
    }

    public Task SetCooldownAsync(int year, int day, int part, DateTimeOffset until, CancellationToken cancellationToken) =>
        ExecuteAsync(
            "INSERT INTO answer_cooldowns (year, day, part, until) VALUES ($y, $d, $p, $u) ON CONFLICT(year, day, part) DO UPDATE SET until = excluded.until",
            cancellationToken,
            ("$y", year), ("$d", day), ("$p", part), ("$u", UnixMs.From(until)));

    public async Task<DateTimeOffset?> CooldownUntilAsync(int year, int day, int part, CancellationToken cancellationToken) =>
        await ScalarAsync("SELECT until FROM answer_cooldowns WHERE year = $y AND day = $d AND part = $p", year, day, part, cancellationToken) is long ms
            ? UnixMs.To(ms)
            : null;

    public Task SetKnownAnswerAsync(int year, int day, int part, string answer, CancellationToken cancellationToken) =>
        ExecuteAsync(
            "INSERT INTO known_answers (year, day, part, answer) VALUES ($y, $d, $p, $a) ON CONFLICT(year, day, part) DO UPDATE SET answer = excluded.answer",
            cancellationToken,
            ("$y", year), ("$d", day), ("$p", part), ("$a", answer));

    public async Task<string?> KnownAnswerAsync(int year, int day, int part, CancellationToken cancellationToken) =>
        await ScalarAsync("SELECT answer FROM known_answers WHERE year = $y AND day = $d AND part = $p", year, day, part, cancellationToken) as string;

    private async Task<object?> ScalarAsync(string sql, int year, int day, int part, CancellationToken cancellationToken)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        Bind(cmd, ("$y", year), ("$d", day), ("$p", part));
        return await cmd.ExecuteScalarAsync(cancellationToken);
    }

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken, params (string Name, object Value)[] values)
    {
        await using var connection = await connections.OpenAsync(cancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        Bind(cmd, values);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Bind(SqliteCommand cmd, params (string Name, object Value)[] values)
    {
        foreach (var (name, value) in values)
        {
            cmd.Parameters.AddWithValue(name, value);
        }
    }
}
