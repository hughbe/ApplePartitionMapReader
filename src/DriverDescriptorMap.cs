using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ApplePartitionMapReader;

namespace ApplePartitionMapReader.Utilities;

/// <summary>
/// Represents a Driver Descriptor Map structure. 
/// </summary>
public readonly struct DriverDescriptorMap
{
    /// <summary>
    /// The size of the Driver Descriptor Map in bytes.
    /// </summary>
    public const int Size = 18 + DriverDescriptorMapEntries.Size;

    /// <summary>
    /// Gets the Driver Descriptor Map signature.
    /// </summary>
    public ushort Signature { get; }

    /// <summary>
    /// Gets the block size of the device.
    /// </summary>
    public ushort BlockSize { get; }

    /// <summary>
    /// Gets the number of blocks on the device.
    /// </summary>
    public uint BlockCount { get; }

    /// <summary>
    /// Gets the device type.
    /// </summary>
    public ushort DeviceType { get; }

    /// <summary>
    /// Gets the device ID.
    /// </summary>
    public ushort DeviceId { get; }

    /// <summary>
    /// Gets the driver data.
    /// </summary>
    public uint DriverData { get; }

    /// <summary>
    /// Gets the number of driver descriptors.
    /// </summary>
    public ushort DriverCount { get; }

    /// <summary>
    /// Gets the array of driver descriptor map entries.
    /// </summary>
    public DriverDescriptorMapEntries Entries { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DriverDescriptorMap"/> struct.
    /// </summary>
    /// <param name="data">The span containing the Driver Descriptor Map data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to <see cref="Size"/>.</exception>
    /// <exception cref="InvalidDataException">Thrown when the signature is invalid.</exception>
    public DriverDescriptorMap(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Data span must be exactly {Size} bytes long.", nameof(data));
        }

        // Structure documented in https://ciderpress2.com/formatdoc/APM-notes.html
        // and https://github.com/apple-oss-distributions/IOStorageFamily/blob/IOStorageFamily-116/IOApplePartitionScheme.h#L86-L97
        int offset = 0;

        // +$00 / 2: sbSig - device signature (big-endian 0x4552, 'ER')
        Signature = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        if (Signature != 0x4552) // 'ER'
        {
            throw new InvalidDataException("Invalid Driver Descriptor Map signature.");
        }

        // +$02 / 2: sbBlkSize - block size of the device (usually 512)
        BlockSize = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // +$04 / 4: sbBlkCount - number of blocks on the device
        BlockCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // +$08 / 4: sbDevType - (reserved)
        DeviceType = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // +$0a / 2: sbDevId - (reserved)
        DeviceId = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // +$0c / 4: sbData - (reserved)
        DriverData = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        offset += 4;

        // +$10 / 2: sbDrvrCount - number of driver descriptor entries
        DriverCount = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, 2));
        offset += 2;

        // +$12 / 4: ddBlock - first driver's starting block
        // +$16 / 2: ddSize - size of the driver, in 512-byte blocks
        // +$18 / 2: ddType - operating system type (MacOS = 1)
        // +$1a /486: ddPad - ddBlock/ddSize/ddType entries for additional drivers (8 bytes each)
        Entries = new DriverDescriptorMapEntries(data.Slice(offset, DriverDescriptorMapEntries.Size));
        offset += DriverDescriptorMapEntries.Size;

        Debug.Assert(offset == data.Length, "Did not consume all bytes for Driver Descriptor Map.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DriverDescriptorMap"/> struct with the specified values.
    /// </summary>
    /// <param name="blockSize">The block size of the device (usually 512).</param>
    /// <param name="blockCount">The total number of blocks on the device.</param>
    /// <param name="deviceType">The device type.</param>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="driverData">The driver data.</param>
    /// <param name="driverCount">The number of driver descriptors.</param>
    /// <param name="entries">The driver descriptor map entries.</param>
    public DriverDescriptorMap(ushort blockSize, uint blockCount, ushort deviceType, ushort deviceId, uint driverData, ushort driverCount, DriverDescriptorMapEntries entries)
    {
        Signature = 0x4552; // 'ER'
        BlockSize = blockSize;
        BlockCount = blockCount;
        DeviceType = deviceType;
        DeviceId = deviceId;
        DriverData = driverData;
        DriverCount = driverCount;
        Entries = entries;
    }

    /// <summary>
    /// Writes this Driver Descriptor Map to the specified span in big-endian format.
    /// </summary>
    /// <param name="data">The destination span. Must be at least <see cref="Size"/> bytes.</param>
    public readonly void WriteTo(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Destination must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        BinaryPrimitives.WriteUInt16BigEndian(data.Slice(offset, 2), Signature);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(data.Slice(offset, 2), BlockSize);
        offset += 2;

        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(offset, 4), BlockCount);
        offset += 4;

        BinaryPrimitives.WriteUInt16BigEndian(data.Slice(offset, 2), DeviceType);
        offset += 2;

        BinaryPrimitives.WriteUInt16BigEndian(data.Slice(offset, 2), DeviceId);
        offset += 2;

        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(offset, 4), DriverData);
        offset += 4;

        BinaryPrimitives.WriteUInt16BigEndian(data.Slice(offset, 2), DriverCount);
        offset += 2;

        Entries.WriteTo(data.Slice(offset, DriverDescriptorMapEntries.Size));
        offset += DriverDescriptorMapEntries.Size;

        Debug.Assert(offset == Size, "Did not write the expected number of bytes for Driver Descriptor Map.");
    }
}
