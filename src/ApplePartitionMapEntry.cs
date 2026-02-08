using System.Buffers.Binary;
using System.Diagnostics;
using ApplePartitionMapReader.Utilities;

namespace ApplePartitionMapReader;

/// <summary>
/// Represents an entry in the Apple Partition Map.
/// </summary>
public struct ApplePartitionMapEntry
{
    /// <summary>
    /// The size of an Apple Partition Map Entry in bytes.
    /// </summary>
    public const int Size = 136;
    
    /// <summary>
    /// The expected block signature value ('PM').
    /// </summary>
    public const int BlockSignature = 0x504D; // 'PM'

    /// <summary>
    /// Gets the partition map entry signature.
    /// </summary>
    public ushort Signature { get; }

    /// <summary>
    /// Gets the first reserved field.
    /// </summary>
    public ushort Reserved1 { get; }

    /// <summary>
    /// Gets the total number of entries in the partition map.
    /// </summary>
    public uint MapEntryCount { get; }

    /// <summary>
    /// Gets the partition start sector.
    /// </summary>
    public uint PartitionStartBlock { get; }

    /// <summary>
    /// Gets the partition number of sectors.
    /// </summary>
    public uint PartitionBlockCount { get; }

    /// <summary>
    /// Gets the partition name.
    /// </summary>
    public String32 Name { get; }

    /// <summary>
    /// Gets the partition type.
    /// </summary>
    public String32 Type { get; }

    /// <summary>
    /// Gets the data area start sector.
    /// </summary>
    public uint DataStartBlock { get; }

    /// <summary>
    /// Gets the data area number of sectors.
    /// </summary>
    public uint DataBlockCount { get; }

    /// <summary>
    /// Gets the status flags.
    /// </summary>
    public ApplePartitionMapStatusFlags StatusFlags { get; }

    /// <summary>
    /// Gets the boot code start sector.
    /// </summary>
    public uint BootCodeStartBlock { get; }

    /// <summary>
    /// Gets the boot code number of sectors.
    /// </summary>
    public uint BootCodeBlockCount { get; }

    /// <summary>
    /// Gets the boot code address.
    /// </summary>
    public uint BootCodeAddress { get; }

    /// <summary>
    /// Gets the second reserved field.
    /// </summary>
    public uint Reserved2 { get; }

    /// <summary>
    /// Gets the boot code entry point.
    /// </summary>
    public uint BootCodeEntryPoint { get; }

    /// <summary>
    /// Gets the third reserved field.
    /// </summary>
    public uint Reserved3 { get; }

    /// <summary>
    /// Gets the boot code checksum.
    /// </summary>
    public uint BootCodeChecksum { get; }

    /// <summary>
    /// Gets the processor type.
    /// </summary>
    public String16 ProcessorType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplePartitionMapEntry"/> struct from the given data.
    /// </summary>
    /// <param name="data">The span containing the partition map entry data.</param>
    /// <exception cref="ArgumentException">Thrown when the data length is not equal to the expected size.</exception>
    public ApplePartitionMapEntry(Span<byte> data)
    {
        if (data.Length != Size)
        {
            throw new ArgumentException($"Partition map entry data must be exactly {Size} bytes long.", nameof(data));
        }

        // Structure documented in https://ciderpress2.com/formatdoc/APM-notes.html
        int offset = 0;

        // +$000 / 2: pmSig - partition signature (big-endian 0x504d, 'PM')
        Signature = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        if (Signature != 0x504D) // 'PM'
        {
            throw new InvalidDataException("Invalid Apple Partition Map Entry signature.");
        }

        // +$002 / 2: pmSigPad - (reserved)
        Reserved1 = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // +$004 / 4: pmMapBlkCnt - number of blocks in partition map
        MapEntryCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$008 / 4: pmPyPartStart - block number of first block of partition
        PartitionStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$00c / 4: pmPartBlkCnt - number of blocks in partition
        PartitionBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$010 /32: pmPartName - partition name string (optional; some special values)
        Name = new String32(data.Slice(offset, String32.Size));
        offset += String32.Size;

        // +$030 /32: pmParType - partition type string (names beginning with "Apple_" are reserved)
        Type = new String32(data.Slice(offset, String32.Size));
        offset += String32.Size;

        // +$050 / 4: pmLgDataStart - first logical block of data area (for e.g. A/UX)
        DataStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$054 / 4: pmDataCnt - number of blocks in data area (for e.g. A/UX)
        DataBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$058 / 4: pmPartStatus - partition status information (used by A/UX)
        StatusFlags = (ApplePartitionMapStatusFlags)BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$05c / 4: pmLgBootStart - first logical block of boot code
        BootCodeStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$060 / 4: pmBootSize - size of boot code, in bytes
        BootCodeBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$064 / 4: pmBootAddr - boot code load address
        BootCodeAddress = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$06c / 4: pmBootEntry - boot code entry point
        Reserved2 = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$06c / 4: pmBootEntry - boot code entry point
        BootCodeEntryPoint = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$070 / 4: pmBootEntry2 - (reserved)
        Reserved3 = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$074 / 4: pmBootCksum - boot code checksum
        BootCodeChecksum = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // +$078 /16: pmProcessor - processor type string ("68000", "68020", "68030", "68040")
        ProcessorType = new String16(data.Slice(offset, String16.Size));
        offset += String16.Size;

        // +$088 /376: (reserved)
        Debug.Assert(offset == data.Length, "Did not read the expected number of bytes for ApplePartitionMapEntry.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplePartitionMapEntry"/> struct with the specified values.
    /// </summary>
    /// <param name="mapEntryCount">The total number of entries in the partition map.</param>
    /// <param name="partitionStartBlock">The block number of the first block of the partition.</param>
    /// <param name="partitionBlockCount">The number of blocks in the partition.</param>
    /// <param name="name">The partition name.</param>
    /// <param name="type">The partition type.</param>
    /// <param name="dataStartBlock">The first logical block of the data area.</param>
    /// <param name="dataBlockCount">The number of blocks in the data area.</param>
    /// <param name="statusFlags">The partition status flags.</param>
    /// <param name="bootCodeStartBlock">The first logical block of boot code.</param>
    /// <param name="bootCodeBlockCount">The size of boot code, in bytes.</param>
    /// <param name="bootCodeAddress">The boot code load address.</param>
    /// <param name="bootCodeEntryPoint">The boot code entry point.</param>
    /// <param name="bootCodeChecksum">The boot code checksum.</param>
    /// <param name="processorType">The processor type string.</param>
    public ApplePartitionMapEntry(
        uint mapEntryCount,
        uint partitionStartBlock,
        uint partitionBlockCount,
        String32 name,
        String32 type,
        uint dataStartBlock,
        uint dataBlockCount,
        ApplePartitionMapStatusFlags statusFlags,
        uint bootCodeStartBlock = 0,
        uint bootCodeBlockCount = 0,
        uint bootCodeAddress = 0,
        uint bootCodeEntryPoint = 0,
        uint bootCodeChecksum = 0,
        String16 processorType = default)
    {
        Signature = (ushort)BlockSignature;
        Reserved1 = 0;
        MapEntryCount = mapEntryCount;
        PartitionStartBlock = partitionStartBlock;
        PartitionBlockCount = partitionBlockCount;
        Name = name;
        Type = type;
        DataStartBlock = dataStartBlock;
        DataBlockCount = dataBlockCount;
        StatusFlags = statusFlags;
        BootCodeStartBlock = bootCodeStartBlock;
        BootCodeBlockCount = bootCodeBlockCount;
        BootCodeAddress = bootCodeAddress;
        Reserved2 = 0;
        BootCodeEntryPoint = bootCodeEntryPoint;
        Reserved3 = 0;
        BootCodeChecksum = bootCodeChecksum;
        ProcessorType = processorType;
    }

    /// <summary>
    /// Writes this partition map entry to the specified span in big-endian format.
    /// </summary>
    /// <param name="data">The destination span. Must be at least <see cref="Size"/> bytes.</param>
    public readonly void WriteTo(Span<byte> data)
    {
        if (data.Length < Size)
        {
            throw new ArgumentException($"Destination must be at least {Size} bytes long.", nameof(data));
        }

        int offset = 0;

        // +$000 / 2: pmSig
        BinaryPrimitives.WriteUInt16BigEndian(data[offset..], Signature);
        offset += 2;

        // +$002 / 2: pmSigPad
        BinaryPrimitives.WriteUInt16BigEndian(data[offset..], Reserved1);
        offset += 2;

        // +$004 / 4: pmMapBlkCnt
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], MapEntryCount);
        offset += 4;

        // +$008 / 4: pmPyPartStart
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], PartitionStartBlock);
        offset += 4;

        // +$00c / 4: pmPartBlkCnt
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], PartitionBlockCount);
        offset += 4;

        // +$010 /32: pmPartName
        Name.AsReadOnlySpan().CopyTo(data.Slice(offset, String32.Size));
        offset += String32.Size;

        // +$030 /32: pmParType
        Type.AsReadOnlySpan().CopyTo(data.Slice(offset, String32.Size));
        offset += String32.Size;

        // +$050 / 4: pmLgDataStart
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], DataStartBlock);
        offset += 4;

        // +$054 / 4: pmDataCnt
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], DataBlockCount);
        offset += 4;

        // +$058 / 4: pmPartStatus
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], (uint)StatusFlags);
        offset += 4;

        // +$05c / 4: pmLgBootStart
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], BootCodeStartBlock);
        offset += 4;

        // +$060 / 4: pmBootSize
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], BootCodeBlockCount);
        offset += 4;

        // +$064 / 4: pmBootAddr
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], BootCodeAddress);
        offset += 4;

        // +$068 / 4: (reserved)
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], Reserved2);
        offset += 4;

        // +$06c / 4: pmBootEntry
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], BootCodeEntryPoint);
        offset += 4;

        // +$070 / 4: (reserved)
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], Reserved3);
        offset += 4;

        // +$074 / 4: pmBootCksum
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], BootCodeChecksum);
        offset += 4;

        // +$078 /16: pmProcessor
        ProcessorType.AsReadOnlySpan().CopyTo(data.Slice(offset, String16.Size));
        offset += String16.Size;

        Debug.Assert(offset == Size, "Did not write the expected number of bytes for ApplePartitionMapEntry.");
    }
}
