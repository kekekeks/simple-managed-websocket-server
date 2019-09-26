using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SimpleHttpServer;

namespace Sandbox
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var srv = new SimpleWebSocketHttpServer(IPAddress.Loopback, 7001);
            srv.Listen();
            var mimeTypes = new Dictionary<string, string>
            {
                ["html"] = "text/html; charset=utf8"
            };
            while (true)
            {
                using (var reqb = await srv.AcceptAsync())
                {
                    if (reqb is SimpleWebSocketHttpRequest req)
                    {
                        using (req)
                        {
                            if (req.IsWebsocketRequest)
                            {
                                HandleWebSocket(await req.AcceptWebSocket());
                                continue;
                            }

                            var path = req.Path.TrimStart('/');
                            if (path == "")
                                path = "index.html";
                            path = path.Replace('/', '.');
                            var res = typeof(Program).Assembly.GetManifestResourceStream(path);
                            if (res == null)
                            {
                                await req.RespondAsync(404, Encoding.UTF8.GetBytes("Not found"), "text/plain");
                                continue;
                            }

                            var ext = Path.GetExtension(path).TrimStart('.');
                            if (!mimeTypes.TryGetValue(ext ?? "", out var mime))
                                mime = "application/octet-stream";
                            var ms = new MemoryStream();
                            res.CopyTo(ms);
                            await req.RespondAsync(200, ms.ToArray(), mime);
                        }
                    }
                }
            }
            
            srv.Dispose();
        }

        private static async void HandleWebSocket(SimpleWebSocket socket)
        {
            using (socket)
            {
                try
                {
                    //await socket.SendMessage("Hello world!");
                    while (true)
                    {
                        var msg = await socket.ReceiveMessage();
                        if (msg == null)
                            return;
                        if (msg.IsText)
                        {
                            var s = msg.AsString();
                            if (s == "get-binary")
                                await socket.SendMessage(false, new byte[] {1, 2, 3, 4, 5});
                            else
                                await socket.SendMessage("Received: " + s);
                        }
                        else
                            await socket.SendMessage("Received " + BitConverter.ToString(msg.Data));

                    }
                }
                catch (EndOfStreamException)
                {

                }
                catch (IOException)
                {
                    
                }
            }
        }
    }
}