using FerroviaApi.Models;
using StazioneFerroviaria.Models.Simulation;

namespace StazioneFerroviaria.Services;

/// <summary>
/// Simulazione stazione ferroviaria.
/// Tick a 2 fasi: pianifica movimenti, applica atomicamente.
/// I treni provengono esclusivamente dal database via AddTrainFromDatabase.
/// </summary>
public class SimulationService
{
    private readonly object _lock = new();
    private readonly Random _random = new();

    public List<SimTrack> Tracks { get; private set; } = new();
    public int TickCount { get; private set; }

    public SimulationService()
    {
        InitializeTracks();
    }

    /// <summary>
    /// Inizializza i binari: 4 transit (20 zone) + 2 maintenance (10 zone).
    /// Replica di TrackSeeder.php.
    /// </summary>
    private void InitializeTracks()
    {
        Tracks.Clear();
        TickCount = 0;

        int zoneId = 1;
        int trackId = 1;

        for (int i = 1; i <= 4; i++)
        {
            var track = new SimTrack
            {
                Id = trackId++,
                Label = $"Transit {i}",
                Type = "transit",
                MaxZones = 20
            };
            for (int j = 0; j < 20; j++)
                track.Zones.Add(new SimZone { Id = zoneId++ });
            Tracks.Add(track);
        }

        for (int i = 1; i <= 2; i++)
        {
            var track = new SimTrack
            {
                Id = trackId++,
                Label = $"Maintenance {i}",
                Type = "maintenance",
                MaxZones = 10
            };
            for (int j = 0; j < 10; j++)
                track.Zones.Add(new SimZone { Id = zoneId++ });
            Tracks.Add(track);
        }
    }

    /// <summary>
    /// Esegue un singolo tick della simulazione.
    /// Replica fedele di Simulation.js -> tick().
    /// </summary>
    public void Tick()
    {
        lock (_lock)
        {
            TickCount++;

            var trainMovements = new List<TrainMovement>();
            var reservedZones = new HashSet<int>();
            var processedTrains = new HashSet<string>();

            // ═══ FASE 1: Identifica treni e pianifica movimenti ═══
            foreach (var track in Tracks)
            {
                for (int i = 0; i < track.Zones.Count; i++)
                {
                    var zone = track.Zones[i];
                    if (zone.Train == null || processedTrains.Contains(zone.Train.Id))
                        continue;

                    var train = zone.Train;
                    processedTrains.Add(train.Id);

                    // Trova le zone occupate da questo treno (ordinate per indice nel binario)
                    var trainZones = track.Zones
                        .Where(z => z.Train != null && z.Train.Id == train.Id)
                        .ToList();

                    // La testa dipende dalla direzione:
                    // direction=1 (destra): testa = ultima zona occupata
                    // direction=-1 (sinistra): testa = prima zona occupata
                    var headZone = train.Direction == 1 ? trainZones[^1] : trainZones[0];
                    var headZoneIndex = track.Zones.IndexOf(headZone);
                    var nextHeadIndex = headZoneIndex + train.Direction;

                    // --- Gestione attesa ---
                    if (train.IsStopping && train.TimeRemaining > 0)
                    {
                        train.TimeRemaining--;
                        continue;
                    }
                    if (train.IsStopping && train.TimeRemaining <= 0)
                    {
                        train.IsStopping = false;
                    }

                    // --- Logica binario manutenzione ---
                    if (track.Type == "maintenance" &&
                        (nextHeadIndex < 0 || nextHeadIndex >= track.Zones.Count))
                    {
                        if (!train.IsStopping)
                        {
                            // Arrivato a fine manutenzione, inizia attesa
                            train.IsStopping = true;
                            train.TimeRemaining = _random.Next(5, 10); // 5-9 ticks
                        }
                        else
                        {
                            // Attesa finita, pianifica inversione
                            trainMovements.Add(new TrainMovement
                            {
                                MoveType = MoveType.Reverse,
                                Train = train,
                                Track = track
                            });
                        }
                        continue;
                    }

                    // --- Logica partenza (treno esce dal binario) ---
                    if (nextHeadIndex < 0 || nextHeadIndex >= track.Zones.Count)
                    {
                        trainMovements.Add(new TrainMovement
                        {
                            MoveType = MoveType.Depart,
                            Train = train,
                            Track = track
                        });
                        continue;
                    }

                    // --- Logica movimento ---
                    var nextZone = track.Zones[nextHeadIndex];
                    var canMove = !reservedZones.Contains(nextZone.Id) &&
                                  (!nextZone.IsOccupied ||
                                   (nextZone.Train != null && nextZone.Train.Id == train.Id));

                    if (canMove)
                    {
                        reservedZones.Add(nextZone.Id);
                        trainMovements.Add(new TrainMovement
                        {
                            MoveType = MoveType.Move,
                            Train = train,
                            Track = track,
                            NewHeadIndex = nextHeadIndex
                        });
                    }
                }
            }

            // ═══ FASE 2: Applica movimenti atomicamente ═══
            foreach (var move in trainMovements)
            {
                if (move.MoveType == MoveType.Reverse)
                {
                    move.Train.Direction *= -1;
                    move.Train.IsStopping = false;
                    continue;
                }

                // Libera le zone correnti del treno
                var currentZones = move.Track.Zones
                    .Where(z => z.Train != null && z.Train.Id == move.Train.Id)
                    .ToList();
                foreach (var z in currentZones)
                {
                    z.IsOccupied = false;
                    z.Train = null;
                }

                if (move.MoveType == MoveType.Move)
                {
                    // Rioccupa le zone dalla nuova posizione della testa
                    var headIndex = move.NewHeadIndex;
                    for (int j = 0; j < move.Train.ZoneCount; j++)
                    {
                        var zoneIndex = headIndex - (j * move.Train.Direction);
                        if (zoneIndex >= 0 && zoneIndex < move.Track.Zones.Count)
                        {
                            move.Track.Zones[zoneIndex].IsOccupied = true;
                            move.Track.Zones[zoneIndex].Train = move.Train;
                        }
                    }
                }
                // MoveType.Depart: il treno e' gia' stato rimosso sopra
            }

            // ═══ FASE 3: Rimuovi treni con partenza scaduta ═══
            var now = DateTime.UtcNow;
            foreach (var track in Tracks)
            {
                foreach (var zone in track.Zones)
                {
                    if (zone.Train?.DepartureTime.HasValue == true && zone.Train.DepartureTime.Value <= now)
                    {
                        zone.IsOccupied = false;
                        zone.Train = null;
                    }
                }
            }
        }
    }

    public void AddTrainFromDatabase(Train dbTrain)
    {
        lock (_lock)
        {
            int direction = dbTrain.Origin.StartsWith("L") ? 1 : -1;
            int zonesNeeded = (int)Math.Ceiling(dbTrain.Length / 2.0);

            SimTrack? track = null;

            if (dbTrain.AssignedPlatform.HasValue)
            {
                var target = Tracks.FirstOrDefault(t => t.Id == dbTrain.AssignedPlatform.Value && t.Type != "maintenance");
                if (target != null)
                {
                    var ei = direction == 1 ? 0 : target.Zones.Count - 1;
                    bool canPlace = true;
                    for (int i = 0; i < zonesNeeded; i++)
                    {
                        var ci = ei + (i * direction);
                        if (ci < 0 || ci >= target.Zones.Count || target.Zones[ci].IsOccupied)
                        { canPlace = false; break; }
                    }
                    if (canPlace) track = target;
                }
            }

            if (track == null && dbTrain.AssignedPlatform.HasValue)
                return;

            if (track == null)
            {
                var availableTracks = Tracks.Where(t =>
                {
                    if (t.Type == "maintenance") return false;
                    var entryIndex = direction == 1 ? 0 : t.Zones.Count - 1;
                    for (int i = 0; i < zonesNeeded; i++)
                    {
                        var checkIndex = entryIndex + (i * direction);
                        if (checkIndex < 0 || checkIndex >= t.Zones.Count ||
                            t.Zones[checkIndex].IsOccupied)
                            return false;
                    }
                    return true;
                }).ToList();

                if (availableTracks.Count == 0) return;
                track = availableTracks[_random.Next(availableTracks.Count)];
            }

            var entryIndex2 = direction == 1 ? 0 : track.Zones.Count - 1;

            var newTrain = new SimTrain
            {
                Id = $"DB-{dbTrain.Id}-{TickCount}",
                DbId = dbTrain.Id,
                Code = $"{dbTrain.TrainType}{dbTrain.TrainNumber}",
                Type = dbTrain.TrainType,
                Length = dbTrain.Length,
                ZoneCount = zonesNeeded,
                Direction = direction,
                IsStopping = !dbTrain.IsTransit,
                TimeRemaining = dbTrain.IsTransit ? 0 : _random.Next(3, 7),
                DepartureTime = dbTrain.ScheduledDepartureTime
            };

            for (int i = 0; i < zonesNeeded; i++)
            {
                var zoneIndex = entryIndex2 + (i * direction);
                if (zoneIndex >= 0 && zoneIndex < track.Zones.Count)
                {
                    track.Zones[zoneIndex].IsOccupied = true;
                    track.Zones[zoneIndex].Train = newTrain;
                }
            }
        }
    }

    public bool HasTrainWithDbId(int dbId)
    {
        lock (_lock)
        {
            return Tracks.Any(t => t.Zones.Any(z => z.Train?.DbId == dbId));
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            InitializeTracks();
        }
    }

    private enum MoveType { Move, Depart, Reverse }

    private class TrainMovement
    {
        public MoveType MoveType { get; set; }
        public SimTrain Train { get; set; } = null!;
        public SimTrack Track { get; set; } = null!;
        public int NewHeadIndex { get; set; }
    }
}
