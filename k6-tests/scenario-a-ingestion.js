/**
 * Scenario A — High-Frequency Ingestion
 *
 * Simulira uredjaje koji salju podatke u kratkim intervalima.
 * Fokus: brzina upisa i overhead protokola.
 *
 * Pokretanje:
 *   k6 run -e PROTOCOL=rest -e VUS=10 scenario-a-ingestion.js
 *   k6 run -e PROTOCOL=rest -e VUS=100 scenario-a-ingestion.js
 *   k6 run -e PROTOCOL=rest -e VUS=500 scenario-a-ingestion.js
 *   k6 run -e PROTOCOL=graphql -e VUS=10 scenario-a-ingestion.js
 *   k6 run -e PROTOCOL=grpc -e VUS=10 scenario-a-ingestion.js
 */

import http from "k6/http";
import grpc from "k6/net/grpc";
import { check } from "k6";
import { generateReading } from "./helpers/data-generator.js";

// ==================== KONFIGURACIJA ====================

const PROTOCOL = __ENV.PROTOCOL || "rest";
const VUS = parseInt(__ENV.VUS) || 10;
const DURATION = __ENV.DURATION || "60s";

const REST_URL = "http://localhost:5000/api/readings";
const GRAPHQL_URL = "http://localhost:5002/graphql";
const GRPC_HOST = "localhost:50051";

export const options = {
    scenarios: {
        ingestion: {
            executor: "constant-vus",
            vus: VUS,
            duration: DURATION,
        },
    },
    thresholds: {
        http_req_duration: ["p(95)<2000"],
    },
};

// ==================== gRPC KLIJENT ====================

const grpcClient = new grpc.Client();
grpcClient.load(["../grpc-service/protos"], "sensor.proto");

// Konekcija se otvara jednom po VU (ne u svakoj iteraciji)
let grpcConnected = false;

// ==================== TEST FUNKCIJE ====================

function restIngest() {
    const reading = generateReading();
    const payload = JSON.stringify({
        ts: reading.ts,
        deviceId: reading.device_id,
        co: reading.co,
        humidity: reading.humidity,
        light: reading.light,
        lpg: reading.lpg,
        motion: reading.motion,
        smoke: reading.smoke,
        temp: reading.temp,
    });

    const res = http.post(REST_URL, payload, {
        headers: { "Content-Type": "application/json" },
    });

    check(res, {
        "REST status 201": (r) => r.status === 201,
    });
}

function grpcIngest() {
    if (!grpcConnected) {
        grpcClient.connect(GRPC_HOST, { plaintext: true, timeout: "5s" });
        grpcConnected = true;
    }

    const reading = generateReading();
    const res = grpcClient.invoke("sensor.SensorService/IngestReading", reading);

    check(res, {
        "gRPC status OK": (r) => r && r.status === grpc.StatusOK,
    });
}

function graphqlIngest() {
    const reading = generateReading();
    const query = `mutation {
        ingestReading(input: {
            ts: ${reading.ts},
            deviceId: "${reading.device_id}",
            co: ${reading.co},
            humidity: ${reading.humidity},
            light: ${reading.light},
            lpg: ${reading.lpg},
            motion: ${reading.motion},
            smoke: ${reading.smoke},
            temp: ${reading.temp}
        }) {
            id
        }
    }`;

    const res = http.post(GRAPHQL_URL, JSON.stringify({ query }), {
        headers: { "Content-Type": "application/json" },
    });

    check(res, {
        "GraphQL status 200": (r) => r.status === 200,
        "GraphQL no errors": (r) => !JSON.parse(r.body).errors,
    });
}

// ==================== MAIN ====================

export default function () {
    switch (PROTOCOL) {
        case "rest":
            restIngest();
            break;
        case "grpc":
            grpcIngest();
            break;
        case "graphql":
            graphqlIngest();
            break;
        default:
            console.error(`Nepoznat protokol: ${PROTOCOL}`);
    }
}
