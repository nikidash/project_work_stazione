# Fix: mapping binario corretto e stato occupazione dinamico

## Causa del problema (perché i treni non venivano assegnati correttamente)

In `AddTrainFromDatabase`, quando il binario richiesto (`AssignedPlatform`) era occupato
(anche parzialmente nelle zone di ingresso), il codice cadeva in un **fallback random**:
sceglieva un qualsiasi binario transit libero. L'utente assegnava un treno al binario 2,
ma lo vedeva apparire sul binario 4 (o altro).

Nella Dashboard, `Sector.IsAvailable` non veniva mai aggiornato: restava al valore
iniziale del DB (sempre `true`). Lo stato "Libero/Occupato" nella tabella binari
era sempre "Libero", anche con treni attivi.

Inoltre, la Dashboard caricava TUTTI i treni dal DB senza filtrare per
`ScheduledArrivalTime`/`ScheduledDepartureTime`, mostrando treni già partiti.

## Modifiche effettuate per correggere il mapping binario → treno

### SimulationService.cs — assegnazione stretta

Aggiunta la riga:
```csharp
if (track == null && dbTrain.AssignedPlatform.HasValue)
    return;
```

Dopo il tentativo sul binario richiesto, se il binario è occupato E il treno
ha un `AssignedPlatform` impostato, il metodo esce senza piazzare il treno.
Il treno non viene più messo su un binario random.

Il treno verrà ritentato al prossimo ciclo di `LoadCurrentTrains` (ogni 5 tick
nel simulation loop). Quando il binario si libera, il treno verrà piazzato
correttamente.

Il fallback random resta SOLO per treni senza `AssignedPlatform` (null).

## Come il simulatore ora gestisce TRANSIT e MAINTENANCE

- Binari TRANSIT (Id 1-4, 20 zone): assegnati tramite `AssignedPlatform` (1-4)
  dal dropdown in AddTrain. I settori nel DB hanno PlatformNumber 1-4.
- Binari MAINTENANCE (Id 5-6, 10 zone): non accessibili dal dropdown AddTrain
  (PlatformNumber è null per i settori maintenance). Usati solo dalla logica
  interna di simulazione (inversione treni, sosta manutenzione).

Il check `t.Type != "maintenance"` in AddTrainFromDatabase impedisce che un treno
DB venga piazzato per errore su un binario maintenance.

## Come funziona il filtro Arrivo/Partenza e l'aggiornamento automatico

### Ingresso treni
- **Evento immediato**: AddTrain salva, TrainNotification notifica, Simulator piazza
  se `ScheduledArrivalTime <= now` e binario libero
- **Refresh periodico** (ogni 5 tick): LoadCurrentTrains ricarica treni attivi
  dal DB, aggiunge quelli nuovi, ritenta quelli non ancora piazzati

### Uscita treni
- **Tick fase 3**: ogni tick, rimuove treni con `DepartureTime <= DateTime.UtcNow`
- **Uscita fisica**: treni che raggiungono la fine del binario escono naturalmente

### Dashboard
- Treni filtrati: `ScheduledArrivalTime <= now && ScheduledDepartureTime > now`
- Solo treni attualmente in stazione vengono visualizzati

## Come viene gestito e aggiornato lo stato occupato dei binari

Lo stato `IsAvailable` dei settori viene calcolato **dinamicamente** ad ogni
caricamento della Dashboard, senza persistere nel DB:

```csharp
foreach (var sector in sectors)
{
    if (sector.PlatformNumber.HasValue)
        sector.IsAvailable = !trains.Any(t => t.AssignedPlatform == sector.PlatformNumber);
}
```

Un settore è "Occupato" se esiste almeno un treno attivo (nella finestra
Arrivo/Partenza) con `AssignedPlatform` uguale al `PlatformNumber` del settore.

Questo approccio è sempre accurato: non dipende da aggiornamenti manuali
del campo `IsAvailable` nel DB, che potevano andare fuori sincronizzazione.
