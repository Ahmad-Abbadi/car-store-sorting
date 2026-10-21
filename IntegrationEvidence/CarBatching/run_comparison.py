"""Alternate the two release builds in installed browsers on an otherwise idle Editor."""
from pathlib import Path
import subprocess
import sys

folder = Path(__file__).resolve().parent
runs = [
    ('Chrome', 'Baseline', 'pile', '1'), ('Chrome', 'Optimized', 'pile', '1'),
    ('Edge', 'Optimized', 'pile', '1'), ('Edge', 'Baseline', 'pile', '1'),
    ('Chrome', 'Optimized', 'pile', '2'), ('Chrome', 'Baseline', 'pile', '2'),
    ('Edge', 'Baseline', 'pile', '2'), ('Edge', 'Optimized', 'pile', '2'),
    ('Chrome', 'Baseline', 'shelves', '1'), ('Chrome', 'Optimized', 'shelves', '1'),
    ('Edge', 'Optimized', 'shelves', '1'), ('Edge', 'Baseline', 'shelves', '1'),
]
for run in runs:
    print('Starting ' + '/'.join(run), flush=True)
    subprocess.run([sys.executable, str(folder / 'run_browser_benchmark.py'), *run], check=True)
subprocess.run([sys.executable, str(folder / 'summarize_benchmarks.py')], check=True)
