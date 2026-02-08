using ApplePartitionMapReader.Utilities;

namespace ApplePartitionMapReader.Tests;

public class ApplePartitionMapWriterTests
{
    private const ApplePartitionMapStatusFlags DefaultFlags =
        ApplePartitionMapStatusFlags.Valid |
        ApplePartitionMapStatusFlags.Allocated |
        ApplePartitionMapStatusFlags.InUse |
        ApplePartitionMapStatusFlags.Readable |
        ApplePartitionMapStatusFlags.Writable;

    [Fact]
    public void WriteTo_SinglePartition_Roundtrip()
    {
        byte[] partitionData = new byte[1024];
        Random.Shared.NextBytes(partitionData);

        var writer = new ApplePartitionMapWriter();
        writer.AddPartition("TestPart", "Apple_HFS", partitionData);

        using var stream = new MemoryStream();
        writer.WriteTo(stream);

        stream.Position = 0;
        var map = new ApplePartitionMap(stream, 0);

        // 2 entries: partition map self-entry + 1 user partition.
        Assert.Equal(2, map.Count);

        // Partition 0: self-referencing partition map entry.
        var entry0 = map[0];
        Assert.Equal(ApplePartitionMapEntry.BlockSignature, entry0.Signature);
        Assert.Equal(2u, entry0.MapEntryCount);
        Assert.Equal("Apple", entry0.Name.ToString());
        Assert.Equal("Apple_partition_map", entry0.Type.ToString());
        Assert.Equal(1u, entry0.PartitionStartBlock);
        Assert.Equal(2u, entry0.PartitionBlockCount);

        // Partition 1: user partition.
        var entry1 = map[1];
        Assert.Equal(ApplePartitionMapEntry.BlockSignature, entry1.Signature);
        Assert.Equal(2u, entry1.MapEntryCount);
        Assert.Equal("TestPart", entry1.Name.ToString());
        Assert.Equal("Apple_HFS", entry1.Type.ToString());
        Assert.Equal(3u, entry1.PartitionStartBlock); // Block 0=DDM, 1-2=map, 3=data start.
        Assert.Equal(2u, entry1.PartitionBlockCount); // 1024 bytes = 2 blocks.
        Assert.Equal(DefaultFlags, entry1.StatusFlags);

        // Verify the partition data is readable at the correct offset.
        stream.Seek(entry1.PartitionStartBlock * 512, SeekOrigin.Begin);
        byte[] readBack = new byte[partitionData.Length];
        stream.ReadExactly(readBack);
        Assert.Equal(partitionData, readBack);
    }

    [Fact]
    public void WriteTo_MultiplePartitions_Roundtrip()
    {
        byte[] data1 = new byte[512];
        byte[] data2 = new byte[2048];
        byte[] data3 = new byte[768]; // Not a multiple of 512 - tests padding.
        Random.Shared.NextBytes(data1);
        Random.Shared.NextBytes(data2);
        Random.Shared.NextBytes(data3);

        var writer = new ApplePartitionMapWriter();
        writer.AddPartition("Part1", "Apple_HFS", data1);
        writer.AddPartition("Part2", "Apple_MFS", data2);
        writer.AddPartition("Part3", "Apple_PRODOS", data3, ApplePartitionMapStatusFlags.Valid | ApplePartitionMapStatusFlags.Allocated | ApplePartitionMapStatusFlags.InUse | ApplePartitionMapStatusFlags.Readable);

        using var stream = new MemoryStream();
        writer.WriteTo(stream);

        stream.Position = 0;
        var map = new ApplePartitionMap(stream, 0);

        // 4 entries: partition map + 3 user partitions.
        Assert.Equal(4, map.Count);

        // Entry 0: partition map.
        var e0 = map[0];
        Assert.Equal("Apple", e0.Name.ToString());
        Assert.Equal("Apple_partition_map", e0.Type.ToString());
        Assert.Equal(1u, e0.PartitionStartBlock);
        Assert.Equal(4u, e0.PartitionBlockCount); // 4 map entries.

        // Entry 1.
        var e1 = map[1];
        Assert.Equal("Part1", e1.Name.ToString());
        Assert.Equal("Apple_HFS", e1.Type.ToString());
        Assert.Equal(5u, e1.PartitionStartBlock); // Block 0=DDM, 1-4=map, 5=data.
        Assert.Equal(1u, e1.PartitionBlockCount); // 512 bytes = 1 block.

        // Entry 2.
        var e2 = map[2];
        Assert.Equal("Part2", e2.Name.ToString());
        Assert.Equal("Apple_MFS", e2.Type.ToString());
        Assert.Equal(6u, e2.PartitionStartBlock);
        Assert.Equal(4u, e2.PartitionBlockCount); // 2048 bytes = 4 blocks.

        // Entry 3.
        var e3 = map[3];
        Assert.Equal("Part3", e3.Name.ToString());
        Assert.Equal("Apple_PRODOS", e3.Type.ToString());
        Assert.Equal(10u, e3.PartitionStartBlock);
        Assert.Equal(2u, e3.PartitionBlockCount); // 768 bytes -> 2 blocks (padded).
        Assert.Equal(ApplePartitionMapStatusFlags.Valid | ApplePartitionMapStatusFlags.Allocated | ApplePartitionMapStatusFlags.InUse | ApplePartitionMapStatusFlags.Readable, e3.StatusFlags);

        // Verify partition data readback.
        stream.Seek(e1.PartitionStartBlock * 512, SeekOrigin.Begin);
        byte[] readBack1 = new byte[data1.Length];
        stream.ReadExactly(readBack1);
        Assert.Equal(data1, readBack1);

        stream.Seek(e2.PartitionStartBlock * 512, SeekOrigin.Begin);
        byte[] readBack2 = new byte[data2.Length];
        stream.ReadExactly(readBack2);
        Assert.Equal(data2, readBack2);

        stream.Seek(e3.PartitionStartBlock * 512, SeekOrigin.Begin);
        byte[] readBack3 = new byte[data3.Length];
        stream.ReadExactly(readBack3);
        Assert.Equal(data3, readBack3);
    }

    [Fact]
    public void WriteTo_DriverDescriptorMap_WrittenCorrectly()
    {
        byte[] data = new byte[512];
        var writer = new ApplePartitionMapWriter();
        writer.AddPartition("Test", "Apple_HFS", data);

        using var stream = new MemoryStream();
        writer.WriteTo(stream);

        stream.Position = 0;
        var map = new ApplePartitionMap(stream, 0);

        var ddm = map.DriverDescriptorMap;
        Assert.NotNull(ddm);
        Assert.Equal(0x4552, ddm.Value.Signature);
        Assert.Equal(512u, ddm.Value.BlockSize);
        Assert.Equal(4u, ddm.Value.BlockCount); // DDM(1) + map(2) + data(1) = 4 blocks.
    }

    [Fact]
    public void WriteTo_HfsMfsProDos_Roundtrip()
    {
        byte[] hfsData = File.ReadAllBytes(Path.Combine("Inputs", "HFS", "Apple II Setup.dsk"));
        byte[] mfsData = File.ReadAllBytes(Path.Combine("Inputs", "MFS", "SystemStartup.dsk"));
        byte[] proDosData = File.ReadAllBytes(Path.Combine("Inputs", "ProDOS", "gs-os5.02.dsk.po"));

        var writer = new ApplePartitionMapWriter();
        writer.AddPartition("HFS Volume", "Apple_HFS", hfsData);
        writer.AddPartition("MFS Volume", "Apple_MFS", mfsData);
        writer.AddPartition("ProDOS Volume", "Apple_PRODOS", proDosData);

        using var stream = new MemoryStream();
        writer.WriteTo(stream);

        // Write the combined disk image to the Inputs directory.
        string outputPath = Path.Combine("Inputs", "combined.dsk");
        File.WriteAllBytes(outputPath, stream.ToArray());

        // Read back and verify.
        stream.Position = 0;
        var map = new ApplePartitionMap(stream, 0);

        Assert.Equal(4, map.Count);

        // Partition map self-entry.
        var partMap = map[0];
        Assert.Equal("Apple", partMap.Name.ToString());
        Assert.Equal("Apple_partition_map", partMap.Type.ToString());
        Assert.Equal(4u, partMap.MapEntryCount);

        // HFS partition.
        var hfs = map[1];
        Assert.Equal("HFS Volume", hfs.Name.ToString());
        Assert.Equal("Apple_HFS", hfs.Type.ToString());
        Assert.Equal((uint)((hfsData.Length + 511) / 512), hfs.PartitionBlockCount);

        // MFS partition.
        var mfs = map[2];
        Assert.Equal("MFS Volume", mfs.Name.ToString());
        Assert.Equal("Apple_MFS", mfs.Type.ToString());
        Assert.Equal((uint)((mfsData.Length + 511) / 512), mfs.PartitionBlockCount);

        // ProDOS partition.
        var proDos = map[3];
        Assert.Equal("ProDOS Volume", proDos.Name.ToString());
        Assert.Equal("Apple_PRODOS", proDos.Type.ToString());
        Assert.Equal((uint)((proDosData.Length + 511) / 512), proDos.PartitionBlockCount);

        // Verify data roundtrip for each partition.
        stream.Seek(hfs.PartitionStartBlock * 512, SeekOrigin.Begin);
        byte[] hfsReadBack = new byte[hfsData.Length];
        stream.ReadExactly(hfsReadBack);
        Assert.Equal(hfsData, hfsReadBack);

        stream.Seek(mfs.PartitionStartBlock * 512, SeekOrigin.Begin);
        byte[] mfsReadBack = new byte[mfsData.Length];
        stream.ReadExactly(mfsReadBack);
        Assert.Equal(mfsData, mfsReadBack);

        stream.Seek(proDos.PartitionStartBlock * 512, SeekOrigin.Begin);
        byte[] proDosReadBack = new byte[proDosData.Length];
        stream.ReadExactly(proDosReadBack);
        Assert.Equal(proDosData, proDosReadBack);
    }

    [Fact]
    public void WriteTo_NullStream_ThrowsArgumentNullException()
    {
        var writer = new ApplePartitionMapWriter();
        writer.AddPartition("Test", "Apple_HFS", new byte[512]);

        Assert.Throws<ArgumentNullException>("stream", () => writer.WriteTo(null!));
    }

    [Fact]
    public void WriteTo_NoPartitions_ThrowsInvalidOperationException()
    {
        var writer = new ApplePartitionMapWriter();

        using var stream = new MemoryStream();
        Assert.Throws<InvalidOperationException>(() => writer.WriteTo(stream));
    }

    [Fact]
    public void AddPartition_NullName_ThrowsArgumentNullException()
    {
        var writer = new ApplePartitionMapWriter();
        Assert.Throws<ArgumentNullException>("name", () => writer.AddPartition(null!, "Apple_HFS", new byte[512]));
    }

    [Fact]
    public void AddPartition_NullType_ThrowsArgumentNullException()
    {
        var writer = new ApplePartitionMapWriter();
        Assert.Throws<ArgumentNullException>("type", () => writer.AddPartition("Test", null!, new byte[512]));
    }

    [Fact]
    public void AddPartition_NullData_ThrowsArgumentNullException()
    {
        var writer = new ApplePartitionMapWriter();
        Assert.Throws<ArgumentNullException>("data", () => writer.AddPartition("Test", "Apple_HFS", null!));
    }

    [Fact]
    public void WriteTo_EnumerateWrittenPartitions()
    {
        byte[] data = new byte[1536]; // 3 blocks.
        Random.Shared.NextBytes(data);

        var writer = new ApplePartitionMapWriter();
        writer.AddPartition("Disk", "Apple_HFS", data);

        using var stream = new MemoryStream();
        writer.WriteTo(stream);

        stream.Position = 0;
        var map = new ApplePartitionMap(stream, 0);

        int count = 0;
        foreach (var partition in map)
        {
            count++;
            Assert.Equal(ApplePartitionMapEntry.BlockSignature, partition.Signature);
        }
        Assert.Equal(2, count);
    }
}
