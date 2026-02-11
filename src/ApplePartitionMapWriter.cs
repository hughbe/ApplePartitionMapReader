using ApplePartitionMapReader.Utilities;

namespace ApplePartitionMapReader;

/// <summary>
/// Builds and writes an Apple Partition Map disk image to a stream.
/// </summary>
public sealed class ApplePartitionMapWriter
{
    private readonly List<PartitionDefinition> _partitions = new();

    /// <summary>
    /// Adds a partition with the specified name, type, data and status flags.
    /// </summary>
    /// <param name="name">The partition name (at most 32 characters).</param>
    /// <param name="type">The partition type (at most 32 characters), e.g. "Apple_HFS".</param>
    /// <param name="data">The raw partition data. Will be zero-padded to the next 512-byte block boundary.</param>
    /// <param name="statusFlags">The partition status flags.</param>
    public void AddPartition(string name, string type, byte[] data, ApplePartitionMapStatusFlags statusFlags = ApplePartitionMapStatusFlags.Valid | ApplePartitionMapStatusFlags.Allocated | ApplePartitionMapStatusFlags.InUse | ApplePartitionMapStatusFlags.Readable | ApplePartitionMapStatusFlags.Writable)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(data);

        _partitions.Add(new PartitionDefinition(name, type, data, statusFlags));
    }

    /// <summary>
    /// Writes the complete Apple Partition Map disk image to the specified stream.
    /// The output contains a Driver Descriptor Map at block 0, partition map entries starting at block 1,
    /// and partition data following the map entries.
    /// </summary>
    /// <param name="stream">The destination stream. Must be writable.</param>
    public void WriteTo(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite)
        {
            throw new ArgumentException("Stream must be writable.", nameof(stream));
        }

        if (_partitions.Count == 0)
        {
            throw new InvalidOperationException("At least one partition must be added before writing.");
        }

        // The partition map always includes an entry for itself, plus one entry per user partition.
        int mapEntryCount = _partitions.Count + 1;

        // Block layout:
        //   Block 0:           Driver Descriptor Map
        //   Blocks 1..N:       Partition map entries (N = mapEntryCount)
        //   Blocks N+1..:      Partition data
        uint dataStartBlock = (uint)(mapEntryCount + 1); // +1 for DDM at block 0

        // Calculate block count for each partition and total block count.
        uint totalBlocks = dataStartBlock;
        uint[] partitionBlockCounts = new uint[_partitions.Count];
        for (int i = 0; i < _partitions.Count; i++)
        {
            partitionBlockCounts[i] = (uint)((_partitions[i].Data.Length + 511) / 512);
            totalBlocks += partitionBlockCounts[i];
        }

        Span<byte> block = stackalloc byte[512];

        // Block 0: Driver Descriptor Map.
        block.Clear();
        var ddm = new DriverDescriptorMap(512, totalBlocks, 0, 0, 0, 0, default);
        ddm.WriteTo(block);
        stream.Write(block);

        // Block 1: Partition map self-entry.
        block.Clear();
        var mapSelfEntry = new ApplePartitionMapEntry(
            mapBlockCount: (uint)mapEntryCount,
            partitionStartBlock: 1,
            partitionBlockCount: (uint)mapEntryCount,
            name: String32.FromString("Apple"),
            type: String32.FromString(ApplePartitionMapIdentifiers.ApplePartitionMap),
            dataStartBlock: 0,
            dataBlockCount: (uint)mapEntryCount,
            statusFlags: ApplePartitionMapStatusFlags.Valid | ApplePartitionMapStatusFlags.Allocated | ApplePartitionMapStatusFlags.InUse | ApplePartitionMapStatusFlags.Readable | ApplePartitionMapStatusFlags.Writable
        );
        mapSelfEntry.WriteTo(block);
        stream.Write(block);

        // Blocks 2..N: User partition entries.
        uint currentDataBlock = dataStartBlock;
        for (int i = 0; i < _partitions.Count; i++)
        {
            block.Clear();
            var entry = new ApplePartitionMapEntry(
                mapBlockCount: (uint)mapEntryCount,
                partitionStartBlock: currentDataBlock,
                partitionBlockCount: partitionBlockCounts[i],
                name: String32.FromString(_partitions[i].Name),
                type: String32.FromString(_partitions[i].Type),
                dataStartBlock: 0,
                dataBlockCount: partitionBlockCounts[i],
                statusFlags: _partitions[i].StatusFlags
            );
            entry.WriteTo(block);
            stream.Write(block);
            currentDataBlock += partitionBlockCounts[i];
        }

        // Write partition data, each padded to 512-byte block boundary.
        for (int i = 0; i < _partitions.Count; i++)
        {
            byte[] data = _partitions[i].Data;
            stream.Write(data);

            int remainder = data.Length % 512;
            if (remainder != 0)
            {
                block.Clear();
                stream.Write(block[..(512 - remainder)]);
            }
        }
    }

    private sealed record PartitionDefinition(string Name, string Type, byte[] Data, ApplePartitionMapStatusFlags StatusFlags);
}
