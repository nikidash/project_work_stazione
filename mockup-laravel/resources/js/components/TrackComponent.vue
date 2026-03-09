<template>
    <div class="p-4">
        <h3 class="text-lg font-semibold text-gray-800 flex items-center justify-between">
            <span>{{ track.label }}</span>
            <span class="text-sm font-normal text-gray-500 capitalize">{{ track.type }}</span>
        </h3>
        <div class="flex border-2 border-gray-300 h-12 rounded-md mt-2">
            <div
                v-for="(zone, index) in track.zones"
                :key="zone.id"
                class="flex-1 border-r-2 border-gray-300 last:border-r-0 flex items-center justify-center relative"
                :class="getZoneClass(zone)"
                :title="getZoneTitle(zone)"
            >
                <div v-if="isTrainHead(zone, index)" class="flex items-center text-white font-mono text-xs z-10">
                    <span v-if="zone.train.direction === -1" class="mr-1">◀</span>
                    <span>{{ zone.train.code }}</span>
                    <span v-if="zone.train.direction === 1" class="ml-1">▶</span>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
export default {
    props: {
        track: {
            type: Object,
            required: true,
        },
    },
    methods: {
        getZoneClass(zone) {
            if (!zone.is_occupied || !zone.train) {
                return 'bg-gray-200 hover:bg-gray-300 transition-colors';
            }
            // Add a subtle pattern for stopping trains
            if (zone.train.isStopping && this.track.type === 'platform') {
                 switch (zone.train.type) {
                    case 'R': case 'V': case 'F': return 'bg-blue-700 stopping-train-pattern'; // Passenger
                    case 'T': case 'S': return 'bg-red-700 stopping-train-pattern'; // Cargo
                    default: return 'bg-gray-800 stopping-train-pattern';
                }
            }
            switch (zone.train.type) {
                case 'R': case 'V': case 'F': return 'bg-blue-600'; // Passenger
                case 'T': case 'S': return 'bg-red-600'; // Cargo
                default: return 'bg-gray-700';
            }
        },
        isTrainHead(zone, index) {
            if (!zone.train) return false;
            // It's the head if it's the first zone of the train in the direction of travel
            const dir = zone.train.direction;
            if (dir === 1) { // Moving left to right
                return index === 0 || !this.track.zones[index - 1].train || this.track.zones[index - 1].train.id !== zone.train.id;
            }
            if (dir === -1) { // Moving right to left
                return index === this.track.zones.length - 1 || !this.track.zones[index + 1].train || this.track.zones[index + 1].train.id !== zone.train.id;
            }
            return false;
        },
        getZoneTitle(zone) {
            if (!zone.train) return `Zone ${zone.id}`;
            const stoppingStatus = zone.train.isStopping ? `Stopping (wait: ${zone.train.timeRemaining})` : 'Passing through';
            return `Train: ${zone.train.code} | Length: ${zone.train.length} cars | ${stoppingStatus}`;
        }
    },
};
</script>

<style scoped>
.stopping-train-pattern {
    background-image: repeating-linear-gradient(45deg, rgba(255, 255, 255, 0.1), rgba(255, 255, 255, 0.1) 1px, transparent 1px, transparent 5px);
}
</style>
