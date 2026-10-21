"""Local connection check using the exact JavaScript SDK shipped in the Unity player."""
import http.server
import json
from pathlib import Path
import re
from bytebrew_preview import WebGLHandler

ROOT = Path(__file__).resolve().parent.parent


class ConnectionHandler(WebGLHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=str(ROOT / "Builds/ByteBrewConnected"), **kwargs)

    def do_GET(self):
        route = self.path.split("?", 1)[0]
        if route == "/bytebrew-sdk.js":
            content = (ROOT / "Assets/ByteBrewSDK/Plugins/WebGL/ByteBrewSDK.jspre").read_bytes()
            mime = "application/javascript"
        elif route == "/bytebrew-check.html":
            settings = (ROOT / "Assets/ByteBrewSDK/Resources/ByteBrewSettings.asset").read_text(encoding="utf-8-sig")
            def value(name):
                return re.search(r"^  " + name + r":[ \t]*([^\r\n]*)", settings, re.MULTILINE).group(1).strip()
            config = json.dumps([value("webGameID"), value("webSDKKey"), "connection_check"])
            content = ("""<!doctype html><html><meta charset="utf-8"><title>Toy Car Sort — ByteBrew check</title>
<style>body{font:20px system-ui;max-width:720px;margin:70px auto;padding:20px;background:#f6f8fc;color:#17243d}h1{font-size:30px}#status{padding:24px;background:white;border-radius:12px}</style>
<h1>Toy Car Sort · ByteBrew connection</h1><p>Checks the Web SDK bundled with the game using its configured app keys.</p>
<div id="status">Initializing ByteBrew…</div><script src="/bytebrew-sdk.js"></script><script>
const config = CONFIG;
const status = document.getElementById('status');
try {
  ByteBrewSDK.ByteBrew.initializeByteBrew(...config);
  const started = Date.now();
  const timer = setInterval(() => {
    try {
      if (ByteBrewSDK.ByteBrew.isByteBrewInitialized()) {
        clearInterval(timer);
        ByteBrewSDK.ByteBrew.newCustomEvent('bytebrew_connection_check', 'source=local_verification;');
        status.textContent = 'ByteBrew initialized successfully. Connection-check event submitted.';
        status.style.border = '2px solid #24935a';
        console.log('BYTEBREW_CONNECTION_READY');
      } else if (Date.now() - started > 30000) {
        clearInterval(timer); status.textContent = 'ByteBrew initialization timed out.';
      }
    } catch (error) { clearInterval(timer); status.textContent = 'Connection check failed: ' + error.name; }
  }, 250);
} catch (error) { status.textContent = 'Connection check failed: ' + error.name; }
</script></html>""").replace("CONFIG", config).encode("utf-8")
            mime = "text/html; charset=utf-8"
        else:
            return super().do_GET()
        self.send_response(200)
        self.send_header("Content-Type", mime)
        self.send_header("Content-Length", str(len(content)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(content)


if __name__ == "__main__":
    http.server.ThreadingHTTPServer(("127.0.0.1", 8876), ConnectionHandler).serve_forever()
