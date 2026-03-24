using Microsoft.Data.Sqlite;
using Dapper;
using Microsoft.Extensions.Logging;

namespace ILinkBotSDK.Storage;

/// <summary>
/// SQLite state storage implementation
/// </summary>
public class SqliteStateStorage : IStateStorage, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteStateStorage>? _logger;
    private SqliteConnection? _connection;
    private bool _disposed;

    public SqliteStateStorage(string databasePath, ILogger<SqliteStateStorage>? logger = null)
    {
        _connectionString = $"Data Source={databasePath}";
        _logger = logger;
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // Create BotState table
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS BotState (
                Id INTEGER PRIMARY KEY,
                BotId TEXT NOT NULL,
                UserId TEXT,
                Token TEXT NOT NULL,
                BaseUrl TEXT NOT NULL,
                CdnBaseUrl TEXT,
                GetUpdatesBuf TEXT,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )");

        // Create ContextTokens table (with UserId unique constraint)
        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS ContextTokens (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL UNIQUE,
                ContextToken TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            )");

        // Ensure UNIQUE constraint exists (for backward compatibility)
        try
        {
            connection.Execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_context_tokens_userid ON ContextTokens(UserId)");
        }
        catch
        {
            // Index already exists or creation failed, ignore
        }

        _logger?.LogInformation("SQLite database initialized at {Path}", _connectionString);
    }

    private SqliteConnection GetConnection()
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
        {
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
        }
        return _connection;
    }

    public async Task<BotStateData?> GetBotStateAsync()
    {
        var connection = GetConnection();
        var result = await connection.QueryFirstOrDefaultAsync<BotStateData>(
            "SELECT BotId, UserId, Token, BaseUrl, CdnBaseUrl, GetUpdatesBuf, CreatedAt, UpdatedAt FROM BotState WHERE Id = 1");
        return result;
    }

    public async Task SaveBotStateAsync(BotStateData state)
    {
        var connection = GetConnection();
        var now = DateTime.UtcNow.ToString("o");

        // Use UPSERT syntax
        await connection.ExecuteAsync(@"
            INSERT INTO BotState (Id, BotId, UserId, Token, BaseUrl, CdnBaseUrl, GetUpdatesBuf, CreatedAt, UpdatedAt)
            VALUES (1, @BotId, @UserId, @Token, @BaseUrl, @CdnBaseUrl, @GetUpdatesBuf, @CreatedAt, @UpdatedAt)
            ON CONFLICT(Id) DO UPDATE SET
                BotId = @BotId,
                UserId = @UserId,
                Token = @Token,
                BaseUrl = @BaseUrl,
                CdnBaseUrl = @CdnBaseUrl,
                GetUpdatesBuf = @GetUpdatesBuf,
                UpdatedAt = @UpdatedAt",
            new
            {
                state.BotId,
                state.UserId,
                state.Token,
                state.BaseUrl,
                state.CdnBaseUrl,
                state.GetUpdatesBuf,
                CreatedAt = now,
                UpdatedAt = now
            });

        _logger?.LogDebug("Bot state saved");
    }

    public async Task DeleteBotStateAsync()
    {
        var connection = GetConnection();
        await connection.ExecuteAsync("DELETE FROM BotState WHERE Id = 1");
        _logger?.LogDebug("Bot state deleted");
    }

    public async Task<string?> GetContextTokenAsync(string userId)
    {
        var connection = GetConnection();
        var result = await connection.QueryFirstOrDefaultAsync<ContextTokenData>(
            "SELECT UserId, ContextToken, CreatedAt, UpdatedAt FROM ContextTokens WHERE UserId = @UserId",
            new { UserId = userId });
        return result?.ContextToken;
    }

    public async Task SaveContextTokenAsync(string userId, string contextToken)
    {
        var connection = GetConnection();
        var now = DateTime.UtcNow.ToString("o");

        await connection.ExecuteAsync(@"
            INSERT INTO ContextTokens (UserId, ContextToken, CreatedAt, UpdatedAt)
            VALUES (@UserId, @ContextToken, @CreatedAt, @UpdatedAt)
            ON CONFLICT(UserId) DO UPDATE SET
                ContextToken = @ContextToken,
                UpdatedAt = @UpdatedAt",
            new
            {
                UserId = userId,
                ContextToken = contextToken,
                CreatedAt = now,
                UpdatedAt = now
            });

        _logger?.LogDebug("Context token saved for user {UserId}", userId);
    }

    public async Task DeleteContextTokenAsync(string userId)
    {
        var connection = GetConnection();
        await connection.ExecuteAsync("DELETE FROM ContextTokens WHERE UserId = @UserId",
            new { UserId = userId });
        _logger?.LogDebug("Context token deleted for user {UserId}", userId);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }
}
