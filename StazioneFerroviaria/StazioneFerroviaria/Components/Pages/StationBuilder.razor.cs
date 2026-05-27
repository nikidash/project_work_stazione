using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Text.Json;
using System.Text;
using StazioneFerroviaria.Models.StationBuilder;

namespace StazioneFerroviaria.Components.Pages;

public partial class StationBuilder : ComponentBase
{
    [Inject] private IWebHostEnvironment Env { get; set; } = null!;
    
    private const int GridSpacing = 20;
    private const int GridCols = 50;
    private const int GridRows = 30;

    public StationLayoutData Layout { get; set; } = new();
    public ElementTool SelectedTool { get; set; } = ElementTool.DrawTrack;
    public int ActivePlatformNumber { get; set; } = 1;
    
    public StationNode? ActiveDrawNode { get; set; }

    public string GeneratedJson { get; set; } = string.Empty;
    public string GeneratedSvg { get; set; } = string.Empty;
    public bool ShowSaveSuccess { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var path = Path.Combine(Env.WebRootPath, "station-layout.json");
        if (File.Exists(path))
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var loaded = JsonSerializer.Deserialize<StationLayoutData>(json);
                if (loaded != null) Layout = loaded;
            }
            catch { }
        }
        FixLongSegments();
        UpdateOutputs();
    }

    private void SelectTool(ElementTool tool)
    {
        SelectedTool = tool;
        ActiveDrawNode = null;
    }

    private void OnGridClick(int x, int y)
    {
        if (SelectedTool == ElementTool.DrawTrack || SelectedTool == ElementTool.DrawPlatform || SelectedTool == ElementTool.DrawStorage)
        {
            var clickedNode = GetOrCreateNode(x, y);

            if (ActiveDrawNode == null)
            {
                ActiveDrawNode = clickedNode;
            }
            else
            {
                if (ActiveDrawNode.Id != clickedNode.Id)
                {
                    var points = GetLinePoints(ActiveDrawNode.X, ActiveDrawNode.Y, clickedNode.X, clickedNode.Y);
                    for (int i = 0; i < points.Count - 1; i++)
                    {
                        var nA = GetOrCreateNode(points[i].X, points[i].Y);
                        var nB = GetOrCreateNode(points[i + 1].X, points[i + 1].Y);
                        
                        if (!SegmentExists(nA.Id, nB.Id))
                        {
                            Layout.Segments.Add(new StationSegment
                            {
                                StartNodeId = nA.Id,
                                EndNodeId = nB.Id,
                                IsPlatform = SelectedTool == ElementTool.DrawPlatform,
                                IsStorage = SelectedTool == ElementTool.DrawStorage,
                                PlatformNumber = SelectedTool == ElementTool.DrawPlatform ? ActivePlatformNumber : null
                            });
                        }
                    }
                    ActiveDrawNode = clickedNode; // chain drawing
                    UpdateOutputs();
                }
                else
                {
                    // Clicked same node, stop chaining
                    ActiveDrawNode = null;
                }
            }
        }
        else if (SelectedTool == ElementTool.ToggleSwitch)
        {
            var node = GetNodeAt(x, y);
            if (node != null) 
            { 
                node.IsSwitch = !node.IsSwitch; 
                node.IsCrossing = false; 
                UpdateOutputs(); 
            }
        }
        else if (SelectedTool == ElementTool.ToggleCrossing)
        {
            var node = GetNodeAt(x, y);
            if (node != null) 
            { 
                node.IsCrossing = !node.IsCrossing; 
                node.IsSwitch = false; 
                UpdateOutputs(); 
            }
        }
        else if (SelectedTool == ElementTool.Erase)
        {
            var node = GetNodeAt(x, y);
            if (node != null)
            {
                Layout.Segments.RemoveAll(s => s.StartNodeId == node.Id || s.EndNodeId == node.Id);
                Layout.Nodes.Remove(node);
                UpdateOutputs();
            }
        }
    }

    private void OnSegmentClick(StationSegment segment)
    {
        if (SelectedTool == ElementTool.Erase)
        {
            Layout.Segments.Remove(segment);
            CleanOrphanNodes();
            UpdateOutputs();
        }
    }

    private StationNode GetOrCreateNode(int x, int y)
    {
        var node = Layout.Nodes.FirstOrDefault(n => n.X == x && n.Y == y);
        if (node == null)
        {
            node = new StationNode { X = x, Y = y };
            Layout.Nodes.Add(node);
        }
        return node;
    }

    private StationNode? GetNodeAt(int x, int y) => Layout.Nodes.FirstOrDefault(n => n.X == x && n.Y == y);

    private bool SegmentExists(string id1, string id2)
    {
        return Layout.Segments.Any(s => (s.StartNodeId == id1 && s.EndNodeId == id2) || (s.StartNodeId == id2 && s.EndNodeId == id1));
    }

    private void CleanOrphanNodes()
    {
        Layout.Nodes.RemoveAll(n => !n.IsSwitch && !n.IsCrossing && !Layout.Segments.Any(s => s.StartNodeId == n.Id || s.EndNodeId == n.Id));
    }

    private void UpdateOutputs()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        GeneratedJson = JsonSerializer.Serialize(Layout, options);

        GenerateSvgString();
    }

    private void GenerateSvgString()
    {
        var sb = new StringBuilder();
        int width = GridCols * GridSpacing;
        int height = GridRows * GridSpacing;
        
        sb.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" width=\"100%\" height=\"100%\">");
        sb.AppendLine("  <style>");
        sb.AppendLine("    .track { stroke: #6c757d; stroke-width: 4; stroke-linecap: round; }");
        sb.AppendLine("    .platform { stroke: #28a745; stroke-width: 8; stroke-linecap: round; }");
        sb.AppendLine("    .switch { fill: #ffc107; stroke: #333; stroke-width: 2; }");
        sb.AppendLine("    .crossing { fill: #dc3545; stroke: #333; stroke-width: 2; }");
        sb.AppendLine("    .joint { fill: #333; }");
        sb.AppendLine("  </style>");

        int offset = GridSpacing / 2;

        foreach (var segment in Layout.Segments)
        {
            var startNode = Layout.Nodes.FirstOrDefault(n => n.Id == segment.StartNodeId);
            var endNode = Layout.Nodes.FirstOrDefault(n => n.Id == segment.EndNodeId);
            if (startNode != null && endNode != null)
            {
                var cssClass = segment.IsPlatform ? "platform" : "track";
                sb.AppendLine($"  <line x1=\"{startNode.X * GridSpacing + offset}\" y1=\"{startNode.Y * GridSpacing + offset}\" x2=\"{endNode.X * GridSpacing + offset}\" y2=\"{endNode.Y * GridSpacing + offset}\" class=\"{cssClass}\" />");
            }
        }

        foreach (var node in Layout.Nodes)
        {
            int px = node.X * GridSpacing + offset;
            int py = node.Y * GridSpacing + offset;
            
            if (node.IsSwitch)
            {
                sb.AppendLine($"  <circle cx=\"{px}\" cy=\"{py}\" r=\"6\" class=\"switch\" />");
            }
            else if (node.IsCrossing)
            {
                sb.AppendLine($"  <circle cx=\"{px}\" cy=\"{py}\" r=\"6\" class=\"crossing\" />");
            }
            else
            {
                sb.AppendLine($"  <circle cx=\"{px}\" cy=\"{py}\" r=\"3\" class=\"joint\" />");
            }
        }

        sb.AppendLine("</svg>");
        GeneratedSvg = sb.ToString();
    }

    private void ClearGrid()
    {
        Layout.Nodes.Clear();
        Layout.Segments.Clear();
        ActiveDrawNode = null;
        UpdateOutputs();
    }
    
    // UI Helpers
    private StationNode? GetNode(string id) => Layout.Nodes.FirstOrDefault(n => n.Id == id);

    private List<(int X, int Y)> GetLinePoints(int x0, int y0, int x1, int y1)
    {
        var points = new List<(int X, int Y)>();
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            points.Add((x0, y0));
            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
        return points;
    }

    private void FixLongSegments()
    {
        bool changed = false;
        var existingSegments = Layout.Segments.ToList();
        
        foreach (var segment in existingSegments)
        {
            if (segment.IsPlatform && !segment.PlatformNumber.HasValue)
            {
                segment.PlatformNumber = 1;
                changed = true;
            }

            var n1 = GetNode(segment.StartNodeId);
            var n2 = GetNode(segment.EndNodeId);
            if (n1 == null || n2 == null) continue;

            if (Math.Abs(n1.X - n2.X) > 1 || Math.Abs(n1.Y - n2.Y) > 1)
            {
                changed = true;
                Layout.Segments.Remove(segment);
                
                var points = GetLinePoints(n1.X, n1.Y, n2.X, n2.Y);
                for (int i = 0; i < points.Count - 1; i++)
                {
                    var pA = GetOrCreateNode(points[i].X, points[i].Y);
                    var pB = GetOrCreateNode(points[i + 1].X, points[i + 1].Y);
                    
                    if (!SegmentExists(pA.Id, pB.Id))
                    {
                        Layout.Segments.Add(new StationSegment
                        {
                            StartNodeId = pA.Id,
                            EndNodeId = pB.Id,
                            IsPlatform = segment.IsPlatform,
                            PlatformNumber = segment.PlatformNumber
                        });
                    }
                }
            }
        }
        
        if (changed)
        {
            _ = SaveLayout();
        }
    }

    private async Task SaveLayout()
    {
        var path = Path.Combine(Env.WebRootPath, "station-layout.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(Layout, options);
        await File.WriteAllTextAsync(path, json);
        
        ShowSaveSuccess = true;
        StateHasChanged();
        
        // Hide message after 3 seconds
        await Task.Delay(3000);
        ShowSaveSuccess = false;
        StateHasChanged();
    }
}
