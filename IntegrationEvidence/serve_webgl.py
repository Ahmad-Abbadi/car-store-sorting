from http.server import ThreadingHTTPServer, SimpleHTTPRequestHandler
from pathlib import Path
from urllib.parse import urlsplit
import mimetypes
import sys

root = Path(__file__).resolve().parents[1] / 'Builds' / (sys.argv[1] if len(sys.argv)>1 else 'ThemedSortingWebGL')
class Handler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(root), **kwargs)
    def guess_type(self, path):
        if path.endswith('.gz'): path = path[:-3]
        elif path.endswith('.br'): path = path[:-3]
        if path.endswith('.wasm'): return 'application/wasm'
        if path.endswith('.js'): return 'application/javascript'
        return super().guess_type(path)
    def end_headers(self):
        path = urlsplit(self.path).path
        if path.endswith('.gz'): self.send_header('Content-Encoding', 'gzip')
        elif path.endswith('.br'): self.send_header('Content-Encoding', 'br')
        self.send_header('Cross-Origin-Opener-Policy', 'same-origin')
        self.send_header('Cross-Origin-Embedder-Policy', 'require-corp')
        super().end_headers()

if __name__ == '__main__':
    ThreadingHTTPServer(('127.0.0.1', int(sys.argv[2]) if len(sys.argv)>2 else 8188), Handler).serve_forever()
