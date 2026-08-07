using Custom2FA_Demo.Abstractions;
using Custom2FA_Demo.Domain;
using Microsoft.Data.Sqlite;

namespace Custom2FA_Demo.Infrastructure;

/// <summary>
/// SQLite 用户 + UserTokens 存储。
/// 启动时 EnsureMigratedAsync：建表 / 增量加列 / 把旧版 Users 上的密钥迁到 UserTokens。
/// </summary>
public sealed class SqliteUserStore : IUserStore, IUserTokenStore, IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SqliteUserStore(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Default") ?? "Data Source=custom2fa.db";
    }

    public async Task EnsureMigratedAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();

            await ExecAsync(conn, """
                CREATE TABLE IF NOT EXISTS __SchemaVersion (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    Version INTEGER NOT NULL
                );
                """);

            var version = await ScalarLongAsync(conn, "SELECT Version FROM __SchemaVersion WHERE Id = 1") ?? 0;

            if (version < 1)
            {
                await MigrateToV1Async(conn);
                await UpsertVersionAsync(conn, 1);
                version = 1;
            }

            if (version < 2)
            {
                await MigrateToV2Async(conn);
                await UpsertVersionAsync(conn, 2);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>V1：基础 Users（兼容最早 Demo）。</summary>
    private static async Task MigrateToV1Async(SqliteConnection conn)
    {
        await ExecAsync(conn, """
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT PRIMARY KEY,
                UserName TEXT NOT NULL UNIQUE COLLATE NOCASE,
                PasswordHash TEXT NOT NULL,
                TwoFactorEnabled INTEGER NOT NULL DEFAULT 0,
                AuthenticatorKey TEXT NULL,
                RecoveryCodes TEXT NULL,
                CreatedAt TEXT NOT NULL
            );
            """);
    }

    /// <summary>
    /// V2：Users 增加邮箱/手机/位标志；新增 UserTokens；
    /// 若旧列还有 AuthenticatorKey/RecoveryCodes，迁入 UserTokens 后保留旧列（兼容读，新写入走 Token 表）。
    /// </summary>
    private static async Task MigrateToV2Async(SqliteConnection conn)
    {
        await AddColumnIfMissingAsync(conn, "Users", "Email", "TEXT NULL");
        await AddColumnIfMissingAsync(conn, "Users", "EmailConfirmed", "INTEGER NOT NULL DEFAULT 0");
        await AddColumnIfMissingAsync(conn, "Users", "PhoneNumber", "TEXT NULL");
        await AddColumnIfMissingAsync(conn, "Users", "PhoneConfirmed", "INTEGER NOT NULL DEFAULT 0");
        await AddColumnIfMissingAsync(conn, "Users", "TwoFactorMethods", "INTEGER NOT NULL DEFAULT 0");

        await ExecAsync(conn, """
            CREATE TABLE IF NOT EXISTS UserTokens (
                UserId TEXT NOT NULL,
                LoginProvider TEXT NOT NULL,
                Name TEXT NOT NULL,
                Value TEXT NULL,
                PRIMARY KEY (UserId, LoginProvider, Name)
            );
            """);

        // 把旧版写在 Users 上的密钥/恢复码迁到 UserTokens（对齐 Identity）
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                SELECT Id, AuthenticatorKey, RecoveryCodes FROM Users
                WHERE (AuthenticatorKey IS NOT NULL AND AuthenticatorKey != '')
                   OR (RecoveryCodes IS NOT NULL AND RecoveryCodes != '')
                """;
            await using var reader = await cmd.ExecuteReaderAsync();
            var rows = new List<(string Id, string? Key, string? Codes)>();
            while (await reader.ReadAsync())
            {
                rows.Add((
                    reader.GetString(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2)));
            }

            await reader.CloseAsync();

            foreach (var (id, key, codes) in rows)
            {
                if (!string.IsNullOrEmpty(key))
                    await UpsertTokenAsync(conn, id, UserTokenNames.InternalLoginProvider, UserTokenNames.AuthenticatorKey, key);
                if (!string.IsNullOrEmpty(codes))
                    await UpsertTokenAsync(conn, id, UserTokenNames.InternalLoginProvider, UserTokenNames.RecoveryCodes, codes);

                // 已启用 2FA 且有密钥时，默认勾上 Authenticator 位
                await using var upd = conn.CreateCommand();
                upd.CommandText = """
                    UPDATE Users
                    SET TwoFactorMethods = CASE
                        WHEN TwoFactorEnabled = 1 AND TwoFactorMethods = 0 THEN 1
                        ELSE TwoFactorMethods END
                    WHERE Id = $id
                    """;
                upd.Parameters.AddWithValue("$id", id);
                await upd.ExecuteNonQueryAsync();
            }
        }
    }

    private static async Task AddColumnIfMissingAsync(SqliteConnection conn, string table, string column, string definition)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return;
        }
        await reader.CloseAsync();
        await ExecAsync(conn, $"ALTER TABLE {table} ADD COLUMN {column} {definition}");
    }

    private static async Task UpsertVersionAsync(SqliteConnection conn, int version)
    {
        await ExecAsync(conn, $"""
            INSERT INTO __SchemaVersion (Id, Version) VALUES (1, {version})
            ON CONFLICT(Id) DO UPDATE SET Version = excluded.Version;
            """);
    }

    private static async Task UpsertTokenAsync(SqliteConnection conn, string userId, string provider, string name, string value)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO UserTokens (UserId, LoginProvider, Name, Value)
            VALUES ($u, $p, $n, $v)
            ON CONFLICT(UserId, LoginProvider, Name) DO UPDATE SET Value = excluded.Value;
            """;
        cmd.Parameters.AddWithValue("$u", userId);
        cmd.Parameters.AddWithValue("$p", provider);
        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$v", value);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<AppUser?> FindByNameAsync(string userName)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, UserName, PasswordHash, Email, EmailConfirmed, PhoneNumber, PhoneConfirmed,
                   TwoFactorEnabled, TwoFactorMethods, CreatedAt
            FROM Users WHERE UserName = $u LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$u", userName);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        var user = MapUser(reader);
        await reader.CloseAsync();
        await LoadTokensAsync(conn, user);
        return user;
    }

    public async Task<AppUser?> FindByIdAsync(Guid id)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, UserName, PasswordHash, Email, EmailConfirmed, PhoneNumber, PhoneConfirmed,
                   TwoFactorEnabled, TwoFactorMethods, CreatedAt
            FROM Users WHERE Id = $id LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$id", id.ToString());
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;
        var user = MapUser(reader);
        await reader.CloseAsync();
        await LoadTokensAsync(conn, user);
        return user;
    }

    public async Task CreateAsync(AppUser user)
    {
        await _gate.WaitAsync();
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO Users (
                    Id, UserName, PasswordHash, Email, EmailConfirmed, PhoneNumber, PhoneConfirmed,
                    TwoFactorEnabled, TwoFactorMethods, CreatedAt)
                VALUES ($id, $u, $p, $e, $ec, $ph, $pc, $tfa, $m, $c)
                """;
            BindUser(cmd, user);
            await cmd.ExecuteNonQueryAsync();
            await PersistTokensAsync(conn, user);
        }
        finally { _gate.Release(); }
    }

    public async Task UpdateAsync(AppUser user)
    {
        await _gate.WaitAsync();
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = """
                UPDATE Users SET
                    UserName=$u, PasswordHash=$p, Email=$e, EmailConfirmed=$ec,
                    PhoneNumber=$ph, PhoneConfirmed=$pc, TwoFactorEnabled=$tfa,
                    TwoFactorMethods=$m, CreatedAt=$c
                WHERE Id=$id
                """;
            BindUser(cmd, user);
            await cmd.ExecuteNonQueryAsync();
            await PersistTokensAsync(conn, user);
        }
        finally { _gate.Release(); }
    }

    public async Task<string?> GetTokenAsync(Guid userId, string loginProvider, string name)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        return await GetTokenCoreAsync(conn, userId.ToString(), loginProvider, name);
    }

    public async Task SetTokenAsync(Guid userId, string loginProvider, string name, string? value)
    {
        await _gate.WaitAsync();
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            if (value is null)
                await RemoveTokenCoreAsync(conn, userId.ToString(), loginProvider, name);
            else
                await UpsertTokenAsync(conn, userId.ToString(), loginProvider, name, value);
        }
        finally { _gate.Release(); }
    }

    public async Task RemoveTokenAsync(Guid userId, string loginProvider, string name)
    {
        await _gate.WaitAsync();
        try
        {
            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync();
            await RemoveTokenCoreAsync(conn, userId.ToString(), loginProvider, name);
        }
        finally { _gate.Release(); }
    }

    private static async Task LoadTokensAsync(SqliteConnection conn, AppUser user)
    {
        user.AuthenticatorKey = await GetTokenCoreAsync(conn, user.Id.ToString(), UserTokenNames.InternalLoginProvider, UserTokenNames.AuthenticatorKey);
        user.RecoveryCodes = await GetTokenCoreAsync(conn, user.Id.ToString(), UserTokenNames.InternalLoginProvider, UserTokenNames.RecoveryCodes);
    }

    private static async Task PersistTokensAsync(SqliteConnection conn, AppUser user)
    {
        if (user.AuthenticatorKey is null)
            await RemoveTokenCoreAsync(conn, user.Id.ToString(), UserTokenNames.InternalLoginProvider, UserTokenNames.AuthenticatorKey);
        else
            await UpsertTokenAsync(conn, user.Id.ToString(), UserTokenNames.InternalLoginProvider, UserTokenNames.AuthenticatorKey, user.AuthenticatorKey);

        if (user.RecoveryCodes is null)
            await RemoveTokenCoreAsync(conn, user.Id.ToString(), UserTokenNames.InternalLoginProvider, UserTokenNames.RecoveryCodes);
        else
            await UpsertTokenAsync(conn, user.Id.ToString(), UserTokenNames.InternalLoginProvider, UserTokenNames.RecoveryCodes, user.RecoveryCodes);
    }

    private static async Task<string?> GetTokenCoreAsync(SqliteConnection conn, string userId, string provider, string name)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Value FROM UserTokens
            WHERE UserId=$u AND LoginProvider=$p AND Name=$n LIMIT 1
            """;
        cmd.Parameters.AddWithValue("$u", userId);
        cmd.Parameters.AddWithValue("$p", provider);
        cmd.Parameters.AddWithValue("$n", name);
        var result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? null : Convert.ToString(result);
    }

    private static async Task RemoveTokenCoreAsync(SqliteConnection conn, string userId, string provider, string name)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM UserTokens WHERE UserId=$u AND LoginProvider=$p AND Name=$n";
        cmd.Parameters.AddWithValue("$u", userId);
        cmd.Parameters.AddWithValue("$p", provider);
        cmd.Parameters.AddWithValue("$n", name);
        await cmd.ExecuteNonQueryAsync();
    }

    private static void BindUser(SqliteCommand cmd, AppUser user)
    {
        cmd.Parameters.AddWithValue("$id", user.Id.ToString());
        cmd.Parameters.AddWithValue("$u", user.UserName);
        cmd.Parameters.AddWithValue("$p", user.PasswordHash);
        cmd.Parameters.AddWithValue("$e", (object?)user.Email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ec", user.EmailConfirmed ? 1 : 0);
        cmd.Parameters.AddWithValue("$ph", (object?)user.PhoneNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$pc", user.PhoneConfirmed ? 1 : 0);
        cmd.Parameters.AddWithValue("$tfa", user.TwoFactorEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$m", (int)user.TwoFactorMethods);
        cmd.Parameters.AddWithValue("$c", user.CreatedAt.ToString("O"));
    }

    private static AppUser MapUser(SqliteDataReader reader) => new()
    {
        Id = Guid.Parse(reader.GetString(0)),
        UserName = reader.GetString(1),
        PasswordHash = reader.GetString(2),
        Email = reader.IsDBNull(3) ? null : reader.GetString(3),
        EmailConfirmed = reader.GetInt64(4) == 1,
        PhoneNumber = reader.IsDBNull(5) ? null : reader.GetString(5),
        PhoneConfirmed = reader.GetInt64(6) == 1,
        TwoFactorEnabled = reader.GetInt64(7) == 1,
        TwoFactorMethods = (TwoFactorMethods)(int)reader.GetInt64(8),
        CreatedAt = DateTime.Parse(reader.GetString(9), null, System.Globalization.DateTimeStyles.RoundtripKind)
    };

    private static async Task ExecAsync(SqliteConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<long?> ScalarLongAsync(SqliteConnection conn, string sql)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        var result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? null : Convert.ToInt64(result);
    }

    public ValueTask DisposeAsync()
    {
        _gate.Dispose();
        return ValueTask.CompletedTask;
    }
}
