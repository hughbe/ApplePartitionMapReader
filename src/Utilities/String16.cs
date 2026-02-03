using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace ApplePartitionMapReader.Utilities;

/// <summary>
/// Represents a fixed-size string of up to 16 bytes.
/// </summary>
[InlineArray(Size)]
public struct String16
{
    /// <summary>
    /// Gets the string value.
    /// </summary>
    public const int Size = 16;

    /// <summary>
    /// The first element of the array.
    /// </summary>
    private byte _element0;

    /// <summary>
    /// Gets the string value.
    /// </summary>
    /// <param name="data">The span containing the string bytes.</param>
    /// <exception cref="ArgumentException">Thrown when the data span length is not equal to <see cref="Size"/>.</exception>
    public String16(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data span must be exactly {Size} bytes long.", nameof(data));
        }

        

        data.CopyTo(AsSpan());
    }

    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>   
    public Span<byte> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _element0, Size);

    /// <inheritdoc/>
    public override string ToString()
    {
        var span = AsSpan();
        int length = span.IndexOf((byte)0);
        if (length < 0)
        {
            length = span.Length;
        }

        return Encoding.ASCII.GetString(span[..length]);
    }

    /// <summary>
    /// Implicitly converts the <see cref="String16"/> to a <see cref="string"/>.
    /// </summary>
    /// <param name="str">The <see cref="String16"/> instance.</param>
    /// <returns>The converted string.</returns>
    public static implicit operator string(String16 str) => str.ToString();
}
