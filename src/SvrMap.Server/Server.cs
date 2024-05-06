using System.Buffers;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SvrMap.Shared;

namespace SvrMap.Server;

public sealed partial class Server(ILogger<Server> logger, IOptions<ServerSettings> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var activeTasks = new HashSet<Task>();
        var tcpListener = new TcpListener(options.Value.IpAddress, options.Value.Port);
        
        try
        {
            tcpListener.Start();
            
            while (!cancellationToken.IsCancellationRequested)
            {
                var tcpClient = await tcpListener.AcceptTcpClientAsync(cancellationToken);
                activeTasks.Add(HandleRequest(tcpClient, cancellationToken)
                    .ContinueWith(task => activeTasks.Remove(task), cancellationToken));
            }
        }
        finally
        {
            await Task.WhenAll(activeTasks);
            tcpListener.Stop();
        }
    }

    private async Task HandleRequest(TcpClient client, CancellationToken cancellationToken)
    {
        using var _ = logger.BeginScope(new { RequestId = Guid.NewGuid() });
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(Request.Size, Response.Size));

        try
        {
            var bytesRead = await client.GetStream().ReadAsync(buffer, cancellationToken);
            var request = Request.ReadFrom(buffer.AsSpan(0, bytesRead));

            var found = options.Value.Services.TryGetValue(request.ServiceName, out var portNumber);
            if (found) LogNotFound(logger, request.ServiceName);
            else LogFound(logger, request.ServiceName, portNumber);

            var response = new Response(found, portNumber);
            response.WriteTo(buffer);
            await client.GetStream().WriteAsync(buffer.AsMemory(0, Response.Size), cancellationToken);
        }
        catch (Exception exception)
        {
            LogError(logger, exception);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            client.Dispose();
        }
    }

    [LoggerMessage(LogLevel.Information, "Listening on port {PortNumber}")]
    private static partial void LogStarted(ILogger logger, int portNumber);

    [LoggerMessage(LogLevel.Debug, "Requested service '{ServiceName}' found with port {PortNumber}")]
    private static partial void LogFound(ILogger logger, string serviceName, int portNumber);

    [LoggerMessage(LogLevel.Warning, "Requested service '{ServiceName}' not found")]
    private static partial void LogNotFound(ILogger logger, string serviceName);

    [LoggerMessage(LogLevel.Error, "An error occurred while handling request")]
    private static partial void LogError(ILogger logger, Exception exception);
}