export default class Simulation {
    constructor(tracks = []) {
        this.tracks = tracks;
        this.isRunning = false;
        this.intervalId = null;
        this.speed = 1000;
        this.tickCount = 0;
        this.listeners = [];
    }

    setTracks(tracks) {
        this.tracks = tracks;
        this.notify();
    }

    setSpeed(ms) {
        this.speed = ms;
        if (this.isRunning) {
            this.pause();
            this.play();
        }
    }

    play() {
        if (this.isRunning) return;
        this.isRunning = true;
        this.intervalId = setInterval(() => this.tick(), this.speed);
        this.notify();
    }

    pause() {
        this.isRunning = false;
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
        this.notify();
    }

    reset() {
        this.pause();
        this.tickCount = 0;
        this.tracks.forEach(track => {
            track.is_occupied = false;
            track.train = null;
            track.zones.forEach(zone => {
                zone.is_occupied = false;
                zone.train = null;
            });
        });
        this.notify();
    }

    step() {
        this.tick();
    }

    tick() {
        this.tickCount++;
        const trainMovements = [];
        const reservedZones = new Set(); // Prevents collisions

        // --- 1. Identify trains and plan movements ---
        const processedTrains = new Set();
        this.tracks.forEach(track => {
            for (let i = 0; i < track.zones.length; i++) {
                const zone = track.zones[i];
                if (!zone.train || processedTrains.has(zone.train.id)) continue;

                const train = zone.train;
                processedTrains.add(train.id);

                const trainZones = track.zones.filter(z => z.train && z.train.id === train.id);
                const headZone = train.direction === 1 ? trainZones[trainZones.length - 1] : trainZones[0];
                const headZoneIndex = track.zones.indexOf(headZone);
                const nextHeadIndex = headZoneIndex + train.direction;

                // --- Handle Waiting ---
                if (train.isStopping && train.timeRemaining > 0) {
                    train.timeRemaining--;
                    continue; // Keep waiting
                }
                // If waiting time is over, allow movement
                if (train.isStopping && train.timeRemaining <= 0) {
                    train.isStopping = false;
                }

                // --- Plan Maintenance Track Logic ---
                if (track.type === 'maintenance' && (nextHeadIndex < 0 || nextHeadIndex >= track.zones.length)) {
                    // Arrived at end of maintenance, start waiting
                    if (!train.isStopping) {
                        train.isStopping = true;
                        train.timeRemaining = Math.floor(Math.random() * 5) + 5; // Wait 5-9 ticks
                    } 
                    // Finished waiting, plan reversal
                    else {
                        trainMovements.push({ type: 'reverse', train });
                    }
                    continue;
                }

                // --- Plan Departure ---
                if (nextHeadIndex < 0 || nextHeadIndex >= track.zones.length) {
                    trainMovements.push({ type: 'depart', train, track });
                    continue;
                }
                
                // --- Plan Movement ---
                const nextZone = track.zones[nextHeadIndex];
                // Check if the very next zone is free or part of the same train
                const canMove = !reservedZones.has(nextZone.id) && (!nextZone.is_occupied || nextZone.train.id === train.id);

                if (canMove) {
                    // Reserve the *single* next zone for the head
                    reservedZones.add(nextZone.id);
                    trainMovements.push({ type: 'move', train, track, newHeadIndex: nextHeadIndex });
                }
            }
        });
        
        // --- 2. Apply movements atomically ---
        trainMovements.forEach(move => {
            const { train, track } = move;

            if (move.type === 'reverse') {
                train.direction *= -1; // Flip direction
                train.isStopping = false; // Ensure it can move after reversing
                return;
            }

            const currentZones = track.zones.filter(z => z.train && z.train.id === train.id);
            currentZones.forEach(z => {
                z.is_occupied = false;
                z.train = null;
            });

            if (move.type === 'move') {
                const headIndex = move.newHeadIndex;
                for (let i = 0; i < train.zones; i++) {
                    const zoneIndex = headIndex - (i * train.direction);
                     if (track.zones[zoneIndex]) {
                        track.zones[zoneIndex].is_occupied = true;
                        track.zones[zoneIndex].train = train;
                    }
                }
            }
        });


        // --- 3. Generate new trains ---
        if (Math.random() < 0.2) {
            const direction = Math.random() < 0.5 ? 1 : -1;
            const length = Math.floor(Math.random() * 4) + 2;
            const zonesNeeded = Math.ceil(length / 2);

            const availableTracks = this.tracks.filter(track => {
                if (track.type === 'maintenance' && track.zones.some(z => z.is_occupied)) return false;

                const entryIndex = direction === 1 ? 0 : track.zones.length - 1;
                for (let i = 0; i < zonesNeeded; i++) {
                    const checkZone = track.zones[entryIndex - (i * direction)];
                    if (!checkZone || checkZone.is_occupied) return false;
                }
                return true;
            });

            if (availableTracks.length > 0) {
                const track = availableTracks[Math.floor(Math.random() * availableTracks.length)];
                const entryIndex = direction === 1 ? 0 : track.zones.length - 1;
                const trainType = ['R', 'V', 'F', 'T', 'S'][Math.floor(Math.random() * 5)];
                const isStopping = track.type === 'platform' && Math.random() < 0.7;
                const timeRemaining = isStopping ? Math.floor(Math.random() * 3) + 2 : 0;

                const newTrain = {
                    id: `TR-${this.tickCount}-${track.id}`,
                    code: `${trainType}${Math.floor(Math.random() * 900) + 100}`,
                    type: trainType, length, zones: zonesNeeded, direction, isStopping, timeRemaining,
                };
                
                for (let i = 0; i < zonesNeeded; i++) {
                    const z = track.zones[entryIndex - (i * direction)];
                     if(z) {
                        z.is_occupied = true;
                        z.train = newTrain;
                    }
                }
            }
        }
        this.notify();
    }

    subscribe(callback) {
        this.listeners.push(callback);
    }

    notify() {
        this.listeners.forEach(callback => callback(this));
    }
}