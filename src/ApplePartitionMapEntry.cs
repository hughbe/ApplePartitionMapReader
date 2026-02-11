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
    public ushort Padding { get; }

    /// <summary>
    /// Gets the total number of entries in the partition map.
    /// </summary>
    public uint MapBlockCount { get; }

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
    public ApplePartitionMapStatusFlags Status { get; }

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
    /// Gets the second boot code address (reserved).
    /// </summary>
    public uint BootCodeAddress2 { get; }

    /// <summary>
    /// Gets the boot code entry point.
    /// </summary>
    public uint BootCodeEntryPoint { get; }

    /// <summary>
    /// Gets the second boot code entry point.
    /// </summary>
    public uint BootCodeEntryPoint2 { get; }

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

        // Structure documented in https://dev.os9.ca/techpubs/mac/pdf/Devices/SCSI_Manager.pdf
        // 3-2 to 53-27 and https://ciderpress2.com/formatdoc/APM-notes.html
        int offset = 0;

        // pmSig The partition signature. This field should contain the value of the
        // pMapSIG constant ($504D). An earlier but still supported version
        // uses the value $5453.
        Signature = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;
        if (Signature != 0x504D) // 'PM'
        {
            throw new InvalidDataException("Invalid Apple Partition Map Entry signature.");
        }

        // pmSigPad Reserved
        Padding = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
        offset += 2;

        // pmMapBlkCnt The size of the partition map, in blocks.
        MapBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmPyPartStart The physical block number of the first block of the partition.
        PartitionStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmPartBlkCnt The size of the partition, in blocks.
        PartitionBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmPartName An optional partition name, up to 32 bytes in length. If the string
        // is less than 32 bytes, it must be terminated with the ASCII NUL
        // character (a byte with a value of 0). If the partition name begins
        // with Maci (for Macintosh), the Start Manager will perform
        // checksum verification of the device driver’s boot code. Otherwise,
        // this field is ignored.
        Name = new String32(data.Slice(offset, String32.Size));
        offset += String32.Size;

        // pmParType A string that identifies the partition type. Names that begin with
        // Apple_ are reserved for use by Apple Computer, Inc. Names
        // shorter than 32 characters must be terminated with the NUL
        // character. The following standard partition types are defined for
        // the pmParType field:
        // String Meaning
        // Apple_partition_map Partition contains a partition map
        // Apple_Driver Partition contains a device driver
        // Apple_Driver43 Partition contains a SCSI Manager 4.3
        // device driver
        // Apple_MFS Partition uses the original Macintosh
        // File System (64K ROM version)
        // Apple_HFS Partition uses the Hierarchical File
        // System implemented in 128K and
        // later ROM versions
        // Apple_Unix_SVR2 Partition uses the Unix file system
        // Apple_PRODOS Partition uses the ProDOS file system
        // Apple_Free Partition is unused
        // Apple_Scratch Partition is empty
        Type = new String32(data.Slice(offset, String32.Size));
        offset += String32.Size;

        // pmLgDataStart The logical block number of the first block containing file system
        // data. This is for use by operating systems, such as A/UX, in which
        // the file system does not begin at logical block 0 of the partition. 
        DataStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmDataCnt The size of the file system data area, in blocks. This is used in
        // conjunction with the pmLgDataStart field, for those operating
        // systems in which the file system does not begin at logical block 0
        // of the partition.
        DataBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmPartStatus Two words of status information about the partition. The low-order
        // byte of the low-order word contains status information used only
        // by the A/UX operating system:
        // Bit Meaning
        // 0 Set if a valid partition map entry
        // 1 Set if partition is already allocated; clear if available
        // 2 Set if partition is in use; may be cleared after a system reset
        // 3 Set if partition contains valid boot information
        // 4 Set if partition allows reading
        // 5 Set if partition allows writing
        // 6 Set if boot code is position-independent
        // 7 Unused
        // The remaining bytes of the pmPartStatus field are reserved. 
        Status = (ApplePartitionMapStatusFlags)BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmLgBootStart The logical block number of the first block containing boot code.
        BootCodeStartBlock = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmBootSize The size of the boot code, in bytes.
        BootCodeBlockCount = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmBootAddr The memory address where the boot code is to be loaded.
        BootCodeAddress = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmBootAddr2 Reserved.
        BootCodeAddress2 = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmBootEntry The memory address to which the Start Manager will transfer
        // control after loading the boot code into memory.
        BootCodeEntryPoint = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmBootEntry2 Reserved.
        BootCodeEntryPoint2 = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmBootCksum The boot code checksum. The Start Manager can compare this value
        // against the calculated checksum after loading the code.
        BootCodeChecksum = BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);
        offset += 4;

        // pmProcessor An optional string that identifies the type of processor that will
        // execute the boot code. Strings shorter than 16 bytes must be
        // terminated with the ASCII NUL character. The following processor
        // types are defined: 68000, 68020, 68030, and 68040. 
        ProcessorType = new String16(data.Slice(offset, String16.Size));
        offset += String16.Size;

        // pmPad Reserved.
        Debug.Assert(offset == data.Length, "Did not read the expected number of bytes for ApplePartitionMapEntry.");
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplePartitionMapEntry"/> struct with the specified values.
    /// </summary>
    /// <param name="mapBlockCount">The total number of entries in the partition map.</param>
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
        uint mapBlockCount,
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
        Signature = BlockSignature;
        Padding = 0;
        MapBlockCount = mapBlockCount;
        PartitionStartBlock = partitionStartBlock;
        PartitionBlockCount = partitionBlockCount;
        Name = name;
        Type = type;
        DataStartBlock = dataStartBlock;
        DataBlockCount = dataBlockCount;
        Status = statusFlags;
        BootCodeStartBlock = bootCodeStartBlock;
        BootCodeBlockCount = bootCodeBlockCount;
        BootCodeAddress = bootCodeAddress;
        BootCodeAddress2 = 0;
        BootCodeEntryPoint = bootCodeEntryPoint;
        BootCodeEntryPoint2 = 0;
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
        BinaryPrimitives.WriteUInt16BigEndian(data[offset..], Padding);
        offset += 2;

        // +$004 / 4: pmMapBlkCnt
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], MapBlockCount);
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
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], (uint)Status);
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
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], BootCodeAddress2);
        offset += 4;

        // +$06c / 4: pmBootEntry
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], BootCodeEntryPoint);
        offset += 4;

        // +$070 / 4: (reserved)
        BinaryPrimitives.WriteUInt32BigEndian(data[offset..], BootCodeEntryPoint2);
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
