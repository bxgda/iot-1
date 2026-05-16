import os
import time
import psycopg2
from psycopg2 import pool

_connection_pool = None


def get_pool():
    """Kreira ili vraca postojeci connection pool ka PostgreSQL bazi."""
    global _connection_pool

    if _connection_pool is not None:
        return _connection_pool

    db_host = os.environ.get("DB_HOST", "localhost")
    db_name = os.environ.get("DB_NAME", "iot")
    db_user = os.environ.get("DB_USER", "iot_user")
    db_pass = os.environ.get("DB_PASS", "iot_pass")
    db_port = os.environ.get("DB_PORT", "5432")

    # Retry logika — ceka da PostgreSQL bude spreman
    for attempt in range(10):
        try:
            _connection_pool = pool.ThreadedConnectionPool(
                minconn=2,
                maxconn=10,
                host=db_host,
                database=db_name,
                user=db_user,
                password=db_pass,
                port=db_port,
            )
            print(f"[DB] Povezan na PostgreSQL ({db_host}:{db_port}/{db_name})")
            return _connection_pool
        except psycopg2.OperationalError as e:
            print(f"[DB] Pokusaj {attempt + 1}/10 — cekam PostgreSQL... ({e})")
            time.sleep(2)

    raise Exception("Nije moguce povezati se na PostgreSQL nakon 10 pokusaja.")


def get_connection():
    """Vraca konekciju iz pool-a."""
    return get_pool().getconn()


def release_connection(conn):
    """Vraca konekciju nazad u pool."""
    get_pool().putconn(conn)
