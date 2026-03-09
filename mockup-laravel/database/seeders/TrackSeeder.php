<?php

namespace Database\Seeders;

use Illuminate\Database\Seeder;
use App\Models\Track;
use App\Models\Zone;

class TrackSeeder extends Seeder
{
    /**
     * Run the database seeds.
     */
    public function run(): void
    {
        // Create transit tracks
        for ($i = 1; $i <= 4; $i++) {
            $track = Track::create([
                'label' => 'Transit ' . $i,
                'type' => 'transit',
                'max_zones' => 20,
            ]);

            for ($j = 1; $j <= 20; $j++) {
                Zone::create([
                    'track_id' => $track->id,
                    'zone_index' => $j,
                ]);
            }
        }

        // Create maintenance tracks
        for ($i = 1; $i <= 2; $i++) {
            $track = Track::create([
                'label' => 'Maintenance ' . $i,
                'type' => 'maintenance',
                'max_zones' => 10,
            ]);

            for ($j = 1; $j <= 10; $j++) {
                Zone::create([
                    'track_id' => $track->id,
                    'zone_index' => $j,
                ]);
            }
        }
    }
}
