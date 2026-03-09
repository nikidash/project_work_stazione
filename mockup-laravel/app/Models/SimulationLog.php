<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class SimulationLog extends Model
{
    use HasFactory;

    protected $fillable = [
        'train_id',
        'event',
    ];

    public function train()
    {
        return $this->belongsTo(Train::class);
    }
}
