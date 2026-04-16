# Fix: gestione binari, dropdown AddTrain, manutenzione automatica

## Causa del problema

### Perché compariva "N/A"

Il campo `Train.AssignedPlatform` è `int?` (nullable). La pagina AddTrain non offriva
alcun modo per selezionare un binario, quindi il valore restava `null`.
Nella Dashboard, `@(t.AssignedPlatform?.ToString() ?? "N/A")` mostrava "N/A".

### Perché i binari non c'erano

La tabella `sectors` nel DB poteva essere vuota: nessun seed o inizializzazione
automatica era presente nel codice Blazor. I 4 binari transit (corrispondenti
ai track 1-4 del simulatore) non venivano creati.

### Perché la manutenzione non funzionava

Non esisteva logica per creare automaticamente record nella tabella `maintenance`
quando un treno veniva inserito. La tabella `maintenance` richiede un riferimento
a `trains_log` (non a `trains`), quindi serviva anche creare un record TrainsLog.

## Modifiche effettuate

### 1. Program.cs — Seed settori

Aggiunto blocco all'avvio dell'app che verifica se la tabella `sectors` è vuota.
Se sì, crea 4 settori transit:
- PlatformNumber 1-4
- Length 20 (corrispondente ai 20 zone dei transit tracks del simulatore)
- IsAvailable true, IsStop false

### 2. AddTrain.razor — Dropdown binario + checkbox manutenzione

- Aggiunto campo `<InputSelect>` "Binario" con opzioni caricate dal DB (settori con PlatformNumber)
- Aggiunto checkbox "In manutenzione"

### 3. AddTrain.razor.cs — Logica completa

- `OnInitializedAsync`: carica i settori dal DB
- `SaveTrain`:
  - Imposta `AssignedPlatform` dal valore selezionato nel dropdown
  - Se "In manutenzione" è attivo:
    1. Crea un record `TrainsLog` con i dati del treno (richiesto come FK)
    2. Crea un record `Maintenance` collegato al TrainsLog (start=ora, end=+2h)
  - Il treno appare nella tabella "Treni in Manutenzione" della Dashboard

### 4. SimulationService.cs — Preferenza binario

`AddTrainFromDatabase` ora:
1. Se il treno ha `AssignedPlatform`, cerca il track corrispondente nel simulatore
2. Verifica che sia libero e di tipo transit
3. Se disponibile, piazza il treno lì
4. Se non disponibile (o nessun binario assegnato), fallback al primo transit track libero

## Come sono gestiti i binari

- I 4 binari transit (PlatformNumber 1-4) vengono creati automaticamente all'avvio se assenti
- Corrispondono ai SimTrack con Id 1-4 del simulatore
- Il dropdown in AddTrain li carica dal DB, quindi sono sempre sincronizzati
- Se in futuro si aggiungono binari nel DB, appariranno automaticamente nel dropdown

## Come funziona la logica della manutenzione

1. L'utente spunta "In manutenzione" nel form AddTrain
2. Il treno viene salvato normalmente nella tabella `trains`
3. Viene creato un record `trains_log` (copia dei dati, + tempi effettivi = ora)
4. Viene creato un record `maintenance` con:
   - `train_id` = id del TrainsLog appena creato
   - `maintenance_start` = ora corrente
   - `maintenance_end` = ora + 2 ore
5. La Dashboard carica `maintenance` con `Include(m => m.TrainLog)` e mostra il treno
   nella sezione "Treni in Manutenzione"
