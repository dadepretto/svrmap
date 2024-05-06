using System.Net;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SvrMap.Server;

public sealed class ServerSettings
{
    public IPAddress IpAddress { get; init; } = IPAddress.Any;
    public required int Port { get; init; } = 449;
    public required IReadOnlyDictionary<string, int> Services { get; init; } = new Dictionary<string, int>();
}