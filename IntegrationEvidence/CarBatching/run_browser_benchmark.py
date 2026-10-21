"""Run the local, isolated release benchmark in installed desktop browsers.

Usage: python run_browser_benchmark.py Chrome Baseline pile 1
Keep the Unity Editor idle and run one browser at a time for final measurements.
"""
from pathlib import Path
import json
import hashlib
import ctypes
import subprocess
import sys
import time

browser, build, scenario, repetition = sys.argv[1:5]
quality = sys.argv[5] if len(sys.argv) > 5 else 'medium'

def machine_memory():
    class MemoryStatus(ctypes.Structure):
        _fields_ = [('length', ctypes.c_ulong), ('loadPercent', ctypes.c_ulong),
                    ('totalPhysical', ctypes.c_ulonglong), ('availablePhysical', ctypes.c_ulonglong),
                    ('totalPageFile', ctypes.c_ulonglong), ('availablePageFile', ctypes.c_ulonglong),
                    ('totalVirtual', ctypes.c_ulonglong), ('availableVirtual', ctypes.c_ulonglong),
                    ('availableExtendedVirtual', ctypes.c_ulonglong)]
    status = MemoryStatus()
    status.length = ctypes.sizeof(status)
    if not ctypes.windll.kernel32.GlobalMemoryStatusEx(ctypes.byref(status)):
        return {'available': False}
    return {'physicalBytes': status.totalPhysical, 'availablePhysicalBytes': status.availablePhysical,
            'loadPercent': status.loadPercent, 'availableCommitBytes': status.availablePageFile}
executables = {
    'Chrome': Path('C:/Program Files/Google/Chrome/Application/chrome.exe'),
    'Edge': Path('C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe'),
}
if browser not in executables or build not in ('Baseline', 'Optimized', 'Instanced', 'Occlusion', 'Current', 'Spatial', 'Direct', 'Grouped', 'ShelfTint', 'Final') or scenario not in ('pile', 'shelves', 'motion') or quality not in ('medium', 'low'):
    raise SystemExit('Choose Chrome/Edge, Baseline/Optimized/Instanced/Occlusion/Current/Spatial and pile/shelves')
root = Path.cwd()
evidence = root / 'IntegrationEvidence/CarBatching'
profile = root / 'Temp/CarBatchingBrowsers' / f'{browser}-{build}-{scenario}-{repetition}'
profile.mkdir(parents=True, exist_ok=True)
port = {'Baseline': 8197, 'Optimized': 8198, 'Instanced': 8199, 'Occlusion': 8200, 'Current': 8201, 'Spatial': 8202, 'Direct': 8203, 'Grouped': 8204, 'ShelfTint': 8205, 'Final': 8206}[build]
report = evidence / f'{build}-{scenario}.json'
arguments = [str(executables[browser]), '--headless=new', '--no-first-run', '--no-default-browser-check',
             '--window-size=1280,821', '--disable-background-timer-throttling',
             '--disable-renderer-backgrounding', '--disable-backgrounding-occluded-windows',
             '--user-data-dir=' + str(profile),
             f'http://localhost:{port}/?carBenchmark=1&scenario={scenario}&quality={quality}']
payload = root / 'Builds' / ('CarBatching' + build) / 'Build'
payload_hashes = {path.name: hashlib.sha256(path.read_bytes()).hexdigest()
                  for path in sorted(payload.glob('*')) if path.is_file()}
started = time.time()
memory_before = machine_memory()
with (evidence / f'{browser}-{build}-{scenario}-{repetition}.log').open('w') as log:
    process = subprocess.Popen(arguments, stdout=log, stderr=log)
    try:
        while time.time() - started < 300:
            if report.exists() and report.stat().st_mtime >= started:
                try:
                    data = json.loads(report.read_text())
                except (OSError, json.JSONDecodeError):
                    time.sleep(.5)
                    continue
                if not data.get('webgl', {}).get('capturePending', True):
                    data['runner'] = {'browser': browser, 'build': build, 'repetition': repetition,
                                      'mode': 'headless=new', 'arguments': arguments[1:-1],
                                      'payloadSha256': payload_hashes,
                                      'machineMemoryBefore': memory_before, 'machineMemoryAfter': machine_memory()}
                    destination = evidence / f'{browser}-{build}-{scenario}-{repetition}.json'
                    destination.write_text(json.dumps(data, indent=2))
                    print(json.dumps({'report': str(destination), 'averageFrameMs': data['averageFrameMs'],
                                      'p95FrameMs': data['p95FrameMs'], 'drawCalls': data['webgl']['drawCalls'],
                                      'triangles': data['webgl']['triangles'], 'renderer': data['webgl'].get('renderer')}))
                    break
            if process.poll() is not None:
                raise RuntimeError(f'{browser} exited before reporting: {process.returncode}')
            time.sleep(.5)
        else:
            raise TimeoutError(f'{browser} did not complete {build}/{scenario} within five minutes')
    finally:
        # Only this explicitly launched browser and its children are terminated.
        subprocess.run(['taskkill', '/PID', str(process.pid), '/T', '/F'], stdout=subprocess.DEVNULL,
                       stderr=subprocess.DEVNULL, check=False)
