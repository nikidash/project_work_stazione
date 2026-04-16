# Fix: binari mostrano solo treni nella finestra Arrivo-Partenza

## Come venivano gestiti prima i binari

Il simulatore caricava TUTTI i treni dal database all'avvio (`Db.Trains.ToListAsync()`)
e li piazzava tutti sui binari, senza verificare `ScheduledArrivalTime` e
`ScheduledDepartureTime`. Risultato:

- Treni già partiti restavano sui binari
- Treni con arrivo futuro comparivano subito
- Ogni riavvio del simulatore accumulava treni fantasma

Non esisteva alcun meccanismo di rimozione basato sul tempo reale.

## Cosa è stato modificato per mostrare solo i treni attuali

### SimTrain.cs — Nuovi campi

- `DbId` (int?): identifica univocamente il treno nel DB, usato per evitare duplicati
- `DepartureTime` (DateTime?): orario di partenza programmato, usato per la rimozione automatica

### SimulationService.cs — 3 modifiche

1. **AddTrainFromDatabase**: imposta `DbId = dbTrain.Id` e `DepartureTime = dbTrain.ScheduledDepartureTime`
   sul SimTrain creato

2. **Tick() fase 3** (nuova): dopo i movimenti, scansiona tutte le zone e rimuove
   i treni il cui `DepartureTime <= DateTime.UtcNow`. Il treno scompare dal binario
   in tempo reale.

3. **HasTrainWithDbId(int)** (nuovo): restituisce true se un treno con quel DbId
   è già presente in qualche zona. Usato dal simulatore per evitare duplicati
   durante il refresh periodico.

### Simulator.razor.cs — Filtro temporale

- **LoadCurrentTrains()** (nuovo): query `Db.Trains.Where(arrival <= now && departure > now)`,
  poi per ogni treno non già presente chiama AddTrainFromDatabase
- **OnInitializedAsync**: chiama LoadCurrentTrains invece di caricare tutti
- **RunSimulationLoop**: ogni 5 tick, chiama LoadCurrentTrains per aggiungere
  treni la cui ora di arrivo è appena trascorsa
- **OnTrainAddedFromDb**: piazza il treno solo se `ScheduledArrivalTime <= now`
- **DoReset**: dopo il reset dei binari, chiama LoadCurrentTrains

## Come l'inserimento da AddTrain viene visualizzato immediatamente

1. AddTrain salva il treno con `ScheduledArrivalTime = DateTime.UtcNow`
2. Chiama `TrainNotification.NotifyTrainAdded(train)`
3. L'evento arriva a Simulator, che verifica `arrival <= now` (vero, perché appena impostato)
4. Chiama `AddTrainFromDatabase` → il treno appare sul binario corretto
5. `InvokeAsync(StateHasChanged)` aggiorna la UI immediatamente

## Come avviene l'aggiornamento automatico dei treni nei binari

### Ingresso treni (arrivo)

- **Immediato** (evento): quando AddTrain salva un treno con arrivo=ora
- **Periodico** (refresh ogni 5 tick): per treni con arrivo futuro che diventano
  attuali mentre la simulazione è in corso

### Uscita treni (partenza)

- **Tick fase 3**: ad ogni tick, i treni con `DepartureTime <= DateTime.UtcNow`
  vengono rimossi da tutte le zone che occupano
- **Uscita naturale**: treni che raggiungono la fine del binario escono via
  MoveType.Depart (logica preesistente)

Il primo meccanismo che scatta (partenza oraria o uscita fisica) rimuove il treno.
