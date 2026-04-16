# Fix: simulatore collegato al database, rimosso spawn interno

## Come funzionava prima il simulatore (separato)

Il simulatore (`SimulationService`) aveva 3 fasi nel metodo `Tick()`:

1. **Fase 1**: Scansione treni esistenti, pianificazione movimenti (direzione, sosta, partenza)
2. **Fase 2**: Applicazione atomica dei movimenti pianificati
3. **Fase 3**: Generazione casuale di nuovi treni (20% probabilità per tick) tramite `SpawnTrain()`

Il metodo `SpawnTrain()` creava treni con tipo, lunghezza e direzione casuali,
piazzandoli su un binario disponibile. Questi treni non avevano alcun legame
con il database: erano dati interni del simulatore.

Il metodo `AddTrainFromDatabase()` esisteva già (aggiunto nel ciclo precedente)
ma il simulatore continuava anche a generare treni propri.

## Cosa è stato cambiato

### SimulationService.cs

- **Rimosso** il metodo `SpawnTrain()` (circa 60 righe)
- **Rimossa** la fase 3 dal `Tick()` (il blocco `if (_random.NextDouble() < 0.2)`)
- `Tick()` ora ha solo 2 fasi: pianificazione movimenti + applicazione atomica
- `AddTrainFromDatabase()` è ora l'UNICO modo per aggiungere treni al simulatore
- `InitializeTracks()` e `Reset()` invariati (creano i 6 binari vuoti)

### Simulator.razor.cs

- Aggiunto `[Inject] FerroviaApiDbContext Db` per accesso al database
- `OnInitialized()` → `OnInitializedAsync()`:
  - Sottoscrizione evento `TrainNotification.OnTrainAdded` (invariata)
  - **Nuovo**: carica tutti i treni dal DB con `Db.Trains.ToListAsync()`
  - Per ogni treno, chiama `SimService.AddTrainFromDatabase(t)`
- `DoReset()` → `async Task DoReset()`:
  - Dopo il reset dei binari, ricarica i treni dal DB
  - Il simulatore torna allo stato iniziale con i treni attuali del database

## Come ora legge dal database

1. **All'avvio** (OnInitializedAsync): il simulatore carica `Db.Trains` e piazza
   ogni treno sul binario corrispondente (AssignedPlatform → track Id nel simulatore)
2. **In tempo reale** (evento OnTrainAdded): quando AddTrain salva un nuovo treno,
   `TrainNotificationService` notifica il simulatore che lo piazza immediatamente
3. **Dopo reset**: il simulatore ricarica tutti i treni dal DB

## Come avviene l'aggiornamento automatico

Il flusso completo:

1. L'utente compila il form in AddTrain e clicca Salva
2. `AddTrain.razor.cs` salva il treno nel DB con `Db.SaveChangesAsync()`
3. Chiama `TrainNotification.NotifyTrainAdded(train)`
4. `TrainNotificationService` (singleton) invoca l'evento `OnTrainAdded`
5. `Simulator.razor.cs` riceve l'evento in `OnTrainAddedFromDb`
6. Chiama `SimService.AddTrainFromDatabase(train)` che piazza il treno nel simulatore
7. Chiama `InvokeAsync(StateHasChanged)` per aggiornare la UI

Il treno appare nel binario corretto e inizia a muoversi al prossimo tick,
senza necessità di refresh manuale.
