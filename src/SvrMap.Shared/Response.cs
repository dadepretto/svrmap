using System.Buffers.Binary;
using System.Text;

// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace SvrMap.Shared;

public readonly struct Response(bool found, int portNumber)
{
    private static readonly Encoding CodePage;

    public const int Size = 5;

    public bool Found { get; } = found;
    public int PortNumber { get; } = portNumber;

    static Response()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        CodePage = Encoding.GetEncoding(850);
    }

    public Response(int? portNumber) : this(portNumber.HasValue, portNumber ?? default(int))
    {
    }

    public void WriteTo(Span<byte> buffer)
    {
        EnsureBufferSize(buffer);

        CodePage.GetBytes(Found ? "+" : "-", buffer[..1]);
        BinaryPrimitives.WriteInt32BigEndian(buffer[1..], PortNumber);
    }

    public static Response ReadFrom(Span<byte> buffer)
    {
        EnsureBufferSize(buffer);

        var found = (char)buffer[0] switch
        {
            '+' => true,
            '-' => false,
            var value => throw new Exception($"Invalid response character {value} at position 0")
        };
        var portNumber = BinaryPrimitives.ReadInt32BigEndian(buffer[1..]);

        return new Response(found, portNumber);
    }

    private static void EnsureBufferSize(Span<byte> buffer)
    {
        if (buffer.Length < Size)
            throw new InvalidOperationException($"Request buffer must be at least {Size} bytes");
    }
}