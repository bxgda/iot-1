CREATE TABLE IF NOT EXISTS sensor_readings (
    id          BIGSERIAL PRIMARY KEY,
    ts          TIMESTAMPTZ     NOT NULL,
    device_id   VARCHAR(20)     NOT NULL,
    co          DOUBLE PRECISION,
    humidity    DOUBLE PRECISION,
    light       BOOLEAN,
    lpg         DOUBLE PRECISION,
    motion      BOOLEAN,
    smoke       DOUBLE PRECISION,
    temp        DOUBLE PRECISION
);

CREATE INDEX idx_readings_device_id ON sensor_readings (device_id);
CREATE INDEX idx_readings_ts ON sensor_readings (ts);
CREATE INDEX idx_readings_device_ts ON sensor_readings (device_id, ts);
