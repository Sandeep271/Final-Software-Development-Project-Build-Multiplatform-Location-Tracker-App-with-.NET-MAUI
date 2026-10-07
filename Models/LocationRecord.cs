using SQLite;

namespace LocationHeatMapApp.Models;

/// <summary>
/// Represents one recorded device position stored in SQLite.
/// </summary>
[Table("LocationPoints")]
public sealed class LocationRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime TimestampUtc { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}
