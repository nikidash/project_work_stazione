# Fix: treni DB non visibili nel simulatore

## Causa del problema

Errore di segno nella formula di calcolo degli indici zone in `SimulationService.cs`.

Nei metodi `SpawnTrain()` e `AddTrainFromDatabase()`, la formula per determinare
quali zone occupare era:

```
entryIndex - (i * direction)
```

Questa formula produce indici fuori range per treni che occupano più di 1 zona:
- direction=1 (ingresso da sinistra, entry=0): indici 0, -1, -2, ... (negativi)
- direction=-1 (ingresso da destra, entry=19): indici 19, 20, 21, ... (oltre il massimo)

Il controllo di disponibilità binari trovava sempre indici fuori range, quindi
`availableTracks` risultava vuoto e il metodo usciva senza piazzare il treno.

I treni random con `length=2` (zonesNeeded=1) funzionavano per caso: con `i=0`
la formula dà `entryIndex - 0 = entryIndex`, quindi il segno non conta.

I treni inseriti da DB hanno `Length >= 6` (vincolo nel model), quindi
`zonesNeeded >= 3`. Il bug si manifestava sempre e il treno non appariva mai.

## Modifiche effettuate

File modificato: `Services/SimulationService.cs`

4 righe cambiate (tutte con la stessa correzione di segno):

1. **SpawnTrain() - controllo disponibilità** (riga ~232):
   `entryIndex - (i * direction)` → `entryIndex + (i * direction)`

2. **SpawnTrain() - piazzamento zone** (riga ~267):
   `entryIndex - (i * direction)` → `entryIndex + (i * direction)`

3. **AddTrainFromDatabase() - controllo disponibilità** (riga ~289):
   `entryIndex - (i * direction)` → `entryIndex + (i * direction)`

4. **AddTrainFromDatabase() - piazzamento zone** (riga ~314):
   `entryIndex2 - (i * direction)` → `entryIndex2 + (i * direction)`

La logica di movimento in `Tick()` fase 2 (riposizionamento corpo dietro la testa)
usa correttamente il segno `-` e non è stata modificata.

## Perché ora funziona

Con la formula corretta `entryIndex + (i * direction)`:

| Caso | Calcolo | Risultato |
|------|---------|-----------|
| direction=1, entry=0, i=0 | 0 + 0 = 0 | zona 0 (OK) |
| direction=1, entry=0, i=1 | 0 + 1 = 1 | zona 1 (OK) |
| direction=1, entry=0, i=2 | 0 + 2 = 2 | zona 2 (OK) |
| direction=-1, entry=19, i=0 | 19 + 0 = 19 | zona 19 (OK) |
| direction=-1, entry=19, i=1 | 19 + (-1) = 18 | zona 18 (OK) |
| direction=-1, entry=19, i=2 | 19 + (-2) = 17 | zona 17 (OK) |

Le zone vengono ora occupate correttamente a partire dal punto di ingresso,
estendendosi nella direzione di marcia del treno.

Effetto collaterale positivo: anche i treni random con `length > 2` ora si
piazzano correttamente su più zone (prima funzionavano solo quelli con 1 zona).
