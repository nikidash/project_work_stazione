<?php

use Illuminate\Http\Request;
use Illuminate\Support\Facades\Route;
use App\Http\Controllers\StationController;

Route::prefix('simulation')->group(function () {
    Route::post('tick', [StationController::class, 'tick']);
    Route::post('reset', [StationController::class, 'reset']);
});

Route::prefix('station')->group(function () {
    Route::get('status', [StationController::class, 'status']);
});

Route::get('analytics', [StationController::class, 'analytics']);
