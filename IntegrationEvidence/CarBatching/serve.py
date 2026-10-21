from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
from urllib.parse import urlsplit
import json, sys

root = Path.cwd() / 'Builds' / ('CarBatching' + sys.argv[1])
evidence = Path.cwd() / 'IntegrationEvidence' / 'CarBatching'
class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs): super().__init__(*args, directory=str(root), **kwargs)
    def guess_type(self, path):
        path = path.removesuffix('.br').removesuffix('.gz')
        if path.endswith('.wasm'): return 'application/wasm'
        if path.endswith('.js'): return 'application/javascript'
        return super().guess_type(path)
    def end_headers(self):
        path = urlsplit(self.path).path
        if path.endswith('.br'): self.send_header('Content-Encoding', 'br')
        if path.endswith('.gz'): self.send_header('Content-Encoding', 'gzip')
        self.send_header('Cross-Origin-Opener-Policy', 'same-origin')
        self.send_header('Cross-Origin-Embedder-Policy', 'require-corp')
        super().end_headers()
    def do_POST(self):
        if self.path != '/car-benchmark-report': self.send_error(404); return
        data = json.loads(self.rfile.read(min(int(self.headers['Content-Length']), 100000)))
        scenario = data.get('scenario') if data.get('scenario') in ('pile', 'shelves', 'motion') else 'pile'
        (evidence / (sys.argv[1] + '-' + scenario + '.json')).write_text(json.dumps(data, indent=2))
        self.send_response(200); self.end_headers(); self.wfile.write(b'ok')
ThreadingHTTPServer(('127.0.0.1', int(sys.argv[2])), Handler).serve_forever()
