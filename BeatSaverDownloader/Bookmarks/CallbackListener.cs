using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaverDownloader.Bookmarks
{
    internal sealed class CallbackListener
    {
        private readonly TokenApi _tokenApi;
        private HttpListener _listener;
        public const string CallbackBase = "http://localhost:45222/";
        private bool _shouldRun;

        internal CallbackListener(TokenApi tokenApi)
        {
            _tokenApi = tokenApi;
        }

        public void Start()
        {
            _shouldRun = true;
            _listener = new HttpListener {
                Prefixes = { CallbackBase }
            };

            try
            {
                _listener.Start();

                _ = Task.Run(async () => {
                    while (_shouldRun) {
                        try {
                            var context = await _listener.GetContextAsync().ConfigureAwait(false);
                            await HandleContext(context).ConfigureAwait(false);
                        } catch (Exception e) {
                            Plugin.LOG.Error("An error occured while handling request");
                            Plugin.LOG.Error(e);
                        }
                    }

                    // ReSharper disable once FunctionNeverReturns
                });

                Plugin.LOG.Debug("Internal webserver started");
            } catch (Exception e) {
                Plugin.LOG.Error(e);
            }
        }

        public void Stop()
        {
            _shouldRun = false;
        }

        private readonly byte[] _responseBuffer = Encoding.UTF8.GetBytes("<p>You can now close this tab</p>\n<script>close();</script>");

        private async Task HandleContext(HttpListenerContext ctx)
        {
            var request = ctx.Request;
            using (var response = ctx.Response)
            {
                try
                {
                    Plugin.LOG.Debug($"New incoming request {request.HttpMethod} {request.Url.AbsoluteUri}");

                    if (request.Url.AbsolutePath.Equals("/cb"))
                    {
                        var queryParams = request.Url.Query
                            .Substring(1)
                            .Split('&')
                            .Select(pair => pair.Split('='))
                            .Where(pair => pair.Length > 1)
                            .ToDictionary(pair => pair[0], pair => pair[1]);

                        if (queryParams.TryGetValue("code", out var codeFromQuery))
                        {
                            queryParams.TryGetValue("state", out var state);
                            await _tokenApi.ExchangeCode(codeFromQuery, state);
                        }
                        else
                        {
                            throw new Exception("No query in request");
                        }

                        response.StatusCode = 200;
                        response.Headers.Set("Content-Type", "text/html");
                        response.ContentLength64 = _responseBuffer.Length;

                        using (var ros = response.OutputStream) {
                            await ros.WriteAsync(_responseBuffer, 0, _responseBuffer.Length);
                        }
                    }
                    else
                    {
                        response.StatusCode = 404;
                    }
                }
                catch (Exception e)
                {
                    Plugin.LOG.Error("Something went wrong while trying to handle an incoming request");
                    Plugin.LOG.Error(e);

                    response.StatusCode = 500;
                }
            }
        }
    }
}