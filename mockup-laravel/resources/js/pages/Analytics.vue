<template>
    <div class="p-8">
        <h1 class="text-2xl font-bold">Analytics</h1>
        <div class="mt-8 grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
                <h2 class="text-xl font-semibold">Average Delay per Train Type</h2>
                <ul>
                    <li v-for="(delay, type) in analytics.average_delay_per_type" :key="type">
                        <span class="font-bold">{{ type }}:</span> {{ delay.toFixed(2) }} minutes
                    </li>
                </ul>
            </div>
            <div>
                <h2 class="text-xl font-semibold">Total Incidents</h2>
                <p class="text-3xl font-bold">{{ analytics.total_incidents }}</p>
            </div>
        </div>
        <div class="mt-8">
            <a href="/dashboard" class="text-blue-500 hover:underline">Back to Dashboard</a>
        </div>
    </div>
</template>

<script>
import axios from 'axios';

export default {
    data() {
        return {
            analytics: {
                average_delay_per_type: {},
                total_incidents: 0,
            },
        };
    },
    async mounted() {
        const response = await axios.get('/api/analytics');
        this.analytics = response.data;
    },
};
</script>
