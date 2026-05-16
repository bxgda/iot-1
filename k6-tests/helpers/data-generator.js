/**
 * Helper za generisanje realisticnih IoT senzorskih podataka.
 * Vrednosti su u opsezima koji odgovaraju Smart Home datasetu.
 */

// Uredjaji iz dataseta
export const DEVICES = [
    "b8:27:eb:bf:9d:51",
    "00:0f:00:70:91:0a",
    "1c:bf:ce:15:ec:4d",
];

// Vremenski opseg dataseta (Jul 2020)
export const TIME_FROM = 1594512000;  // 2020-07-12T00:00:00Z
export const TIME_TO = 1594598400;    // 2020-07-13T00:00:00Z

/**
 * Generise jedno senzorsko ocitavanje sa random vrednostima u realisticnim opsezima.
 */
export function generateReading() {
    return {
        ts: TIME_FROM + Math.random() * (TIME_TO - TIME_FROM),
        device_id: DEVICES[Math.floor(Math.random() * DEVICES.length)],
        co: Math.random() * 0.01,
        humidity: 40 + Math.random() * 40,        // 40-80%
        light: Math.random() > 0.5,
        lpg: Math.random() * 0.01,
        motion: Math.random() > 0.7,
        smoke: Math.random() * 0.03,
        temp: 18 + Math.random() * 12,            // 18-30°C
    };
}

/**
 * Generise batch od N ocitavanja.
 */
export function generateBatch(n) {
    const readings = [];
    for (let i = 0; i < n; i++) {
        readings.push(generateReading());
    }
    return readings;
}

/**
 * Vraca random uredjaj iz dataseta.
 */
export function randomDevice() {
    return DEVICES[Math.floor(Math.random() * DEVICES.length)];
}
