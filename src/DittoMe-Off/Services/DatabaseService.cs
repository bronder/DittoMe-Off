using System.IO;
using DittoMeOff.Models;
using Microsoft.Data.Sqlite;
using NLog;

namespace DittoMeOff.Services;

public class DatabaseService : IDatabaseService
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public DatabaseService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.DatabaseFolderName
        );
        
        Directory.CreateDirectory(appDataPath);
        _dbPath = Path.Combine(appDataPath, AppConstants.DatabaseFileName);
        
        _logger.Debug("Database path: {DbPath}", _dbPath);
    }

    public void Initialize()
    {
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();
        
        _logger.Info("Database connection opened");
        
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
        
        _logger.Debug("Database tables initialized");

        // Add FormatType column if it doesn't exist (migration for existing databases)
        try
        {
            using var alterCmd = new SqliteCommand(AppConstants.SqlStatements.AlterTableAddFormatType, _connection);
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

        using var cmd = new SqliteCommand(AppConstants.SqlStatements.InsertItem + "; SELECT last_insert_rowid();", _connection);
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
        
        using var cmd = new SqliteCommand(AppConstants.SqlStatements.GetItems, _connection);
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

        using var cmd = new SqliteCommand(AppConstants.SqlStatements.DeleteItem, _connection);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public void TogglePin(long id)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        using var cmd = new SqliteCommand(AppConstants.SqlStatements.TogglePin, _connection);
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.ExecuteNonQuery();
    }

    public void ClearHistory(bool keepPinned = true)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var sql = keepPinned 
            ? AppConstants.SqlStatements.ClearHistoryKeepPinned
            : AppConstants.SqlStatements.ClearHistoryAll;
            
        using var cmd = new SqliteCommand(sql, _connection);
        cmd.ExecuteNonQuery();
    }

    public void ClearItemsOlderThan(int days, bool keepPinned = true)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        var cutoffTime = DateTime.Now.AddDays(-days).Ticks;
        
        var sql = keepPinned
            ? AppConstants.SqlStatements.ClearItemsOlderThanKeepPinned
            : AppConstants.SqlStatements.ClearItemsOlderThanAll;
            
        using var cmd = new SqliteCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@CutoffTime", cutoffTime);
        cmd.ExecuteNonQuery();
    }

    public int GetItemCount()
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        using var cmd = new SqliteCommand(AppConstants.SqlStatements.GetItemCount, _connection);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void DeleteOldestExcessItems(int keepCount, bool keepPinned = true)
    {
        if (_connection == null) throw new InvalidOperationException("Database not initialized");

        if (keepPinned)
        {
            using var cmd = new SqliteCommand(AppConstants.SqlStatements.DeleteOldestExcessItems, _connection);
            cmd.Parameters.AddWithValue("@KeepCount", keepCount);
            cmd.ExecuteNonQuery();
        }
        else
        {
            // If not keeping pinned, just use the keepCount directly on all items
            var sql = @"DELETE FROM ClipboardItems 
                        WHERE Id NOT IN (
                            SELECT Id FROM ClipboardItems 
                            ORDER BY Timestamp DESC 
                            LIMIT @KeepCount
                        )";
            using var cmd = new SqliteCommand(sql, _connection);
            cmd.Parameters.AddWithValue("@KeepCount", keepCount);
            cmd.ExecuteNonQuery();
        }
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
        _logger.Debug("Database connection closed");
    }
}
