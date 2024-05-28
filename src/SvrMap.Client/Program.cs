using Spectre.Console;
using System.Net.Sockets;
using SvrMap.Shared;

Application.Run();

public static class Application
{
    private static readonly byte[] Buffer = new byte[Math.Max(Request.Size, Response.Size)];
    private static readonly Dictionary<string, Action> Choices = new()
    {
        { "Send request", SendRequest },
        { "Change configuration", ChangeConfiguration },
        { "Exit", () => Environment.Exit(0) }
    };
    
    private static string _hostname = "127.0.0.1";
    private static int _port = 449;

    // ReSharper disable once FunctionNeverReturns
    public static void Run()
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("What would you like to do?").AddChoices(Choices.Keys));

            Choices[choice].Invoke();
        }
    }

    private static void ChangeConfiguration()
    {
        _hostname = AnsiConsole.Ask("Enter the [blue]hostname[/] of the server:", _hostname);
        _port = AnsiConsole.Ask("Enter the [blue]port[/] number:", _port);
    }

    private static void SendRequest()
    {
        var serviceName = AnsiConsole.Ask<string>("Enter the [blue]service name[/]:");
        try
        {
            using var tcpClient = new TcpClient(_hostname, _port);
            
            new Request(serviceName).WriteTo(Buffer);
            tcpClient.GetStream().Write(Buffer.AsSpan(0, Request.Size));
            
            var bytesRead = tcpClient.GetStream().Read(Buffer);
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
