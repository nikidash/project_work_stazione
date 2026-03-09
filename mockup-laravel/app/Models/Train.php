<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Train extends Model
{
    use HasFactory;

    protected $fillable = [
        'code',
        'type',
        'total_wagons',
        'length_in_zones',
        'priority_level',
        'accumulated_delay',
        'status',
        'operation_end_time',
    ];

    protected $casts = [
        'operation_end_time' => 'datetime',
    ];

    public function zones()
    {
        return $this->hasMany(Zone::class, 'occupied_by_train_id');
    }
}
