from http.server import BaseHTTPRequestHandler, HTTPServer
import json

# Define a port to listen on
PORT = 8080

class WebhookHandler(BaseHTTPRequestHandler):
    def _send_ok_response(self):
        self.send_response(200)
        self.send_header('Content-type', 'application/json')
        self.end_headers()
        self.wfile.write(json.dumps({"status": "received"}).encode('utf-8'))

    def do_POST(self):
        content_length = int(self.headers.get('Content-Length', 0))
        post_data = self.rfile.read(content_length)
        
        print("\n--- NEW WEBHOOK RECEIVED ---")
        
        try:
            # Try to parse and pretty-print JSON
            jsonData = json.loads(post_data)
            print("Headers:")
            print(self.headers)
            print("Body (parsed JSON):")
            print(json.dumps(jsonData, indent=2))
        except json.JSONDecodeError:
            # Fallback for non-JSON or malformed data
            print("Headers:")
            print(self.headers)
            print("Body (raw):")
            print(post_data.decode('utf-8'))
        
        print("------------------------------\n")
        
        # Send a 200 OK response
        self._send_ok_response()
        
    def do_GET(self):
        # You can add a simple GET handler if you want
        print("\n--- GET request received ---")
        self._send_ok_response()

def run(server_class=HTTPServer, handler_class=WebhookHandler, port=PORT):
    # Listen on all available interfaces (0.0.0.0)
    server_address = ('0.0.0.0', port)
    httpd = server_class(server_address, handler_class)
    
    print(f"Starting simple HTTP server on port {port}...")
    print(f"Send your webhook to http://<your-server-ip>:{port}")
    httpd.serve_forever()

if __name__ == "__main__":
    run()
