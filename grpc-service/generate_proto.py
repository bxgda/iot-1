"""
Skripta za generisanje Python koda iz .proto fajla.
Pokreni: python generate_proto.py
"""
import subprocess
import sys

subprocess.run(
    [
        sys.executable,
        "-m",
        "grpc_tools.protoc",
        "-I",
        "protos",
        "--python_out=.",
        "--grpc_python_out=.",
        "protos/sensor.proto",
    ],
    check=True,
)
print("Generisani fajlovi: sensor_pb2.py, sensor_pb2_grpc.py")
