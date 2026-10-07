using LocationHeatMapApp.Data;
using LocationHeatMapApp.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace LocationHeatMapApp.Services;

/// <summary>
/// Listens for foreground location changes and persists meaningful movement to SQLite.
/// </summary>
public sealed class LocationTrackingService
{
    private const double MinimumMovementMeters = 5.0;
    private static readonly TimeSpan MinimumSaveInterval = TimeSpan.FromSeconds(5);

    private readonly LocationDatabase _database;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    private Location? _lastSavedLocation;
    private DateTime _lastSavedAtUtc = DateTime.MinValue;

    public LocationTrackingService(LocationDatabase database)
    {
        _database = database;
    }

    public event Action<LocationRecord>? LocationSaved;
    public event Action<string>? TrackingError;

    public bool IsTracking => Geolocation.IsListeningForeground;

    public async Task<bool> StartAsync()
    {
        if (IsTracking)
        {
            return true;
        }

        if (!Geolocation.IsEnabled)
        {
            TrackingError?.Invoke("Location services are disabled on this device.");
            return false;
        }

        var permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (permission != PermissionStatus.Granted)
        {
            permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (permission != PermissionStatus.Granted)
        {
            TrackingError?.Invoke("Location permission was not granted.");
            return false;
        }

        try
        {
            Geolocation.LocationChanged += OnLocationChanged;
            Geolocation.ListeningFailed += OnListeningFailed;

            // High accuracy is appropriate for a walking-route heat map. For a
            // production battery-sensitive app, Medium accuracy may be preferable.
            var request = new GeolocationListeningRequest(GeolocationAccuracy.High);
            var started = await Geolocation.StartListeningForegroundAsync(request);

            if (!started)
            {
                DetachEventHandlers();
                TrackingError?.Invoke("The device could not start foreground location tracking.");
                return false;
            }

            // Save an initial point so the map can center immediately when possible.
            var initialLocation = await Geolocation.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10)));

            if (initialLocation is not null)
            {
                await PersistIfNeededAsync(initialLocation, force: true);
            }

            return true;
        }
        catch (Exception ex)
        {
            Stop();
            TrackingError?.Invoke($"Unable to start location tracking: {ex.Message}");
            return false;
        }
    }

    public void Stop()
    {
        if (Geolocation.IsListeningForeground)
        {
            Geolocation.StopListeningForeground();
        }

        DetachEventHandlers();
    }

    private async void OnLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        try
        {
            await PersistIfNeededAsync(e.Location, force: false);
        }
        catch (Exception ex)
        {
            TrackingError?.Invoke($"Unable to save location: {ex.Message}");
        }
    }

    private void OnListeningFailed(object? sender, GeolocationListeningFailedEventArgs e)
    {
        DetachEventHandlers();
        TrackingError?.Invoke($"Location tracking stopped: {e.Error}");
    }

    private async Task PersistIfNeededAsync(Location location, bool force)
    {
        await _saveLock.WaitAsync();
        try
        {
            var nowUtc = DateTime.UtcNow;

            if (!force && _lastSavedLocation is not null)
            {
                var distanceKm = Location.CalculateDistance(
                    _lastSavedLocation,
                    location,
                    DistanceUnits.Kilometers);

                var distanceMeters = distanceKm * 1000.0;
                var elapsed = nowUtc - _lastSavedAtUtc;

                // Ignore GPS jitter and very frequent duplicate updates.
                if (distanceMeters < MinimumMovementMeters && elapsed < MinimumSaveInterval)
                {
                    return;
                }
            }

            var record = new LocationRecord
            {
                TimestampUtc = nowUtc,
                Latitude = location.Latitude,
                Longitude = location.Longitude
            };

            await _database.SaveLocationAsync(record);

            _lastSavedLocation = new Location(location.Latitude, location.Longitude);
            _lastSavedAtUtc = nowUtc;
            LocationSaved?.Invoke(record);
        }
        finally
        {
            _saveLock.Release();
        }
    }

    private void DetachEventHandlers()
    {
        Geolocation.LocationChanged -= OnLocationChanged;
        Geolocation.ListeningFailed -= OnListeningFailed;
    }
}
