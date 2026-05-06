using FerroviaApi.Models;
using StazioneFerroviaria.Models.Simulation;

namespace StazioneFerroviaria.Services;

/// <summary>
/// Simulazione stazione ferroviaria — tick a 4 fasi.
///
/// Ciclo di vita non-T: ingresso (lato A o B) → avanzamento → sosta in piattaforma centrale
///                      → riparte nella stessa direzione → uscita dal lato opposto.
/// Ciclo di vita T    : ingresso → attraversamento completo a 1 zona/tick → uscita lato opposto.
///
/// Priorità accesso binario: T=5 > F=4 > V=3 > R=2 > S=1
/// Tie-break passeggeri: ritardo accumulato (cargo escluso).
/// S preferisce binari morti; piattaforma esplicita obbligatoria se AssignedPlatform != null.
///
/// StopHeadZone: zona centrale del binario dove la testa del treno si ferma.
///   direction=1 → StopHeadZone = track.Count/2 + zonesNeeded/2 - 1
///   direction=-1→ StopHeadZone = track.Count/2 - zonesNeeded/2
/// Treni T → StopHeadZone = -1 (attraversano il binario per intero).
/// </summary>
public class SimulationService
{
    private readonly object _lock   = new();
    private readonly Random _random = new();

    public List<SimTrack> Tracks { get; private set; } = new();
    public int TickCount { get; private set; }

    // ── Code e dizionari ──────────────────────────────────────────────────
    private readonly List<WaitingEntry>      _waitingQueue    = new();
    private readonly Dictionary<int, Train>  _dbTrainById     = new();
    private readonly List<DepartedEntry>     _departedTrains  = new();

    /// <summary>
    /// Contiene gli ID DB di tutti i treni che hanno completato il ciclo (partiti).
    /// Questo set è l'unico guard affidabile contro il reload: sopravvive anche
    /// se PersistDepartedTrainsAsync fallisce silenziosamente (nessun TrainsLog
    /// scritto) perché vive in memoria per tutta la sessione di simulazione.
    /// Azzerato solo da Reset().
    /// </summary>
    private readonly HashSet<int> _completedDbIds = new();

    // ── Strutture ─────────────────────────────────────────────────────────

    private sealed class WaitingEntry
    {
        public Train    DbTrain  { get; init; } = null!;
        public DateTime QueuedAt { get; init; }
    }

    public sealed class DepartedEntry
    {
        public Train    DbTrain       { get; init; } = null!;
        public DateTime ActualArrival { get; init; }
        public DateTime ActualDepart  { get; init; }
        public int?     Platform      { get; init; }
    }

    // ── Priorità ──────────────────────────────────────────────────────────

    public static int GetTrainPriority(string trainType) => trainType switch
    {
        "T" => 5,
        "F" => 4,
        "V" => 3,
        "R" => 2,
        "S" => 1,
        _   => 0
    };

    // ── API pubblica thread-safe ───────────────────────────────────────────

    public IReadOnlyList<(Train Train, DateTime QueuedAt)> GetWaitingQueue()
    {
        lock (_lock)
            return _waitingQueue.Select(w => (w.DbTrain, w.QueuedAt)).ToList();
    }

    /// <summary>Svuota e restituisce i treni partiti per la scrittura su TrainsLog.</summary>
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

    // ── Costruttore ────────────────────────────────────────────────────────

    public SimulationService() => InitializeTracks();

    private void InitializeTracks()
    {
        Tracks.Clear();
        TickCount = 0;
        int zoneId = 1, trackId = 1;

        for (int i = 1; i <= 4; i++)
        {
            var t = new SimTrack { Id = trackId++, Label = $"Transit {i}", Type = "transit", MaxZones = 20 };
            for (int j = 0; j < 20; j++) t.Zones.Add(new SimZone { Id = zoneId++ });
            Tracks.Add(t);
        }
        for (int i = 1; i <= 2; i++)
        {
            var t = new SimTrack { Id = trackId++, Label = $"Maintenance {i}", Type = "maintenance", MaxZones = 10 };
            for (int j = 0; j < 10; j++) t.Zones.Add(new SimZone { Id = zoneId++ });
            Tracks.Add(t);
        }
    }

    // ── Tick ──────────────────────────────────────────────────────────────

    public void Tick()
    {
        lock (_lock)
        {
            TickCount++;

            var trainMovements  = new List<TrainMovement>();
            var reservedZones   = new HashSet<int>();
            var processedTrains = new HashSet<string>();

            // ═══ FASE 1: pianifica movimenti ═══
            foreach (var track in Tracks)
            {
                for (int i = 0; i < track.Zones.Count; i++)
                {
                    var zone = track.Zones[i];
                    if (zone.Train == null || processedTrains.Contains(zone.Train.Id)) continue;

                    var train = zone.Train;
                    processedTrains.Add(train.Id);

                    // Treni in uscita gestiti dalla Fase 2.5 — nessun movimento
                    if (train.IsExiting) continue;

                    // Identifica la testa (zona di punta nella direzione del moto)
                    var trainZones    = track.Zones.Where(z => z.Train?.Id == train.Id).ToList();
                    var headZone      = train.Direction == 1 ? trainZones[^1] : trainZones[0];
                    var headZoneIndex = track.Zones.IndexOf(headZone);
                    var nextHeadIndex = headZoneIndex + train.Direction;

                    // ── Piattaforma centrale: sosta e partenza ─────────────────────
                    // Applicato solo se StopHeadZone >= 0 (treni non-T).
                    if (train.StopHeadZone >= 0 && headZoneIndex == train.StopHeadZone)
                    {
                        if (train.IsStopping)
                        {
                            // Countdown sosta in piattaforma
                            if (train.TimeRemaining > 0)
                            {
                                train.TimeRemaining--;
                            }
                            else
                            {
                                // Sosta completata
                                train.IsStopping   = false;
                                train.StopHeadZone = -1; // non si ri-ferma piu'

                                if (track.Type == "maintenance")
                                {
                                    // Binario morto: nessuna uscita dall'altro lato.
                                    // Il treno inverte la direzione e torna al lato d'ingresso.
                                    train.Direction *= -1;
                                }
                                // Binario di transito/piattaforma:
                                // direction invariata → continua verso l'uscita opposta
                            }
                        }
                        else if (!train.HasStoppedAtPlatform)
                        {
                            // Prima volta in piattaforma: avvia sosta gaussiana
                            train.HasStoppedAtPlatform = true;
                            int stopTime = track.Type == "maintenance"
                                ? (int)Math.Clamp(Math.Round(NextGaussian(10, 3)), 5, 30)
                                : (int)Math.Clamp(Math.Round(NextGaussian(2.5, 1)), 1, 8);
                            train.IsStopping    = true;
                            train.TimeRemaining = stopTime;
                        }
                        // Se HasStoppedAtPlatform=true e IsStopping=false e StopHeadZone=-1:
                        // sosta gia' completata, treno in viaggio verso l'uscita opposta.
                        continue; // nessun movimento
                    }

                    // ── Fine binario ──────────────────────────────────────────────
                    // Raggiunto da: T (attraversamento), S dopo inversione, R/V/F dopo sosta.
                    // Invece di sparire istantaneamente, attiva l'uscita graduale (IsExiting).
                    // La Fase 2.5 rimuove una zona di coda per tick; Fase 3 logga a fine uscita.
                    if (nextHeadIndex < 0 || nextHeadIndex >= track.Zones.Count)
                    {
                        train.IsExiting = true;
                        continue; // elaborato in Fase 2.5
                    }

                    // ── Movimento normale: verifica disponibilità ────────────────
                    var nextZone = track.Zones[nextHeadIndex];
                    bool canMove = !reservedZones.Contains(nextZone.Id) &&
                                   (!nextZone.IsOccupied || nextZone.Train?.Id == train.Id);
                    if (canMove)
                    {
                        reservedZones.Add(nextZone.Id);
                        trainMovements.Add(new TrainMovement
                        {
                            Train        = train,
                            Track        = track,
                            NewHeadIndex = nextHeadIndex
                        });
                    }
                }
            }

            // ═══ FASE 2: applica movimenti atomicamente ═══
            foreach (var move in trainMovements)
            {
                // Libera le zone attualmente occupate da questo treno
                foreach (var z in move.Track.Zones.Where(z => z.Train?.Id == move.Train.Id))
                {
                    z.IsOccupied = false;
                    z.Train      = null;
                }

                // Ingresso graduale: ogni tick che il treno si muove, la coda avanza di 1 zona
                // finche' il treno non e' completamente entrato nel binario.
                if (move.Train.VisibleZoneCount < move.Train.ZoneCount)
                    move.Train.VisibleZoneCount++;

                // Posiziona le zone visibili: dalla testa verso la coda
                for (int j = 0; j < move.Train.VisibleZoneCount; j++)
                {
                    int zi = move.NewHeadIndex - j * move.Train.Direction;
                    if (zi >= 0 && zi < move.Track.Zones.Count)
                    {
                        move.Track.Zones[zi].IsOccupied = true;
                        move.Track.Zones[zi].Train      = move.Train;
                    }
                }
            }

            // ═══ FASE 2.5: uscita graduale ═══
            // Per ogni treno con IsExiting=true rimuove una zona di coda per tick.
            // Quando VisibleZoneCount=0 imposta DepartureTime per la rimozione in Fase 3.
            {
                var exitProcessed = new HashSet<string>();
                foreach (var xTrack in Tracks)
                {
                    for (int xi = 0; xi < xTrack.Zones.Count; xi++)
                    {
                        var xz = xTrack.Zones[xi];
                        if (xz.Train == null || !xz.Train.IsExiting ||
                            exitProcessed.Contains(xz.Train.Id)) continue;

                        var et = xz.Train;
                        exitProcessed.Add(et.Id);

                        // Zona di coda: la parte del treno ancora nel binario che deve uscire
                        // direction= 1 (uscita a destra) → coda = zona a indice minore
                        // direction=-1 (uscita a sinistra) → coda = zona a indice maggiore
                        SimZone? tailZone = null;
                        int tailIdx = et.Direction == 1 ? int.MaxValue : int.MinValue;
                        for (int ti = 0; ti < xTrack.Zones.Count; ti++)
                        {
                            if (xTrack.Zones[ti].Train?.Id != et.Id) continue;
                            if (et.Direction == 1 && ti < tailIdx) { tailIdx = ti; tailZone = xTrack.Zones[ti]; }
                            if (et.Direction != 1 && ti > tailIdx) { tailIdx = ti; tailZone = xTrack.Zones[ti]; }
                        }

                        if (tailZone != null)
                        {
                            tailZone.IsOccupied = false;
                            tailZone.Train      = null;
                            et.VisibleZoneCount = Math.Max(0, et.VisibleZoneCount - 1);
                        }
                        else
                        {
                            et.VisibleZoneCount = 0;
                        }

                        if (et.VisibleZoneCount == 0)
                        {
                            // IMPORTANTE: la Fase 3 non puo' trovare il treno nelle zone
                            // perche' le abbiamo gia' svuotate tutte. Gestiamo la partenza
                            // direttamente qui: aggiungiamo a _departedTrains e puliamo
                            // _dbTrainById, cosi' PersistDepartedTrainsAsync scrive il TrainsLog.
                            //
                            // _completedDbIds viene aggiornato SEMPRE (anche se _dbTrainById
                            // non contiene piu' la voce) cosi' AddTrainFromDatabase non
                            // riaccoderà questo treno anche se il DB write fallisce.
                            if (et.DbId.HasValue)
                            {
                                _completedDbIds.Add(et.DbId.Value);
                                if (_dbTrainById.TryGetValue(et.DbId.Value, out var depDbTrain))
                                {
                                    _departedTrains.Add(new DepartedEntry
                                    {
                                        DbTrain       = depDbTrain,
                                        ActualArrival = et.ArrivedAt,
                                        ActualDepart  = DateTime.UtcNow,
                                        Platform      = depDbTrain.AssignedPlatform
                                                        ?? TrackIdToPlatform(xTrack.Id)
                                    });
                                    _dbTrainById.Remove(et.DbId.Value);
                                }
                            }
                        }
                    }
                }
            }

            // ═══ FASE 3: rimuovi treni con DepartureTime scaduto ═══
            var now = DateTime.UtcNow;

            var toDepart = new Dictionary<string, (SimTrain SimTrain, SimTrack Track)>();
            foreach (var track in Tracks)
                foreach (var zone in track.Zones)
                    if (zone.Train?.DepartureTime.HasValue == true &&
                        zone.Train.DepartureTime!.Value <= now)
                        toDepart.TryAdd(zone.Train.Id, (zone.Train, track));

            foreach (var track in Tracks)
                foreach (var zone in track.Zones)
                    if (zone.Train != null && toDepart.ContainsKey(zone.Train.Id))
                    {
                        zone.IsOccupied = false;
                        zone.Train      = null;
                    }

            foreach (var (_, (simTrain, track)) in toDepart)
            {
                if (simTrain.DbId.HasValue)
                {
                    _completedDbIds.Add(simTrain.DbId.Value);
                    if (_dbTrainById.TryGetValue(simTrain.DbId.Value, out var dbTrain))
                    {
                        _departedTrains.Add(new DepartedEntry
                        {
                            DbTrain       = dbTrain,
                            ActualArrival = simTrain.ArrivedAt,
                            ActualDepart  = now,
                            Platform      = dbTrain.AssignedPlatform ?? TrackIdToPlatform(track.Id)
                        });
                        _dbTrainById.Remove(simTrain.DbId.Value);
                    }
                }
            }

            // ═══ FASE 4: posiziona treni in attesa (priorità + ritardo) ═══
            var sorted = _waitingQueue
                .OrderByDescending(w => GetTrainPriority(w.DbTrain.TrainType))
                .ThenByDescending(w =>
                    w.DbTrain.TrainType is "T" or "S"
                        ? 0.0
                        : (now - w.DbTrain.ScheduledArrivalTime).TotalMinutes)
                .ToList();

            foreach (var entry in sorted)
                if (TryPlaceOnTrack(entry.DbTrain))
                    _waitingQueue.Remove(entry);
        }
    }

    // ── Aggiunta treni ────────────────────────────────────────────────────

    public void AddTrainFromDatabase(Train dbTrain)
    {
        lock (_lock)
        {
            // Guard primario: il treno ha già completato il suo ciclo in questa sessione.
            // Questo check è immune a fallimenti del DB write (nessun TrainsLog scritto)
            // perché _completedDbIds vive interamente in memoria.
            if (_completedDbIds.Contains(dbTrain.Id)) return;

            if (HasTrainWithDbIdNoLock(dbTrain.Id)) return;
            if (_waitingQueue.Any(w => w.DbTrain.Id == dbTrain.Id)) return;
            // Difesa: se il treno e' ancora in _dbTrainById (es. uscita in corso),
            // non ricominciare il ciclo da capo.
            if (_dbTrainById.ContainsKey(dbTrain.Id)) return;

            _dbTrainById.TryAdd(dbTrain.Id, dbTrain);

            if (TryPlaceOnTrack(dbTrain)) return;

            _waitingQueue.Add(new WaitingEntry { DbTrain = dbTrain, QueuedAt = DateTime.UtcNow });
        }
    }

    private bool TryPlaceOnTrack(Train dbTrain)
    {
        int direction   = dbTrain.Origin.StartsWith("L") ? 1 : -1;
        int zonesNeeded = (int)Math.Ceiling(dbTrain.Length / 2.0);

        // 1. Piattaforma specifica obbligatoria (assegnazione manuale)
        if (dbTrain.AssignedPlatform.HasValue)
        {
            var target = Tracks.FirstOrDefault(t =>
                t.Id == dbTrain.AssignedPlatform.Value && t.Type != "maintenance");
            if (target != null && CanFit(target, direction, zonesNeeded))
            {
                PlaceOn(dbTrain, target, direction, zonesNeeded);
                return true;
            }
            return false;
        }

        // 2. S: binario morto (dead-end siding), meno occupato prima
        if (dbTrain.TrainType == "S")
        {
            var maint = Tracks
                .Where(t => t.Type == "maintenance" && CanFit(t, direction, zonesNeeded))
                .OrderBy(t => t.Zones.Count(z => z.IsOccupied))
                .FirstOrDefault();
            if (maint != null) { PlaceOn(dbTrain, maint, direction, zonesNeeded); return true; }
            // Se tutti i binari morti sono occupati: S attende in coda (non va su transito)
            return false;
        }

        // 3. T/R/V/F: binari di transito.
        //    Ordina per occupazione crescente: T ottiene il binario più libero
        //    (meno rischi di essere bloccato da un treno fermo); R/V/F distribuisce il carico.
        var transit = Tracks
            .Where(t => t.Type != "maintenance" && CanFit(t, direction, zonesNeeded))
            .OrderBy(t => t.Zones.Count(z => z.IsOccupied))
            .ToList();

        if (transit.Count == 0) return false;
        PlaceOn(dbTrain, transit[0], direction, zonesNeeded);
        return true;
    }

    private static bool CanFit(SimTrack track, int direction, int zonesNeeded)
    {
        int entry = direction == 1 ? 0 : track.Zones.Count - 1;
        for (int i = 0; i < zonesNeeded; i++)
        {
            int ci = entry + i * direction;
            if (ci < 0 || ci >= track.Zones.Count || track.Zones[ci].IsOccupied)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Posiziona il treno all'ingresso del binario e calcola la zona di sosta.
    ///
    /// T  (IsTransit): StopHeadZone=-1, attraversano senza fermarsi.
    /// S  (maintenance track): StopHeadZone = estremo finale del binario morto.
    ///    Dopo la sosta invertira' direzione ed uscira' dal lato d'ingresso.
    /// R/V/F (transit track): StopHeadZone = zona centrale del binario.
    ///    Dopo la sosta continuano nella stessa direzione ed escono dal lato opposto.
    /// </summary>
    private void PlaceOn(Train dbTrain, SimTrack track, int direction, int zonesNeeded)
    {
        int entry = direction == 1 ? 0 : track.Zones.Count - 1;

        int stopHeadZone;
        if (dbTrain.IsTransit)
        {
            // T: nessuna sosta, attraversamento completo lato A → lato B
            stopHeadZone = -1;
        }
        else if (track.Type == "maintenance")
        {
            // S su binario morto: avanza fino all'estremo finale del binario
            // direction= 1: testa arriva all'ultimo indice (Count-1), coda riempie all'indietro
            // direction=-1: testa arriva all'indice 0, coda riempie in avanti
            stopHeadZone = direction == 1
                ? track.Zones.Count - 1
                : 0;
        }
        else
        {
            // R/V/F: sosta a meta' del binario (zona piattaforma passeggeri)
            int center = track.Zones.Count / 2;
            stopHeadZone = direction == 1
                ? center + zonesNeeded / 2 - 1
                : center - zonesNeeded / 2;
            // Clamp: testa dentro il binario, spazio per tutta la coda
            stopHeadZone = Math.Clamp(stopHeadZone,
                zonesNeeded - 1,
                track.Zones.Count - zonesNeeded);
        }

        var newTrain = new SimTrain
        {
            Id                   = $"DB-{dbTrain.Id}-{TickCount}",
            DbId                 = dbTrain.Id,
            Code                 = $"{dbTrain.TrainType}{dbTrain.TrainNumber}",
            Type                 = dbTrain.TrainType,
            Length               = dbTrain.Length,
            ZoneCount            = zonesNeeded,
            VisibleZoneCount     = 1,    // ingresso graduale: parte solo con la testa
            Direction            = direction,
            IsStopping           = false,
            TimeRemaining        = 0,
            StopHeadZone         = stopHeadZone,
            HasStoppedAtPlatform = false,
            DepartureTime        = null,
            ArrivedAt            = DateTime.UtcNow
        };

        // Occupa solo la zona di ingresso (testa). La coda entrera' zona per zona
        // ad ogni tick di movimento (Fase 2 cresce VisibleZoneCount).
        track.Zones[entry].IsOccupied = true;
        track.Zones[entry].Train      = newTrain;
    }

    // ── Utilità ────────────────────────────────────────────────────────────

    public bool HasTrainWithDbId(int dbId)
    {
        lock (_lock) return HasTrainWithDbIdNoLock(dbId);
    }

    private bool HasTrainWithDbIdNoLock(int dbId) =>
        Tracks.Any(t => t.Zones.Any(z => z.Train?.DbId == dbId));

    private static int? TrackIdToPlatform(int trackId) =>
        trackId is >= 1 and <= 4 ? trackId : null;

    public void Reset()
    {
        lock (_lock)
        {
            InitializeTracks();
            _waitingQueue.Clear();
            _dbTrainById.Clear();
            _departedTrains.Clear();
            _completedDbIds.Clear();
        }
    }

    // ── Box-Muller ─────────────────────────────────────────────────────────

    private double NextGaussian(double mean, double stdDev)
    {
        double u1 = 1.0 - _random.NextDouble();
        double u2 = 1.0 - _random.NextDouble();
        double z  = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return mean + stdDev * z;
    }

    // ── Classi interne ─────────────────────────────────────────────────────

    private sealed class TrainMovement
    {
        public SimTrain Train        { get; set; } = null!;
        public SimTrack Track        { get; set; } = null!;
        public int      NewHeadIndex { get; set; }
    }
}
