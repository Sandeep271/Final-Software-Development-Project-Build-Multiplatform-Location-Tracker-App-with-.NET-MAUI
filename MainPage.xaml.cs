using LocationHeatMapApp.Data;
using LocationHeatMapApp.Models;
using LocationHeatMapApp.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Maps;

namespace LocationHeatMapApp;

public partial class MainPage : ContentPage
{
    private readonly LocationDatabase _database;
    private readonly LocationTrackingService _trackingService;
    private readonly HeatMapService _heatMapService;
    private DateTime _lastMapRefreshUtc = DateTime.MinValue;

    public MainPage(
        LocationDatabase database,
        LocationTrackingService trackingService,
        HeatMapService heatMapService)
    {
        InitializeComponent();

        _database = database;
        _trackingService = trackingService;
        _heatMapService = heatMapService;

        _trackingService.LocationSaved += OnLocationSaved;
        _trackingService.TrackingError += OnTrackingError;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshHeatMapAsync();
    }

    private async void OnStartClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Requesting location permission...";
        var started = await _trackingService.StartAsync();

        StatusLabel.Text = started
            ? "Tracking is active. Your positions are being saved to SQLite."
            : "Tracking could not be started. Check permissions and location services.";

        if (started)
        {
            await RefreshHeatMapAsync();
        }
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
        _trackingService.Stop();
        StatusLabel.Text = "Tracking stopped. Saved points remain in SQLite.";
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        await RefreshHeatMapAsync();
    }

    private async void OnClearClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Clear location history",
            "Delete every saved location point from this device?",
            "Delete",
            "Cancel");

        if (!confirmed)
        {
            return;
        }

        await _database.DeleteAllAsync();
        LocationMap.MapElements.Clear();
        StatusLabel.Text = "Saved location history was cleared.";
    }

    private void OnLocationSaved(LocationRecord record)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusLabel.Text =
                $"Saved {record.Latitude:F5}, {record.Longitude:F5} at {record.TimestampUtc:HH:mm:ss} UTC";

            // Redrawing on every GPS event can be expensive. Throttle map refreshes.
            if (DateTime.UtcNow - _lastMapRefreshUtc >= TimeSpan.FromSeconds(5))
            {
                _ = RefreshHeatMapAsync();
            }
        });
    }

    private void OnTrackingError(string message)
    {
        MainThread.BeginInvokeOnMainThread(() => StatusLabel.Text = message);
    }

    private async Task RefreshHeatMapAsync()
    {
        var points = await _database.GetLocationsAsync();

        LocationMap.MapElements.Clear();
        foreach (var element in _heatMapService.BuildHeatMap(points))
        {
            LocationMap.MapElements.Add(element);
        }

        if (points.Count == 0)
        {
            StatusLabel.Text = _trackingService.IsTracking
                ? "Tracking is active. Waiting for the first usable location..."
                : "No saved locations yet.";
            return;
        }

        MoveMapToFit(points);
        _lastMapRefreshUtc = DateTime.UtcNow;

        StatusLabel.Text =
            $"Showing {points.Count} saved location point{(points.Count == 1 ? string.Empty : "s")}.";
    }

    private void MoveMapToFit(IReadOnlyList<LocationRecord> points)
    {
        var center = new Location(
            points.Average(point => point.Latitude),
            points.Average(point => point.Longitude));

        var maxDistanceKm = points
            .Select(point => Location.CalculateDistance(
                center,
                new Location(point.Latitude, point.Longitude),
                DistanceUnits.Kilometers))
            .DefaultIfEmpty(0.0)
            .Max();

        // Ensure a useful zoom even when only one or two nearby points exist.
        var radiusKm = Math.Clamp(maxDistanceKm * 1.35, 0.25, 100.0);
        var span = MapSpan.FromCenterAndRadius(center, Distance.FromKilometers(radiusKm));
        LocationMap.MoveToRegion(span);
    }
}
