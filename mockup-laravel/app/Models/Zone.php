<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Zone extends Model
{
    use HasFactory;

    protected $fillable = [
        'track_id',
        'zone_index',
        'is_occupied',
        'occupied_by_train_id',
    ];

    public function track()
    {
        return $this->belongsTo(Track::class);
    }

    public function train()
    {
        return $this->belongsTo(Train::class, 'occupied_by_train_id');
    }
}
