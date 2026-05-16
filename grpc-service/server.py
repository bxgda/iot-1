"""
gRPC server za IoT senzorske podatke.

Pokretanje:
    docker compose up --build
    (ili lokalno: python server.py uz pokrenut PostgreSQL)

Testiranje (PowerShell — koristi cmd /c wrapper):

    # Lista servisa
    grpcurl -plaintext localhost:50051 list

    # Dohvati ocitavanja za uredjaj
    cmd /c "echo {""device_id"": ""b8:27:eb:bf:9d:51"", ""from_ts"": 1594512000, ""to_ts"": 1594515600, ""limit"": 3} | grpcurl -plaintext -d @ localhost:50051 sensor.SensorService/GetReadings"

    # Selektivno — samo temp i humidity (Scenario B)
    cmd /c "echo {""device_id"": ""b8:27:eb:bf:9d:51"", ""from_ts"": 1594512000, ""to_ts"": 1594515600, ""fields"": [""temp"", ""humidity""], ""limit"": 3} | grpcurl -plaintext -d @ localhost:50051 sensor.SensorService/GetSelectedFields"

    # Agregacije (Scenario C)
    cmd /c "echo {""device_id"": ""b8:27:eb:bf:9d:51"", ""from_ts"": 1594512000, ""to_ts"": 1594598400} | grpcurl -plaintext -d @ localhost:50051 sensor.SensorService/GetAggregation"

    # Unos jednog ocitavanja (Scenario A)
    cmd /c "echo {""ts"": 1594512000, ""device_id"": ""test:device:01"", ""co"": 0.005, ""humidity"": 55.0, ""light"": false, ""lpg"": 0.007, ""motion"": true, ""smoke"": 0.02, ""temp"": 23.5} | grpcurl -plaintext -d @ localhost:50051 sensor.SensorService/IngestReading"
"""

import time
from concurrent import futures
from datetime import datetime, timezone

import grpc
from grpc_reflection.v1alpha import reflection

import sensor_pb2
import sensor_pb2_grpc
from db import get_connection, release_connection


class SensorServiceServicer(sensor_pb2_grpc.SensorServiceServicer):
    """Implementacija svih 5 gRPC metoda za IoT senzorske podatke."""

    # ==================== SCENARIO A ====================

    def IngestReading(self, request, context):
        """Unos jednog senzorskog ocitavanja."""
        conn = get_connection()
        try:
            cur = conn.cursor()
            cur.execute(
                """
                INSERT INTO sensor_readings (ts, device_id, co, humidity, light, lpg, motion, smoke, temp)
                VALUES (to_timestamp(%s) AT TIME ZONE 'UTC', %s, %s, %s, %s, %s, %s, %s, %s)
                """,
                (
                    request.ts,
                    request.device_id,
                    request.co,
                    request.humidity,
                    request.light,
                    request.lpg,
                    request.motion,
                    request.smoke,
                    request.temp,
                ),
            )
            conn.commit()
            cur.close()
            return sensor_pb2.IngestResponse(success=True, count=1)
        except Exception as e:
            conn.rollback()
            context.set_details(str(e))
            context.set_code(grpc.StatusCode.INTERNAL)
            return sensor_pb2.IngestResponse(success=False, count=0)
        finally:
            release_connection(conn)

    def IngestBatch(self, request, context):
        """Batch unos vise senzorskih ocitavanja."""
        conn = get_connection()
        try:
            cur = conn.cursor()
            from psycopg2.extras import execute_values

            data = [
                (
                    r.ts,
                    r.device_id,
                    r.co,
                    r.humidity,
                    r.light,
                    r.lpg,
                    r.motion,
                    r.smoke,
                    r.temp,
                )
                for r in request.readings
            ]

            execute_values(
                cur,
                """
                INSERT INTO sensor_readings (ts, device_id, co, humidity, light, lpg, motion, smoke, temp)
                VALUES %s
                """,
                [
                    (
                        datetime.fromtimestamp(d[0], tz=timezone.utc),
                        d[1],
                        d[2],
                        d[3],
                        d[4],
                        d[5],
                        d[6],
                        d[7],
                        d[8],
                    )
                    for d in data
                ],
            )
            conn.commit()
            cur.close()
            return sensor_pb2.IngestResponse(success=True, count=len(data))
        except Exception as e:
            conn.rollback()
            context.set_details(str(e))
            context.set_code(grpc.StatusCode.INTERNAL)
            return sensor_pb2.IngestResponse(success=False, count=0)
        finally:
            release_connection(conn)

    # ==================== SCENARIO B, C ====================

    def GetReadings(self, request, context):
        """Citanje ocitavanja po device_id i vremenskom opsegu."""
        conn = get_connection()
        try:
            cur = conn.cursor()
            limit = request.limit if request.limit > 0 else 100

            cur.execute(
                """
                SELECT ts, device_id, co, humidity, light, lpg, motion, smoke, temp
                FROM sensor_readings
                WHERE device_id = %s
                  AND ts >= to_timestamp(%s) AT TIME ZONE 'UTC'
                  AND ts <= to_timestamp(%s) AT TIME ZONE 'UTC'
                ORDER BY ts
                LIMIT %s
                """,
                (request.device_id, request.from_ts, request.to_ts, limit),
            )

            rows = cur.fetchall()
            cur.close()

            readings = []
            for row in rows:
                readings.append(
                    sensor_pb2.SensorReading(
                        ts=row[0].timestamp(),
                        device_id=row[1],
                        co=row[2] or 0.0,
                        humidity=row[3] or 0.0,
                        light=row[4] or False,
                        lpg=row[5] or 0.0,
                        motion=row[6] or False,
                        smoke=row[7] or 0.0,
                        temp=row[8] or 0.0,
                    )
                )

            return sensor_pb2.ReadingsResponse(readings=readings)
        except Exception as e:
            context.set_details(str(e))
            context.set_code(grpc.StatusCode.INTERNAL)
            return sensor_pb2.ReadingsResponse()
        finally:
            release_connection(conn)

    def GetSelectedFields(self, request, context):
        """Selekcija specificnih polja — klijent bira koja polja zeli."""
        conn = get_connection()
        try:
            cur = conn.cursor()
            limit = request.limit if request.limit > 0 else 100

            # Dozvoljene kolone za selekciju
            allowed = {"co", "humidity", "light", "lpg", "motion", "smoke", "temp"}
            requested = [f for f in request.fields if f in allowed]

            if not requested:
                context.set_details("No valid fields requested.")
                context.set_code(grpc.StatusCode.INVALID_ARGUMENT)
                return sensor_pb2.SelectedFieldsResponse()

            # Uvek dohvatamo ts i device_id + trazena polja
            columns = ["ts", "device_id"] + requested
            columns_sql = ", ".join(columns)

            cur.execute(
                f"""
                SELECT {columns_sql}
                FROM sensor_readings
                WHERE device_id = %s
                  AND ts >= to_timestamp(%s) AT TIME ZONE 'UTC'
                  AND ts <= to_timestamp(%s) AT TIME ZONE 'UTC'
                ORDER BY ts
                LIMIT %s
                """,
                (request.device_id, request.from_ts, request.to_ts, limit),
            )

            rows = cur.fetchall()
            cur.close()

            # Mapiranje tipova: numeric_values za double, bool_values za bool
            bool_fields = {"light", "motion"}
            numeric_fields = {"co", "humidity", "lpg", "smoke", "temp"}

            results = []
            for row in rows:
                fv = sensor_pb2.FieldValues(
                    ts=row[0].timestamp(),
                    device_id=row[1],
                )

                for i, field_name in enumerate(requested):
                    value = row[i + 2]  # +2 jer ts i device_id su prva dva
                    if field_name in bool_fields:
                        fv.bool_values[field_name] = value or False
                    elif field_name in numeric_fields:
                        fv.numeric_values[field_name] = value or 0.0

                results.append(fv)

            return sensor_pb2.SelectedFieldsResponse(readings=results)
        except Exception as e:
            context.set_details(str(e))
            context.set_code(grpc.StatusCode.INTERNAL)
            return sensor_pb2.SelectedFieldsResponse()
        finally:
            release_connection(conn)

    def GetAggregation(self, request, context):
        """Agregacije (avg, min, max) za sve numericke senzorske kolone."""
        conn = get_connection()
        try:
            cur = conn.cursor()

            cur.execute(
                """
                SELECT
                    AVG(co), MIN(co), MAX(co),
                    AVG(humidity), MIN(humidity), MAX(humidity),
                    AVG(lpg), MIN(lpg), MAX(lpg),
                    AVG(smoke), MIN(smoke), MAX(smoke),
                    AVG(temp), MIN(temp), MAX(temp),
                    COUNT(*)
                FROM sensor_readings
                WHERE device_id = %s
                  AND ts >= to_timestamp(%s) AT TIME ZONE 'UTC'
                  AND ts <= to_timestamp(%s) AT TIME ZONE 'UTC'
                """,
                (request.device_id, request.from_ts, request.to_ts),
            )

            row = cur.fetchone()
            cur.close()

            if row is None or row[15] == 0:
                context.set_details("No readings found for specified device and time range.")
                context.set_code(grpc.StatusCode.NOT_FOUND)
                return sensor_pb2.AggregationResponse()

            return sensor_pb2.AggregationResponse(
                avg_co=row[0] or 0.0,
                min_co=row[1] or 0.0,
                max_co=row[2] or 0.0,
                avg_humidity=row[3] or 0.0,
                min_humidity=row[4] or 0.0,
                max_humidity=row[5] or 0.0,
                avg_lpg=row[6] or 0.0,
                min_lpg=row[7] or 0.0,
                max_lpg=row[8] or 0.0,
                avg_smoke=row[9] or 0.0,
                min_smoke=row[10] or 0.0,
                max_smoke=row[11] or 0.0,
                avg_temp=row[12] or 0.0,
                min_temp=row[13] or 0.0,
                max_temp=row[14] or 0.0,
                count=row[15],
            )
        except Exception as e:
            context.set_details(str(e))
            context.set_code(grpc.StatusCode.INTERNAL)
            return sensor_pb2.AggregationResponse()
        finally:
            release_connection(conn)


def serve():
    """Pokrece gRPC server na portu 50051."""
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    sensor_pb2_grpc.add_SensorServiceServicer_to_server(SensorServiceServicer(), server)

    # Reflection — omogucava grpcurl da otkrije servise bez .proto fajla
    service_names = (
        sensor_pb2.DESCRIPTOR.services_by_name["SensorService"].full_name,
        reflection.SERVICE_NAME,
    )
    reflection.enable_server_reflection(service_names, server)

    server.add_insecure_port("[::]:50051")
    print("[gRPC] Server pokrenut na portu 50051")
    server.start()
    server.wait_for_termination()


if __name__ == "__main__":
    serve()
