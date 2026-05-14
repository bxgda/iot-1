-- Staging tabela sa TEXT kolonama za sirovi CSV uvoz
CREATE TEMP TABLE staging_readings (
    ts_raw      TEXT,
    device      TEXT,
    co          DOUBLE PRECISION,
    humidity    DOUBLE PRECISION,
    light       TEXT,
    lpg         DOUBLE PRECISION,
    motion      TEXT,
    smoke       DOUBLE PRECISION,
    temp        DOUBLE PRECISION
);

-- Uvoz CSV-a u staging tabelu
COPY staging_readings FROM '/data/iot_telemetry_data.csv' WITH (FORMAT csv, HEADER true);

-- Konverzija i unos u pravu tabelu
INSERT INTO sensor_readings (ts, device_id, co, humidity, light, lpg, motion, smoke, temp)
SELECT
    to_timestamp(ts_raw::DOUBLE PRECISION) AT TIME ZONE 'UTC',
    device,
    co,
    humidity,
    light::BOOLEAN,
    lpg,
    motion::BOOLEAN,
    smoke,
    temp
FROM staging_readings;

-- Ciscenje
DROP TABLE staging_readings;
