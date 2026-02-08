using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ApplePartitionMapReader;

/// <summary>
/// Represents an array of Driver Descriptor Map Entries.
/// </summary>
[InlineArray(Count)]
public struct DriverDescriptorMapEntries
{
    /// <summary>
    /// The size of the Driver Descriptor Map Entries in bytes.
    /// </summary>
    public const int Size = DriverDescriptorMapEntry.Size * Count;

    /// <summary>
    /// The number of Driver Descriptor Map Entries.
    /// </summary>
    public const int Count = 8;

    /// <summary>
    /// The first entry of the array.
    /// </summary>
    private DriverDescriptorMapEntry _entry0;

    /// <summary>
    /// Initializes a new instance of the <see cref="DriverDescriptorMapEntries"/> struct.
    /// </summary>
    /// <param name="data">The span containing the entries data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to <see cref="Size"/>.</exception>
    public DriverDescriptorMapEntries(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data span must be exactly {Size} bytes long.", nameof(data));
        }

        Span<DriverDescriptorMapEntry> entries = AsSpan();

        for (int i = 0; i < Count; i++)
        {
            var entryData = data.Slice(i * DriverDescriptorMapEntry.Size, DriverDescriptorMapEntry.Size);
            entries[i] = new DriverDescriptorMapEntry(entryData);
        }
    }
    
    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>
    public Span<DriverDescriptorMapEntry> AsSpan() =>
        MemoryMarshal.CreateSpan(ref _entry0, Count);
    
    /// <summary>
    /// Gets a span over the elements of the array.
    /// </summary>
    public readonly ReadOnlySpan<DriverDescriptorMapEntry> AsReadOnlySpan() =>
        MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _entry0), Count);

    /// <summary>
    /// Writes all entries to the specified span in big-endian format.
    /// </summary>
    /// <param name="data">The destination span. Must be at least <see cref="Size"/> bytes.</param>
    public readonly void WriteTo(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Destination must be at least {Size} bytes long.", nameof(data));
        }

        ReadOnlySpan<DriverDescriptorMapEntry> entries = AsReadOnlySpan();
        for (int i = 0; i < Count; i++)
        {
            entries[i].WriteTo(data.Slice(i * DriverDescriptorMapEntry.Size, DriverDescriptorMapEntry.Size));
        }
    }
}
