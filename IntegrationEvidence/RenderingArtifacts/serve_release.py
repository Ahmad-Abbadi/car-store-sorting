"""Local WebGL review server with correct Brotli headers and fresh build responses."""
import functools
import http.server
import sys


class WebGLHandler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith((".br", ".gz")):
            path = path.rsplit(".", 1)[0]
        if path.endswith(".wasm"):
            return "application/wasm"
        return super().guess_type(path)

    def end_headers(self):
        path = self.path.split("?", 1)[0]
        if path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-store")
        super().end_headers()


if __name__ == "__main__":
    handler = functools.partial(WebGLHandler, directory=sys.argv[1])
    http.server.ThreadingHTTPServer(("127.0.0.1", int(sys.argv[2])), handler).serve_forever()
