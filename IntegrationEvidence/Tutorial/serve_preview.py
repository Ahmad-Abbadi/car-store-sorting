"""Local preview for the tutorial WebGL build, with Unity compression headers."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path


class UnityHandler(SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith((".gz", ".br")):
            path = path.rsplit(".", 1)[0]
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


root = Path(__file__).resolve().parents[2] / "Builds" / "FirstShelfTutorialWebGL"
if __name__ == "__main__":
    ThreadingHTTPServer(("127.0.0.1", 8198), partial(UnityHandler, directory=str(root))).serve_forever()
