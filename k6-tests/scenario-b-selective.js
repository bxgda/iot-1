/**
 * Scenario B — Selective Monitoring
 *
 * Simulira klijenta (npr. mobilna app) sa losom vezom koji trazi
 * samo 2 od 7 senzorskih vrednosti (temp i humidity).
 * Fokus: velicina odgovora i over-fetching.
 *
 * Pokretanje:
 *   k6 run -e PROTOCOL=rest -e VUS=10 scenario-b-selective.js
 *   k6 run -e PROTOCOL=rest -e VUS=100 scenario-b-selective.js
 *   k6 run -e PROTOCOL=rest -e VUS=500 scenario-b-selective.js
 *   k6 run -e PROTOCOL=graphql -e VUS=10 scenario-b-selective.js
 *   k6 run -e PROTOCOL=grpc -e VUS=10 scenario-b-selective.js
 */

import http from "k6/http";
import grpc from "k6/net/grpc";
import { check } from "k6";
import { Trend } from "k6/metrics";
import { randomDevice, TIME_FROM, TIME_TO } from "./helpers/data-generator.js";

// ==================== KONFIGURACIJA ====================

const PROTOCOL = __ENV.PROTOCOL || "rest";
const VUS = parseInt(__ENV.VUS) || 10;
const DURATION = __ENV.DURATION || "60s";

const REST_URL = "http://localhost:5000/api/readings/select";
const GRAPHQL_URL = "http://localhost:5002/graphql";
const GRPC_HOST = "localhost:50051";

// Custom metrika za velicinu odgovora
const responseSize = new Trend("response_size_bytes");

export const options = {
    scenarios: {
        selective: {
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

let grpcConnected = false;

// ==================== TEST FUNKCIJE ====================

function restSelective() {
    const device = randomDevice();
    const url = `${REST_URL}?deviceId=${encodeURIComponent(device)}&fields=temp,humidity&from=2020-07-12T00:00:00Z&to=2020-07-12T01:00:00Z&limit=50`;

    const res = http.get(url);

    check(res, {
        "REST status 200": (r) => r.status === 200,
    });

    if (res.body) {
        responseSize.add(res.body.length);
    }
}

function grpcSelective() {
    if (!grpcConnected) {
        grpcClient.connect(GRPC_HOST, { plaintext: true, timeout: "5s" });
        grpcConnected = true;
    }

    const device = randomDevice();
    const res = grpcClient.invoke("sensor.SensorService/GetSelectedFields", {
        device_id: device,
        from_ts: TIME_FROM,
        to_ts: TIME_FROM + 3600,  // 1 sat
        fields: ["temp", "humidity"],
        limit: 50,
    });

    check(res, {
        "gRPC status OK": (r) => r && r.status === grpc.StatusOK,
    });

    if (res.message) {
        responseSize.add(JSON.stringify(res.message).length);
    }
}

function graphqlSelective() {
    const device = randomDevice();
    const query = `query {
        readings(
            deviceId: "${device}",
            from: "2020-07-12T00:00:00Z",
            to: "2020-07-12T01:00:00Z",
            limit: 50
        ) {
            temp
            humidity
        }
    }`;

    const res = http.post(GRAPHQL_URL, JSON.stringify({ query }), {
        headers: { "Content-Type": "application/json" },
    });

    check(res, {
        "GraphQL status 200": (r) => r.status === 200,
        "GraphQL no errors": (r) => !JSON.parse(r.body).errors,
    });

    if (res.body) {
        responseSize.add(res.body.length);
    }
}

// ==================== MAIN ====================

export default function () {
    switch (PROTOCOL) {
        case "rest":
            restSelective();
            break;
        case "grpc":
            grpcSelective();
            break;
        case "graphql":
            graphqlSelective();
            break;
        default:
            console.error(`Nepoznat protokol: ${PROTOCOL}`);
    }
}
