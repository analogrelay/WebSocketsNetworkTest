using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace WebSocketsNetworkTest
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets();

            app.Use(async (context, next) =>
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    await RunSocketAsync(await context.WebSockets.AcceptWebSocketAsync(), context.RequestAborted);
                }
                else
                {
                    await next();
                }
            });

            app.UseFileServer();
        }

        private async Task RunSocketAsync(WebSocket webSocket, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var sendTimer = new Timer(TimerCallback, webSocket, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            try
            {
                Console.WriteLine("*** ACCEPTED ***");

                // Don't do any sending, just let the ping/pong stuff work. Receive in order to detect disconnects
                var buffer = new byte[1024];
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                    if (result.CloseStatus != null)
                    {
                        Console.WriteLine($"*** CLOSED with status {result.CloseStatus} ({(int)result.CloseStatus}): {result.CloseStatusDescription} ***");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("*** RECEIVED ***");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("*** EXCEPTION ***");
                Console.WriteLine(ex);
                Console.WriteLine("*** END EXCEPTION ***");
            }
            finally
            {
                Console.WriteLine($"*** ENDED AFTER {sw.ElapsedMilliseconds:0.00}ms");
            }
        }

        private static async void TimerCallback(object state)
        {
            Console.WriteLine("*** PING ***");
            await ((WebSocket)state).SendAsync(Encoding.UTF8.GetBytes("ping"), WebSocketMessageType.Text, endOfMessage: true, cancellationToken: CancellationToken.None);
        }
    }
}
