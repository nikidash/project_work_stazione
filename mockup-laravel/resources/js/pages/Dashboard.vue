<template>
    <div class="min-h-screen bg-gray-50 font-sans text-gray-900">
        <!-- Header -->
        <header class="bg-white shadow-sm border-b border-gray-200 sticky top-0 z-10">
            <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 h-16 flex items-center justify-between">
                <h1 class="text-xl font-bold text-gray-800 flex items-center gap-2">
                    <span class="text-blue-600">🚆</span> Station Dashboard
                </h1>
                <div class="flex items-center gap-4">
                    <div class="text-sm text-gray-500">
                        Tick: <span class="font-mono font-medium text-gray-700">{{ simulation ? simulation.tickCount : 0 }}</span>
                    </div>
                    <div class="h-4 w-px bg-gray-300"></div>
                    <span :class="{'bg-emerald-100 text-emerald-700': isRunning, 'bg-gray-100 text-gray-600': !isRunning}" class="text-xs font-bold px-2 py-1 rounded uppercase tracking-wide">
                        {{ isRunning ? 'Running' : 'Paused' }}
                    </span>
                </div>
            </div>
        </header>

        <main class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
            <!-- Controls Section -->
            <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 mb-8">
                <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
                    <!-- Action Buttons -->
                    <div class="flex flex-wrap items-center gap-3">
                        <button @click="play" :disabled="isRunning" :class="{'opacity-50 cursor-not-allowed': isRunning}" class="flex items-center gap-2 px-5 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white font-medium rounded-lg shadow-sm transition-all focus:ring-2 focus:ring-emerald-500 focus:ring-offset-1">
                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.752 11.168l-3.197-2.132A1 1 0 0010 9.87v4.263a1 1 0 001.555.832l3.197-2.132a1 1 0 000-1.664z"></path><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                            Play
                        </button>
                        <button @click="pause" :disabled="!isRunning" :class="{'opacity-50 cursor-not-allowed': !isRunning}" class="flex items-center gap-2 px-5 py-2.5 bg-amber-500 hover:bg-amber-600 text-white font-medium rounded-lg shadow-sm transition-all focus:ring-2 focus:ring-amber-500 focus:ring-offset-1">
                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 9v6m4-6v6m7-3a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
                            Pause
                        </button>
                        <button @click="step" class="flex items-center gap-2 px-5 py-2.5 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg shadow-sm transition-all focus:ring-2 focus:ring-blue-500 focus:ring-offset-1">
                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z"></path></svg>
                            Step
                        </button>
                        <button @click="reset" class="flex items-center gap-2 px-5 py-2.5 bg-rose-600 hover:bg-rose-700 text-white font-medium rounded-lg shadow-sm transition-all focus:ring-2 focus:ring-rose-500 focus:ring-offset-1 ml-auto lg:ml-0">
                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path></svg>
                            Reset
                        </button>
                    </div>

                    <!-- Speed Control -->
                    <div class="flex items-center gap-4 bg-gray-50 px-4 py-3 rounded-lg border border-gray-200 w-full lg:w-auto">
                        <label class="text-sm font-medium text-gray-700 whitespace-nowrap">Speed ({{ tickSpeed }}ms)</label>
                        <input type="range" v-model.number="tickSpeed" min="200" max="5000" step="100" class="w-full md:w-48 h-2 bg-gray-300 rounded-lg appearance-none cursor-pointer accent-blue-600">
                    </div>
                </div>
            </div>

            <!-- Tracks Grid -->
            <div v-if="!tracks.length" class="flex flex-col items-center justify-center py-16 bg-white rounded-xl border-2 border-dashed border-gray-200 text-gray-400">
                <svg class="w-16 h-16 mb-4 text-gray-300" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10"></path></svg>
                <p class="text-lg font-medium text-gray-500">No tracks available</p>
                <p class="text-sm">Waiting for simulation data...</p>
            </div>

            <div v-else class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
                <div v-for="track in tracks" :key="track.id" class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden hover:shadow-md transition-shadow">
                    <TrackComponent :track="track" />
                </div>
            </div>
            
            <div class="mt-8 text-center">
                <a href="/analytics" class="inline-flex items-center text-sm font-medium text-blue-600 hover:text-blue-800 hover:underline">
                    View Detailed Analytics
                    <svg class="w-4 h-4 ml-1" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 8l4 4m0 0l-4 4m4-4H3"></path></svg>
                </a>
            </div>
        </main>
    </div>
</template>

<script>
import axios from 'axios';
import TrackComponent from '@/components/TrackComponent.vue';
import Simulation from '@/services/Simulation';

export default {
    components: {
        TrackComponent,
    },
    data() {
        return {
            tracks: [],
            simulation: null,
            tickSpeed: 1000,
        };
    },
    computed: {
        isRunning() {
            return this.simulation ? this.simulation.isRunning : false;
        }
    },
    watch: {
        tickSpeed(newSpeed) {
            if (this.simulation) {
                this.simulation.setSpeed(newSpeed);
            }
        }
    },
    methods: {
        play() {
            this.simulation?.play();
        },
        pause() {
            this.simulation?.pause();
        },
        step() {
            this.simulation?.step();
        },
        reset() {
            this.simulation?.reset();
        },
        async fetchStatus() {
            try {
                const response = await axios.get('/api/station/status');
                this.tracks = response.data;
                
                // Initialize or update simulation with fetched tracks
                if (!this.simulation) {
                    this.simulation = new Simulation(this.tracks);
                    this.simulation.setSpeed(this.tickSpeed);
                    // Force update on simulation tick
                    this.simulation.subscribe(() => {
                        this.$forceUpdate();
                    });
                } else {
                    this.simulation.setTracks(this.tracks);
                }
            } catch (error) {
                console.error("Error fetching station status:", error);
            }
        },
    },
    mounted() {
        this.fetchStatus();
    },
    beforeDestroy() {
        this.simulation?.pause();
    },
};
</script>