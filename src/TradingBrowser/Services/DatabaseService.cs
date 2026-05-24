using Microsoft.Data.Sqlite;
using System;
using System.IO;
using TradingBrowser.Helpers;

namespace TradingBrowser.Services;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        Directory.CreateDirectory(PathHelper.UserDataFolder);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = PathHelper.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true
        }.ToString();

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            // Enable WAL mode for better concurrent read/write performance
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA journal_mode=WAL;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Settings (Key TEXT PRIMARY KEY, Value TEXT);
                CREATE TABLE IF NOT EXISTS History (Id INTEGER PRIMARY KEY AUTOINCREMENT, Url TEXT, Title TEXT, VisitTime DATETIME);
                CREATE TABLE IF NOT EXISTS Bookmarks (Id INTEGER PRIMARY KEY AUTOINCREMENT, Url TEXT, Title TEXT, Position INTEGER);
                CREATE TABLE IF NOT EXISTS Sessions (Id INTEGER PRIMARY KEY AUTOINCREMENT, TabId TEXT, Url TEXT, Title TEXT, IsActive BOOLEAN);
            ";
            cmd.ExecuteNonQuery();
            
            LoggingService.Log("Database initialized successfully.");
        }
        catch (Exception ex)
        {
            LoggingService.Error("Failed to initialize database", ex);
        }
    }

    public SqliteConnection GetConnection() => new SqliteConnection(_connectionString);
}
