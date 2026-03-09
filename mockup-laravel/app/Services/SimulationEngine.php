<?php

namespace App\Services;

use App\Models\Train;
use App\Models\Track;
use App\Models\Zone;
use Carbon\Carbon;

class SimulationEngine
{
    public function tick()
    {
        $this->spawnTrain();
        $this->moveTrains();
    }

    private function spawnTrain()
    {
        if (rand(1, 10) !== 1) {
            return;
        }

        $isPassenger = rand(1, 100) <= 80;

        if ($isPassenger) {
            $rand = rand(1, 100);
            if ($rand <= 80) {
                $type = 'R';
                $priority = 4;
            } elseif ($rand <= 95) {
                $type = 'V';
                $priority = 3;
            } else {
                $type = 'F';
                $priority = 2;
            }
        } else {
            $rand = rand(1, 100);
            if ($rand <= 80) {
                $type = 'T';
                $priority = 1;
            } else {
                $type = 'S';
                $priority = 5;
            }
        }

        $totalWagons = rand(5, 16);
        $lengthInZones = ceil($totalWagons / 2);

        $code = strtoupper(substr($type, 0, 1)) . rand(1000, 9999);

        // Check if code already exists
        while (Train::where('code', $code)->exists()) {
            $code = strtoupper(substr($type, 0, 1)) . rand(1000, 9999);
        }

        Train::create([
            'code' => $code,
            'type' => $type,
            'total_wagons' => $totalWagons,
            'length_in_zones' => $lengthInZones,
            'priority_level' => $priority,
            'status' => 'arriving',
        ]);
    }

    private function moveTrains()
    {
        $trains = Train::whereIn('status', ['arriving', 'in_station'])->orderBy('priority_level', 'desc')->get();

        foreach ($trains as $train) {
            
            if ($train->status === 'arriving') {
                $entryTrack = Track::where('type', 'transit')->inRandomOrder()->first(); 
                if(!$entryTrack) continue;
                
                $firstZone = $entryTrack->zones()->where('zone_index', 1)->first();

                if (!$firstZone->is_occupied) {
                    $canOccupy = true;
                    for ($i = 0; $i < $train->length_in_zones; $i++) {
                        $zone = $entryTrack->zones()->where('zone_index', 1 + $i)->first();
                        if (!$zone || $zone->is_occupied) {
                            $canOccupy = false;
                            break;
                        }
                    }
                    
                    if ($canOccupy) {
                        for ($i = 0; $i < $train->length_in_zones; $i++) {
                            $zone = $entryTrack->zones()->where('zone_index', 1 + $i)->first();
                            $zone->update(['is_occupied' => true, 'occupied_by_train_id' => $train->id]);
                        }
                        $train->update(['status' => 'in_station']);
                    } else {
                        $train->increment('accumulated_delay');
                    }
                } else {
                    $train->increment('accumulated_delay');
                }
                continue; 
            }

            if ($train->updated_at->diffInMinutes(Carbon::now()) < 2) {
                 continue;
            }
            
            $occupiedZones = $train->zones()->orderBy('zone_index', 'desc')->get();
            if ($occupiedZones->isEmpty()) {
                continue; 
            }

            $headZone = $occupiedZones->first();
            $track = $headZone->track;
            $nextZoneIndex = $headZone->zone_index + 1;

            if ($nextZoneIndex > $track->max_zones) {
                $train->zones()->update(['is_occupied' => false, 'occupied_by_train_id' => null]);
                $train->update(['status' => 'departed']);
                continue;
            }
            
            $nextZone = $track->zones()->where('zone_index', $nextZoneIndex)->first();

            if ($nextZone->is_occupied) {
                $train->increment('accumulated_delay');
                continue;
            }

            $tailZone = $occupiedZones->last();
            $tailZone->update(['is_occupied' => false, 'occupied_by_train_id' => null]);
            
            $nextZone->update(['is_occupied' => true, 'occupied_by_train_id' => $train->id]);

            $train->touch();
        }
    }
}
