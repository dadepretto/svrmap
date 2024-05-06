using System.Text;

// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace SvrMap.Shared;

public readonly struct Request(string serviceName)
{
    private static readonly Encoding CodePage;

    public const int Size = 32;

    public string ServiceName { get; } = serviceName;

    static Request()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        CodePage = Encoding.GetEncoding(850);
    }

    public void WriteTo(Span<byte> buffer)
    {
        EnsureBufferSize(buffer);
        var bytesWritten = CodePage.GetBytes(ServiceName, buffer);
        if (bytesWritten < Size) buffer[bytesWritten..].Clear(); // Pad the remaining buffer if needed
    }

    public static Request ReadFrom(Span<byte> buffer)
    {
        EnsureBufferSize(buffer);
        var serviceName = CodePage.GetString(buffer).TrimEnd('\0'); // Trim potential padding
        return new Request(serviceName);
    }

    private static void EnsureBufferSize(Span<byte> buffer)
    {
        if (buffer.Length > Size)
            throw new InvalidOperationException($"Request buffer must be at most {Size} bytes.");
    }

    public override string ToString()
    {
        return ServiceName;
    }
}