# Modifiche apportate al progetto StazioneFerroviaria

## Elenco file modificati

| File | Tipo modifica |
|------|--------------|
| Services/TrainNotificationService.cs | **NUOVO** |
| Services/SimulationService.cs | Modificato |
| Components/Pages/Simulator.razor.cs | Modificato |
| Components/Pages/AddTrain.razor | Modificato |
| Components/Pages/AddTrain.razor.cs | Modificato |
| Program.cs | Modificato |
| wwwroot/app.css | Modificato |

## Descrizione delle modifiche

### 1. TrainNotificationService.cs (NUOVO)
Servizio event-driven singleton che funge da ponte tra la pagina di inserimento e il simulatore.
Espone un evento `OnTrainAdded` di tipo `Action<Train>`.
Quando AddTrain salva un treno nel DB, invoca `NotifyTrainAdded(train)` che scatena l'evento.
Il simulatore si sottoscrive all'evento e aggiunge il treno alla simulazione in tempo reale.

**Motivazione:** soluzione leggera e coerente con Blazor Server — non servono SignalR (già integrato nel circuito) né polling. Un semplice pattern Observer è sufficiente.

### 2. SimulationService.cs
Aggiunto metodo `AddTrainFromDatabase(Train dbTrain)` che:
- Determina la direzione dal campo `Origin` (L* = destra, R* = sinistra)
- Calcola le zone necessarie dalla lunghezza
- Cerca un binario transit disponibile
- Crea un SimTrain con i dati del DB e lo piazza sul binario

Aggiunto `using FerroviaApi.Models` per il tipo Train.

**Motivazione:** permette al simulatore di ricevere treni reali dal database, non solo quelli generati casualmente.

### 3. Simulator.razor.cs
- Sostituito `System.Threading.Timer` con loop async `Task.Delay` + `CancellationTokenSource`. Elimina l'antipattern `async void` del callback timer e gestisce correttamente la cancellazione.
- Aggiunta sottoscrizione a `TrainNotificationService.OnTrainAdded`: quando un treno viene salvato nel DB, appare automaticamente nel simulatore senza refresh.
- `Dispose()` aggiornato: rimuove la sottoscrizione all'evento e cancella il token.
- La velocità si aggiorna al tick successivo (comportamento naturale con `Task.Delay`).

**Motivazione:** il loop async è più pulito e testabile del System.Threading.Timer. L'evento DB→simulatore rende l'esperienza reattiva.

### 4. AddTrain.razor
- Rimossi i campi data (ScheduledArrivalTime, ScheduledDepartureTime): vengono impostati automaticamente lato server.
- Aggiunto `@inject TrainNotificationService` per notificare il simulatore dopo il salvataggio.
- Aggiunta opzione "T" (Merci Transito) nel dropdown tipo treno, che era mancante rispetto al simulatore.
- Aggiunti messaggi di successo/errore visibili all'utente.
- Il form si resetta dopo un salvataggio riuscito, permettendo inserimenti multipli senza navigare altrove.

**Motivazione:** l'utente non doveva inserire date manualmente (requisito esplicito). I feedback visivi migliorano l'usabilità.

### 5. AddTrain.razor.cs
- `ScheduledArrivalTime` impostato a `DateTime.UtcNow` al momento del salvataggio.
- `ScheduledDepartureTime` impostato a `DateTime.UtcNow.AddMinutes(10)`.
- Dopo il salvataggio, invoca `TrainNotification.NotifyTrainAdded(train)`.
- Gestione errori con try/catch su `SaveChangesAsync()` e messaggio visibile.
- Valori di default realistici nel model (Origin="L1", Destination="L", TrainType="R", Length=6).

**Motivazione:** date automatiche server-side come da requisito. La notifica evento collega DB e simulatore.

### 6. Program.cs
Aggiunta registrazione `TrainNotificationService` come singleton.

**Motivazione:** singleton perché deve essere condiviso tra tutte le pagine (AddTrain scrive, Simulator legge).

### 7. wwwroot/app.css
Aggiunta classe CSS `.stopping-pattern` con il gradient a strisce diagonali per i treni in sosta.

**Motivazione:** il pattern era inline nel Simulator.razor (nella legenda) ma non era definito come classe riutilizzabile. Ora la classe è disponibile globalmente.

## Assunzioni fatte

1. **Direzione dal campo Origin:** L1/L2 = direzione destra (direction=1), R1/R2 = direzione sinistra (direction=-1). Basato sulla convenzione L=left entry, R=right entry.
2. **Treni dal DB solo su binari transit:** i treni inseriti dal form non vengono mai piazzati su binari maintenance (riservati alla simulazione random).
3. **Sosta per treni non in transito:** se `IsTransit=false`, il treno entra in sosta (3-7 tick) alla prima opportunità. Se `IsTransit=true`, attraversa senza fermarsi.
4. **Length nel form rispetta Range(6,16):** il vincolo DataAnnotations sul model Train viene validato dal DataAnnotationsValidator nel form.
5. **Singleton per TrainNotificationService:** essendo Blazor Server, un singleton è sufficiente per la comunicazione tra circuiti. Se si migrasse a WebAssembly, servirebbe un hub SignalR.
