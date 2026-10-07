using LocationHeatMapApp.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Maps;

namespace LocationHeatMapApp.Services;

/// <summary>
/// Converts saved positions into map overlays that approximate a heat map.
/// Points are grouped into geographic grid cells; denser cells are drawn
/// larger and with warmer colors.
/// </summary>
public sealed class HeatMapService
{
    // Roughly 110 meters north/south at mid-latitudes. The exact east/west
    // distance varies with latitude, which is sufficient for this visualization.
    private const double BucketSizeDegrees = 0.001;
    private const int HotBucketPointCount = 8;

    public IReadOnlyList<MapElement> BuildHeatMap(IReadOnlyList<LocationRecord> points)
    {
        if (points.Count == 0)
        {
            return Array.Empty<MapElement>();
        }

        var elements = new List<MapElement>();

        // A subtle route line helps show the order in which the device moved.
        if (points.Count > 1)
        {
            var route = new Polyline
            {
                StrokeColor = Color.FromArgb("#884F46E5"),
                StrokeWidth = 4
            };

            foreach (var point in points.OrderBy(point => point.TimestampUtc))
            {
                route.Geopath.Add(new Location(point.Latitude, point.Longitude));
            }

            elements.Add(route);
        }

        var buckets = points
            .GroupBy(point => new
            {
                Latitude = Quantize(point.Latitude),
                Longitude = Quantize(point.Longitude)
            })
            .Select(group => new
            {
                group.Key.Latitude,
                group.Key.Longitude,
                Count = group.Count()
            })
            .OrderBy(bucket => bucket.Count)
            .ToList();

        foreach (var bucket in buckets)
        {
            var intensity = Math.Clamp(
                bucket.Count / (double)HotBucketPointCount,
                0.0,
                1.0);

            var radiusMeters = 45 + (95 * intensity);
            var fillColor = GetHeatColor(bucket.Count);

            elements.Add(new Circle
            {
                Center = new Location(bucket.Latitude, bucket.Longitude),
                Radius = new Distance(radiusMeters),
                FillColor = fillColor,
                StrokeColor = WithAlpha(fillColor, 0.78f),
                StrokeWidth = 1
            });
        }

        return elements;
    }

    private static double Quantize(double coordinate) =>
        Math.Round(coordinate / BucketSizeDegrees) * BucketSizeDegrees;

    private static Color GetHeatColor(int count) => count switch
    {
        <= 1 => Color.FromArgb("#663498DB"), // Blue - low density
        <= 3 => Color.FromArgb("#665AC8FA"), // Cyan
        <= 5 => Color.FromArgb("#6666BB6A"), // Green
        <= 7 => Color.FromArgb("#66FFCA28"), // Yellow
        _ => Color.FromArgb("#66E53935")     // Red - high density
    };

    private static Color WithAlpha(Color color, float alpha) =>
        new(color.Red, color.Green, color.Blue, alpha);
}
