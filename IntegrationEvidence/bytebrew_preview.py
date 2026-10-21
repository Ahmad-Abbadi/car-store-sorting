"""Local-only WebGL verification server; handles Unity's precompressed files."""
import functools
import http.server
import sys


class WebGLHandler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        for suffix in (".gz", ".br"):
            if path.endswith(suffix):
                path = path[:-len(suffix)]
                break
        if path.endswith(".wasm"):
            return "application/wasm"
        return super().guess_type(path)

    def end_headers(self):
        path = self.path.split("?", 1)[0]
        if path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        elif path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        super().end_headers()


if __name__ == "__main__":
    handler = functools.partial(WebGLHandler, directory=sys.argv[1])
    http.server.ThreadingHTTPServer(("127.0.0.1", 8876), handler).serve_forever()
