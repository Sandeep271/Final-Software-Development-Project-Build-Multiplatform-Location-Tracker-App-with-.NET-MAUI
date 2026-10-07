using LocationHeatMapApp.Models;
using SQLite;

namespace LocationHeatMapApp.Data;

/// <summary>
/// Provides asynchronous access to the app's SQLite location database.
/// </summary>
public sealed class LocationDatabase
{
    private const string DatabaseFileName = "locations.db3";
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private SQLiteAsyncConnection? _database;

    private static string DatabasePath =>
        Path.Combine(FileSystem.Current.AppDataDirectory, DatabaseFileName);

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (_database is not null)
        {
            return _database;
        }

        await _initializationLock.WaitAsync();
        try
        {
            if (_database is null)
            {
                var flags = SQLiteOpenFlags.ReadWrite |
                            SQLiteOpenFlags.Create |
                            SQLiteOpenFlags.SharedCache;

                _database = new SQLiteAsyncConnection(DatabasePath, flags);
                await _database.CreateTableAsync<LocationRecord>();
            }
        }
        finally
        {
            _initializationLock.Release();
        }

        return _database;
    }

    public async Task<int> SaveLocationAsync(LocationRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var database = await GetDatabaseAsync();
        return await database.InsertAsync(record);
    }

    public async Task<List<LocationRecord>> GetLocationsAsync()
    {
        var database = await GetDatabaseAsync();
        return await database.Table<LocationRecord>()
            .OrderBy(point => point.TimestampUtc)
            .ToListAsync();
    }

    public async Task<int> DeleteAllAsync()
    {
        var database = await GetDatabaseAsync();
        return await database.DeleteAllAsync<LocationRecord>();
    }
}
