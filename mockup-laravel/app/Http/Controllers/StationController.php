<?php

namespace App\Http\Controllers;

use App\Services\SimulationEngine;
use Illuminate\Http\Request;
use App\Models\Track;
use App\Models\Train;
use App\Models\Zone;
use App\Models\SimulationLog;

class StationController extends Controller
{
    public function tick(SimulationEngine $simulationEngine)
    {
        $simulationEngine->tick();
        return response()->json(['message' => 'Simulation ticked.']);
    }

    public function reset()
    {
        Train::query()->delete();
        Zone::query()->update(['is_occupied' => false, 'occupied_by_train_id' => null]);
        SimulationLog::query()->delete();
        return response()->json(['message' => 'Simulation reset.']);
    }

    public function status()
    {
        $tracks = Track::with('zones.train')->get();
        return response()->json($tracks);
    }

    public function analytics()
    {
        $avgDelay = Train::groupBy('type')
            ->selectRaw('type, avg(accumulated_delay) as avg_delay')
            ->pluck('avg_delay', 'type');

        $totalIncidents = SimulationLog::where('event', 'Priority Conflict')->count(); 

        return response()->json([
            'average_delay_per_type' => $avgDelay,
            'total_incidents' => $totalIncidents,
        ]);
    }
}
