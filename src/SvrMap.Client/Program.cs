using Spectre.Console;
using System.Net.Sockets;
using SvrMap.Shared;

Application.Run();

public static class Application
{
    private static readonly byte[] Buffer = new byte[Math.Max(Request.Size, Response.Size)];
    private static readonly Dictionary<string, Action> Choices = new()
    {
        { "Configure client", ConfigureClient },
        { "Send request", SendRequest },
        { "Exit", () => Environment.Exit(0) }
    };
    
    private static string _hostname = string.Empty;
    private static int _port;
    private static TcpClient? _client;

    // ReSharper disable once FunctionNeverReturns
    public static void Run()
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("What would you like to do?")
                    .AddChoices(Choices.Keys));

            Choices[choice].Invoke();
        }
    }

    private static void ConfigureClient()
    {
        _hostname = AnsiConsole.Ask("Enter the [blue]hostname[/] of the server:", "127.0.0.1");
        _port = AnsiConsole.Ask("Enter the [blue]port[/] number:", 449);

        try
        {
            _client = new TcpClient(_hostname, _port);
            AnsiConsole.MarkupLine("[green]Client successfully configured![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Failed to configure client: {ex.Message}[/]");
            _client = null;
        }
    }

    private static void SendRequest()
    {
        if (_client is null)
        {
            AnsiConsole.MarkupLine("[red]Client not configured! Please configure the client first.[/]");
            return;
        }
        
        var serviceName = AnsiConsole.Ask<string>("Enter the [blue]service name[/]:");
        try
        {
            var stream = _client.GetStream();
            
            new Request(serviceName).WriteTo(Buffer);
            stream.Write(Buffer.AsSpan(0, Request.Size));
            
            var bytesRead = stream.Read(Buffer);
            var response = Response.ReadFrom(Buffer.AsSpan(0, bytesRead));

            AnsiConsole.MarkupLineInterpolated(response.Found
                ? (FormattableString)$"[green]Service {serviceName} found on {response.PortNumber}[/]"
                : (FormattableString)$"[yellow]Service {serviceName} not found![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Communication failure: {ex.Message}[/]");
        }
    }
}
