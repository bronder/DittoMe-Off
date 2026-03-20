using System.IO;
using DittoMeOff.Models;
using Microsoft.Data.Sqlite;

namespace DittoMeOff.Services;

public class DatabaseService : IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public DatabaseService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DittoMeOff"
        );
        
        Directory.CreateDirectory(appDataPath);
        _dbPath = Path.Combine(appDataPath, "clipboard.db");
    }

    public void Initialize()
    {
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();
        
        var createTableSql = @"
            CREATE TABLE IF NOT EXISTS ClipboardItems (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Content TEXT NOT NULL,
                ContentType INTEGER NOT NULL,
                FormatType INTEGER DEFAULT 0,
                Timestamp INTEGER NOT NULL,
                IsPinned INTEGER DEFAULT 0,
                AppSource TEXT,
                Size INTEGER NOT NULL,
                PreviewText TEXT,
                ImageData BLOB
            );
            
            CREATE INDEX IF NOT EXISTS idx_timestamp ON ClipboardItems(Timestamp DESC);
            CREATE INDEX IF NOT EXISTS idx_pinned ON ClipboardItems(IsPinned);
        ";
        
        using var cmd = new SqliteCommand(createTableSql, _connection);
        cmd.ExecuteNonQuery();

        // Add FormatType column if it doesn't exist (migration for existing databases)
        try
        {
            var alterSql = "ALTER TABLE ClipboardItems ADD COLUMN FormatType INTEGER DEFAULT 0";
            using var alterCmd = new SqliteCommand(alterSql, _connection);
            alterCmd.ExecuteNonQuery();
        }
        catch
        {
            // Column already exists, ignore
        }
    }

    public long InsertItem(ClipboardItem item)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var sql = @"
            INSERT INTO ClipboardItems (Content, ContentType, FormatType, Timestamp, IsPinned, AppSource, Size, PreviewText, ImageData)
            VALUES (@Content, @ContentType, @FormatType, @Timestamp, @IsPinned, @AppSource, @Size, @PreviewText, @ImageData);
            SELECT last_insert_rowid();
        ";
        
        using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@Content", item.Content);
        cmd.Parameters.AddWithValue("@ContentType", (int)item.ContentType);
        cmd.Parameters.AddWithValue("@FormatType", (int)item.FormatType);
        cmd.Parameters.AddWithValue("@Timestamp", item.Timestamp.Ticks);
        cmd.Parameters.AddWithValue("@IsPinned", item.IsPinned ? 1 : 0);
        cmd.Parameters.AddWithValue("@AppSource", item.AppSource ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@Size", item.Size);
        cmd.Parameters.AddWithValue("@PreviewText", item.PreviewText ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("@ImageData", item.ImageData ?? (object)DBNull.Value);
        
        var result = cmd.ExecuteScalar();
        return Convert.ToInt64(result);
    }

    public List<ClipboardItem> GetItems(int limit = 100)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var items = new List<ClipboardItem>();
        var sql = @"
            SELECT Id, Content, ContentType, FormatType, Timestamp, IsPinned, AppSource, Size, PreviewText, ImageData
            FROM ClipboardItems
            ORDER BY IsPinned DESC, Timestamp DESC
            LIMIT @Limit
        ";
        
        using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@Limit", limit);
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            items.Add(new ClipboardItem
            {
                Id = reader.GetInt64(0),
                Content = reader.GetString(1),
                ContentType = (ContentType)reader.GetInt32(2),
                FormatType = (ContentFormatType)reader.GetInt32(3),
                Timestamp = new DateTime(reader.GetInt64(4)),
                IsPinned = reader.GetInt32(5) == 1,
                AppSource = reader.IsDBNull(6) ? null : reader.GetString(6),
                Size = reader.GetInt64(7),
                PreviewText = reader.IsDBNull(8) ? null : reader.GetString(8),
                ImageData = reader.IsDBNull(9) ? null : (byte[])reader[9]
            });
        }
        
        return items;
    }

    public void DeleteItem(long id)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var sql = "DELETE FROM ClipboardItems WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public void TogglePin(long id)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var sql = "UPDATE ClipboardItems SET IsPinned = NOT IsPinned WHERE Id = @Id";
        using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public void ClearHistory(bool keepPinned = true)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var sql = keepPinned 
            ? "DELETE FROM ClipboardItems WHERE IsPinned = 0"
            : "DELETE FROM ClipboardItems";
            
        using var cmd = new SqliteCommand(sql, _connection);
        cmd.ExecuteNonQuery();
    }

    public void ClearItemsOlderThan(int days, bool keepPinned = true)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cutoffTime = DateTime.Now.AddDays(-days).Ticks;
        
        if (keepPinned)
        {
            var sql = "DELETE FROM ClipboardItems WHERE IsPinned = 0 AND Timestamp < @CutoffTime";
            using var cmd = new SqliteCommand(sql, _connection);
            cmd.Parameters.AddWithValue("@CutoffTime", cutoffTime);
            cmd.ExecuteNonQuery();
        }
        else
        {
            var sql = "DELETE FROM ClipboardItems WHERE Timestamp < @CutoffTime";
            using var cmd = new SqliteCommand(sql, _connection);
            cmd.Parameters.AddWithValue("@CutoffTime", cutoffTime);
            cmd.ExecuteNonQuery();
        }
    }

    public int GetItemCount()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var sql = "SELECT COUNT(*) FROM ClipboardItems";
        using var cmd = new SqliteCommand(sql, _connection);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
