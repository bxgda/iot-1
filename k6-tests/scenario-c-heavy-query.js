/**
 * Scenario C — Heavy Querying
 *
 * Slozeni upiti nad velikim opsegom istorijskih podataka (agregacije).
 * Fokus: performanse agregacionih upita i serijalizacija rezultata.
 *
 * Pokretanje:
 *   k6 run -e PROTOCOL=rest -e VUS=10 scenario-c-heavy-query.js
 *   k6 run -e PROTOCOL=rest -e VUS=100 scenario-c-heavy-query.js
 *   k6 run -e PROTOCOL=rest -e VUS=500 scenario-c-heavy-query.js
 *   k6 run -e PROTOCOL=graphql -e VUS=10 scenario-c-heavy-query.js
 *   k6 run -e PROTOCOL=grpc -e VUS=10 scenario-c-heavy-query.js
 */

import http from "k6/http";
import grpc from "k6/net/grpc";
import { check, sleep } from "k6";
import { Trend } from "k6/metrics";
import { randomDevice, TIME_FROM, TIME_TO } from "./helpers/data-generator.js";

// ==================== KONFIGURACIJA ====================

const PROTOCOL = __ENV.PROTOCOL || "rest";
const VUS = parseInt(__ENV.VUS) || 10;
const DURATION = __ENV.DURATION || "60s";

const REST_URL = "http://localhost:5000/api/readings/aggregate";
const GRAPHQL_URL = "http://localhost:5002/graphql";
const GRPC_HOST = "localhost:50051";

// Custom metrika za velicinu odgovora
const responseSize = new Trend("response_size_bytes");

export const options = {
    scenarios: {
        heavy_query: {
            executor: "constant-vus",
            vus: VUS,
            duration: DURATION,
        },
    },
    thresholds: {
        http_req_duration: ["p(95)<5000"],
    },
};

// ==================== gRPC KLIJENT ====================

const grpcClient = new grpc.Client();
grpcClient.load(["../grpc-service/protos"], "sensor.proto");

let grpcConnected = false;

// ==================== TEST FUNKCIJE ====================

function restHeavyQuery() {
    const device = randomDevice();
    // Ceo opseg dataseta — tezak upit
    const url = `${REST_URL}?deviceId=${encodeURIComponent(device)}&from=2020-07-12T00:00:00Z&to=2020-07-13T00:00:00Z`;

    const res = http.get(url);

    check(res, {
        "REST status 200": (r) => r.status === 200,
    });

    if (res.body) {
        responseSize.add(res.body.length);
    }
}

function grpcHeavyQuery() {
    if (!grpcConnected) {
        grpcClient.connect(GRPC_HOST, { plaintext: true, timeout: "5s" });
        grpcConnected = true;
    }

    const device = randomDevice();
    const res = grpcClient.invoke("sensor.SensorService/GetAggregation", {
        device_id: device,
        from_ts: TIME_FROM,
        to_ts: TIME_TO,
    });

    check(res, {
        "gRPC status OK": (r) => r && r.status === grpc.StatusOK,
    });

    if (res.message) {
        responseSize.add(JSON.stringify(res.message).length);
    }
}

function graphqlHeavyQuery() {
    const device = randomDevice();
    const query = `query {
        aggregation(
            deviceId: "${device}",
            from: "2020-07-12T00:00:00Z",
            to: "2020-07-13T00:00:00Z"
        ) {
            count
            avgCo minCo maxCo
            avgHumidity minHumidity maxHumidity
            avgLpg minLpg maxLpg
            avgSmoke minSmoke maxSmoke
            avgTemp minTemp maxTemp
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
            restHeavyQuery();
            break;
        case "grpc":
            grpcHeavyQuery();
            break;
        case "graphql":
            graphqlHeavyQuery();
            break;
        default:
            console.error(`Nepoznat protokol: ${PROTOCOL}`);
    }
}
