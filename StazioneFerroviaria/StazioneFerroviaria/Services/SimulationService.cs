using FerroviaApi.Models;
using StazioneFerroviaria.Models.Simulation;
using StazioneFerroviaria.Models.StationBuilder;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace StazioneFerroviaria.Services;

public class SimulationService
{
    private readonly object _lock = new();
    private readonly Random _random = new();
    private readonly IWebHostEnvironment _env;

    public int TickCount { get; private set; }

    public StationLayoutData Layout { get; private set; } = new();
    
    // Mappa SegmentId -> SimZone (un segmento grafico è una zona logica per 2 carrozze)
    public Dictionary<string, SimZone> GlobalZones { get; private set; } = new();
    
    // Treni attivi e le loro rotte calcolate dinamicamente
    public List<ActiveTrainRoute> ActiveRoutes { get; private set; } = new();

    private readonly List<WaitingEntry> _waitingQueue = new();
    private readonly Dictionary<int, Train> _dbTrainById = new();
    private readonly List<DepartedEntry> _departedTrains = new();
    private readonly HashSet<int> _completedDbIds = new();

    private sealed class WaitingEntry
    {
        public Train DbTrain { get; init; } = null!;
        public DateTime QueuedAt { get; init; }
    }

    public sealed class DepartedEntry
    {
        public Train DbTrain { get; init; } = null!;
        public DateTime ActualArrival { get; init; }
        public DateTime ActualDepart { get; init; }
        public int? Platform { get; init; }
    }

    public class ActiveTrainRoute
    {
        public SimTrain Train { get; set; } = null!;
        public List<SimZone> PathZones { get; set; } = new();
    }

    public static int GetTrainPriority(string trainType) => trainType switch
    {
        "T" => 5,
        "F" => 4,
        "V" => 3,
        "R" => 2,
        "S" => 1,
        _ => 0
    };

    public IReadOnlyList<(Train Train, DateTime QueuedAt)> GetWaitingQueue()
    {
        lock (_lock) return _waitingQueue.Select(w => (w.DbTrain, w.QueuedAt)).ToList();
    }

    public IReadOnlyList<DepartedEntry> DrainDepartedTrains()
    {
        lock (_lock)
        {
            if (_departedTrains.Count == 0) return Array.Empty<DepartedEntry>();
            var snap = _departedTrains.ToList();
            _departedTrains.Clear();
            return snap;
        }
    }

    public SimulationService(IWebHostEnvironment env)
    {
        _env = env;
        LoadLayout();
    }

    public void LoadLayout()
    {
        var path = Path.Combine(_env.WebRootPath, "station-layout.json");
        if (File.Exists(path))
        {
            try
            {
                var json = File.ReadAllText(path);
                var layout = JsonSerializer.Deserialize<StationLayoutData>(json);
                if (layout != null)
                {
                    Layout = layout;
                }
            }
            catch { }
        }

        GlobalZones.Clear();
        int zId = 1;
        foreach (var seg in Layout.Segments)
        {
            GlobalZones[seg.Id] = new SimZone 
            { 
                Id = zId++, 
                SegmentId = seg.Id, 
                IsOccupied = false 
            };
        }
    }

    public void Tick()
    {
        lock (_lock)
        {
            TickCount++;
            var reservedZones = new HashSet<string>();
            var now = DateTime.UtcNow;

            // FASE 1 & 2: Muovi i treni lungo la loro rotta calcolata
            foreach (var route in ActiveRoutes.ToList())
            {
                var train = route.Train;
                
                if (train.IsExiting) continue;

                // Trova la zona di testa corrente nel path
                int headIndex = -1;
                for (int i = 0; i < route.PathZones.Count; i++)
                {
                    if (route.PathZones[i].Train?.Id == train.Id)
                    {
                        headIndex = i; // La prima zona trovata nel path è la testa, perché entriamo progressivamente
                    }
                }
                
                // Se la testa sta fermandosi in piattaforma
                if (train.StopHeadZone >= 0 && headIndex == train.StopHeadZone)
                {
                    if (train.IsStopping)
                    {
                        if (train.TimeRemaining > 0) train.TimeRemaining--;
                        else
                        {
                            train.IsStopping = false;
                            train.StopHeadZone = -1; // Sosta finita
                            if (train.Type == "S") 
                            {
                                train.Direction *= -1;
                                train.FromLeft = !train.FromLeft; // Inverte la direzione logica per i claim
                                route.PathZones.Reverse(); // Inverte il path per farlo uscire
                            }
                        }
                    }
                    else if (!train.HasStoppedAtPlatform)
                    {
                        train.HasStoppedAtPlatform = true;
                        train.IsStopping = true;
                        train.TimeRemaining = (int)Math.Max(3, 5 + _random.NextDouble() * 5);
                    }
                    continue; // In sosta
                }

                // Determiniamo il prossimo indice della testa
                int nextHeadIndex = headIndex + 1; // Avanzamento nel path calcolato

                if (nextHeadIndex >= route.PathZones.Count)
                {
                    train.IsExiting = true;
                    continue;
                }

                var nextZone = route.PathZones[nextHeadIndex];
                if (!nextZone.IsOccupied && !reservedZones.Contains(nextZone.SegmentId))
                {
                    reservedZones.Add(nextZone.SegmentId);
                    
                    // Applica movimento
                    if (train.VisibleZoneCount < train.ZoneCount)
                        train.VisibleZoneCount++;

                    // Pulisci zone vecchie occupate da questo treno
                    foreach (var z in route.PathZones)
                        if (z.Train?.Id == train.Id) { z.IsOccupied = false; z.Train = null; }

                    // Posiziona le zone visibili
                    for (int j = 0; j < train.VisibleZoneCount; j++)
                    {
                        int zi = nextHeadIndex - j;
                        if (zi >= 0 && zi < route.PathZones.Count)
                        {
                            route.PathZones[zi].IsOccupied = true;
                            route.PathZones[zi].Train = train;
                        }
                    }
                }
            }

            // FASE 2.5: Uscita graduale
            foreach (var route in ActiveRoutes.ToList())
            {
                var train = route.Train;
                if (!train.IsExiting) continue;

                // Rimuovi la coda (zona con indice più piccolo occupata dal treno)
                SimZone? tailZone = null;
                for (int i = 0; i < route.PathZones.Count; i++)
                {
                    if (route.PathZones[i].Train?.Id == train.Id)
                    {
                        tailZone = route.PathZones[i];
                        break;
                    }
                }

                if (tailZone != null)
                {
                    tailZone.IsOccupied = false;
                    tailZone.Train = null;
                    train.VisibleZoneCount = Math.Max(0, train.VisibleZoneCount - 1);
                }

                if (train.VisibleZoneCount == 0 && !train.DepartureTime.HasValue)
                {
                    train.DepartureTime = now;
                }
            }

            // FASE 3: Rimuovi treni completamente usciti
            foreach (var route in ActiveRoutes.ToList())
            {
                if (route.Train.DepartureTime.HasValue && route.Train.DepartureTime.Value <= now)
                {
                    ActiveRoutes.Remove(route);
                    if (route.Train.DbId.HasValue)
                    {
                        _completedDbIds.Add(route.Train.DbId.Value);
                        if (_dbTrainById.TryGetValue(route.Train.DbId.Value, out var dbTrain))
                        {
                            _departedTrains.Add(new DepartedEntry
                            {
                                DbTrain = dbTrain,
                                ActualArrival = route.Train.ArrivedAt,
                                ActualDepart = now,
                                Platform = null // Potremmo mappare a un ID piattaforma
                            });
                            _dbTrainById.Remove(route.Train.DbId.Value);
                        }
                    }
                }
            }

            // FASE 4: Posiziona treni in attesa
            var sorted = _waitingQueue
                .OrderByDescending(w => GetTrainPriority(w.DbTrain.TrainType))
                .ThenByDescending(w =>
                    w.DbTrain.TrainType is "T" or "S" ? 0.0 : (now - w.DbTrain.ScheduledArrivalTime).TotalMinutes)
                .ToList();

            foreach (var entry in sorted)
                if (TryPlaceOnTrack(entry.DbTrain))
                    _waitingQueue.Remove(entry);
        }
    }

    public void AddTrainFromDatabase(Train dbTrain)
    {
        lock (_lock)
        {
            if (_completedDbIds.Contains(dbTrain.Id)) return;
            if (HasTrainWithDbIdNoLock(dbTrain.Id)) return;
            if (_waitingQueue.Any(w => w.DbTrain.Id == dbTrain.Id)) return;
            if (_dbTrainById.ContainsKey(dbTrain.Id)) return;

            _dbTrainById.TryAdd(dbTrain.Id, dbTrain);

            if (TryPlaceOnTrack(dbTrain)) return;

            _waitingQueue.Add(new WaitingEntry { DbTrain = dbTrain, QueuedAt = DateTime.UtcNow });
        }
    }

    private bool TryPlaceOnTrack(Train dbTrain)
    {
        if (Layout.Nodes.Count == 0 || Layout.Segments.Count == 0) return false;

        bool fromLeft = dbTrain.Origin.StartsWith("L");
        int zonesNeeded = (int)Math.Ceiling(dbTrain.Length / 2.0);

        // Nodi di ingresso (nodi con X estrema)
        var minX = Layout.Nodes.Min(n => n.X);
        var maxX = Layout.Nodes.Max(n => n.X);

        var entryNodes = Layout.Nodes.Where(n => fromLeft ? n.X == minX : n.X == maxX).ToList();
        var exitNodes = Layout.Nodes.Where(n => fromLeft ? n.X == maxX : n.X == minX).ToList();

        if (!entryNodes.Any() || !exitNodes.Any()) return false;

        bool layoutHasPlatform = Layout.Segments.Any(s => s.IsPlatform);
        bool layoutHasStorage = Layout.Segments.Any(s => s.IsStorage);

        bool requirePlatform = layoutHasPlatform && !dbTrain.IsTransit && dbTrain.TrainType != "S";
        bool requireStorage = layoutHasStorage && dbTrain.TrainType == "S";

        HashSet<int>? validPlatforms = null;
        if (requirePlatform)
        {
            validPlatforms = new HashSet<int>();
            var platformLengths = Layout.Segments
                .Where(s => s.IsPlatform && s.PlatformNumber.HasValue)
                .GroupBy(s => s.PlatformNumber.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var kvp in platformLengths)
            {
                if (kvp.Value >= zonesNeeded)
                    validPlatforms.Add(kvp.Key);
            }

            // Se la stazione ha piattaforme, ma nessuna è abbastanza lunga, il treno non può entrare
            if (validPlatforms.Count == 0)
                return false;
        }

        // Calcola i claim per direzione per evitare head-on deadlocks
        var claimedByOpposite = new HashSet<string>();
        foreach (var route in ActiveRoutes)
        {
            if (route.Train.FromLeft != fromLeft)
            {
                foreach (var z in route.PathZones) claimedByOpposite.Add(z.SegmentId);
            }
        }

        // Trova un path tramite BFS
        foreach (var entryNode in entryNodes)
        {
            var path = FindPath(entryNode, exitNodes, requirePlatform, requireStorage, claimedByOpposite, validPlatforms);
            if (path != null && path.Count > 0)
            {
                // Controlla che la prima zona sia libera
                if (!path[0].IsOccupied)
                {
                    PlaceOnPath(dbTrain, path, Math.Min(zonesNeeded, path.Count), fromLeft, validPlatforms);
                    return true;
                }
            }
        }
        return false;
    }

    private List<SimZone>? FindPath(StationNode start, List<StationNode> destinations, bool requirePlatform, bool requireStorage, HashSet<string> claimedByOpposite, HashSet<int>? validPlatforms)
    {
        var queue = new Queue<(StationNode Node, List<SimZone> Path, bool HasValidPlatform)>();
        var visited = new HashSet<string>();

        queue.Enqueue((start, new List<SimZone>(), false));
        visited.Add($"{start.Id}_False");

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            bool destReached = destinations.Any(d => d.Id == current.Node.Id);
            bool isDeadEnd = Layout.Segments.Count(s => s.StartNodeId == current.Node.Id || s.EndNodeId == current.Node.Id) == 1 && current.Node.Id != start.Id;
            bool reachedStorageEnd = false;
            
            if (current.Path.Count > 0)
            {
                var lastSeg = Layout.Segments.First(s => s.Id == current.Path.Last().SegmentId);
                reachedStorageEnd = lastSeg.IsStorage && isDeadEnd;
            }

            if (requireStorage && reachedStorageEnd && !destReached) return current.Path;
            
            if (destReached)
            {
                if (!requireStorage && (!requirePlatform || current.HasValidPlatform))
                    return current.Path;
            }

            // Esplora vicini
            var edges = Layout.Segments.Where(s => s.StartNodeId == current.Node.Id || s.EndNodeId == current.Node.Id).ToList();
            
            foreach (var edge in edges)
            {
                if (GlobalZones.TryGetValue(edge.Id, out var zone) && !claimedByOpposite.Contains(edge.Id))
                {
                    var nextNodeId = edge.StartNodeId == current.Node.Id ? edge.EndNodeId : edge.StartNodeId;
                    
                    bool edgeHasValidPlatform = edge.IsPlatform && edge.PlatformNumber.HasValue && validPlatforms != null && validPlatforms.Contains(edge.PlatformNumber.Value);
                    bool nextHasPlatform = current.HasValidPlatform || edgeHasValidPlatform;
                    
                    string visitKey = $"{nextNodeId}_{nextHasPlatform}";

                    if (visited.Add(visitKey))
                    {
                        var newPath = new List<SimZone>(current.Path) { zone };
                        queue.Enqueue((Layout.Nodes.First(n => n.Id == nextNodeId), newPath, nextHasPlatform));
                    }
                }
            }
        }

        return null;
    }

    private void PlaceOnPath(Train dbTrain, List<SimZone> path, int zonesNeeded, bool fromLeft, HashSet<int>? validPlatforms)
    {
        int stopHeadZone = -1;
        if (!dbTrain.IsTransit)
        {
            if (dbTrain.TrainType == "S") 
            {
                stopHeadZone = path.Count - 1;
            }
            else 
            {
                int targetPlatform = -1;
                foreach (var z in path)
                {
                    var seg = Layout.Segments.First(s => s.Id == z.SegmentId);
                    if (seg.IsPlatform && seg.PlatformNumber.HasValue && validPlatforms != null && validPlatforms.Contains(seg.PlatformNumber.Value))
                    {
                        targetPlatform = seg.PlatformNumber.Value;
                        break;
                    }
                }

                if (targetPlatform != -1)
                {
                    // Find the LAST segment of this platform in the path for the head to stop at the end of the platform
                    for (int i = 0; i < path.Count; i++)
                    {
                        var seg = Layout.Segments.First(s => s.Id == path[i].SegmentId);
                        if (seg.IsPlatform && seg.PlatformNumber == targetPlatform)
                        {
                            stopHeadZone = i;
                        }
                    }
                }
                
                if (stopHeadZone == -1) stopHeadZone = path.Count / 2;
            }
        }

        var newTrain = new SimTrain
        {
            Id = $"DB-{dbTrain.Id}-{TickCount}",
            DbId = dbTrain.Id,
            Code = $"{dbTrain.TrainType}{dbTrain.TrainNumber}",
            Type = dbTrain.TrainType,
            Length = dbTrain.Length,
            ZoneCount = zonesNeeded,
            VisibleZoneCount = 1,
            Direction = 1, // Non usato col grafo, la direzione è implicita nel path
            FromLeft = fromLeft,
            IsStopping = false,
            TimeRemaining = 0,
            StopHeadZone = stopHeadZone,
            HasStoppedAtPlatform = false,
            DepartureTime = null,
            ArrivedAt = DateTime.UtcNow
        };

        path[0].IsOccupied = true;
        path[0].Train = newTrain;

        ActiveRoutes.Add(new ActiveTrainRoute { Train = newTrain, PathZones = path });
    }

    public bool HasTrainWithDbId(int dbId)
    {
        lock (_lock) return HasTrainWithDbIdNoLock(dbId);
    }

    private bool HasTrainWithDbIdNoLock(int dbId) =>
        ActiveRoutes.Any(r => r.Train.DbId == dbId);

    public void Reset()
    {
        lock (_lock)
        {
            TickCount = 0;
            LoadLayout();
            ActiveRoutes.Clear();
            _waitingQueue.Clear();
            _dbTrainById.Clear();
            _departedTrains.Clear();
            _completedDbIds.Clear();
        }
    }
}
