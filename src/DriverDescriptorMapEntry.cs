using System.Buffers.Binary;
using System.Diagnostics;

namespace ApplePartitionMapReader;

/// <summary>
/// Represents an entry in the Driver Descriptor Map.
/// </summary>
public readonly struct DriverDescriptorMapEntry
{
    /// <summary>
    /// The size of a Driver Descriptor Map Entry in bytes.
    /// </summary>
    public const int Size = 8;

    /// <summary>
    /// Gets the starting block of the driver.
    /// </summary>
    public uint StartBlock { get; }

    /// <summary>
    /// Gets the number of blocks occupied by the driver.
    /// </summary>
    public ushort BlockCount { get; }

    /// <summary>
    /// Gets the system type of the driver.
    /// </summary>
    public ushort Type { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DriverDescriptorMapEntry"/> struct.
    /// </summary>
    /// <param name="data">The span containing the entry data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to <see cref="Size"/>.</exception>
    public DriverDescriptorMapEntry(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data must be exactly {Size} bytes long.", nameof(data));
        }

        // Structure documented in https://ciderpress2.com/formatdoc/APM-notes.html
        // and https://github.com/apple-oss-distributions/IOStorageFamily/blob/IOStorageFamily-116/IOApplePartitionScheme.h#L77-L82
        int offset = 0;

        StartBlock = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        BlockCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        Type = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        Debug.Assert(offset == data.Length, "Did not read the expected amount of data for DriverDescriptorMapEntry.");
    }
}
