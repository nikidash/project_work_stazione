namespace StazioneFerroviaria.Models.StationBuilder;

public enum ElementTool
{
    DrawTrack,
    DrawPlatform,
    DrawStorage,
    ToggleSwitch,
    ToggleCrossing,
    Erase
}

public class StationNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsSwitch { get; set; }
    public bool IsCrossing { get; set; }
}

public class StationSegment
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string StartNodeId { get; set; } = string.Empty;
    public string EndNodeId { get; set; } = string.Empty;
    public bool IsPlatform { get; set; }
    public bool IsStorage { get; set; }
    public int? PlatformNumber { get; set; }
}

public class StationLayoutData
{
    public List<StationNode> Nodes { get; set; } = new();
    public List<StationSegment> Segments { get; set; } = new();
}
