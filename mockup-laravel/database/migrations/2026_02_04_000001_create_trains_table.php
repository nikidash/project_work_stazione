<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
        Schema::create('trains', function (Blueprint $table) {
            $table->id();
            $table->string('code')->unique();
            $table->enum('type', ['R', 'V', 'F', 'T', 'S']);
            $table->integer('total_wagons');
            $table->integer('length_in_zones');
            $table->integer('priority_level');
            $table->integer('accumulated_delay')->default(0);
            $table->enum('status', ['arriving', 'in_station', 'maintenance', 'departed']);
            $table->timestamp('operation_end_time')->nullable();
            $table->timestamps();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('trains');
    }
};
