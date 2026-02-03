namespace ApplePartitionMapReader.Tests;

public class ApplePartitionMapTests
{
    [Theory]
    [InlineData("test.iso")]
    public void Ctor_Stream(string diskName)
    {
        var filePath = Path.Combine("Samples", diskName);
        using var stream = File.OpenRead(filePath);
        var map = new ApplePartitionMap(stream, 0);
    }

    [Fact]
    public void Ctor_NullStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => new ApplePartitionMap(null!, 0));
    }

    [Fact]
    public void Entries_TestIso_ReturnsCorrectCount()
    {
        var filePath = Path.Combine("Samples", "test.iso");
        using var stream = File.OpenRead(filePath);
        var map = new ApplePartitionMap(stream, 0);

        var partitions = map.Entries.ToList();

        Assert.Equal(4, partitions.Count);
    }

    [Fact]
    public void Indexer_TestIso_Partition0_ApplePartitionMap()
    {
        var filePath = Path.Combine("Samples", "test.iso");
        using var stream = File.OpenRead(filePath);
        var map = new ApplePartitionMap(stream, 0);

        var partition = map[0];

        Assert.Equal(ApplePartitionMapEntry.BlockSignature, partition.Signature);
        Assert.Equal(4u, partition.MapEntryCount);
        Assert.Equal("Apple", partition.Name.ToString());
        Assert.Equal("Apple_partition_map", partition.Type.ToString());
        Assert.Equal(1u, partition.PartitionStartBlock);
        Assert.Equal(63u, partition.PartitionBlockCount);
        Assert.Equal(0u, partition.DataStartBlock);
        Assert.Equal(63u, partition.DataBlockCount);
        Assert.Equal(ApplePartitionMapStatus.Valid | ApplePartitionMapStatus.Allocated | ApplePartitionMapStatus.InUse | ApplePartitionMapStatus.Readable | ApplePartitionMapStatus.Writable, partition.StatusFlags);
    }

    [Fact]
    public void Indexer_TestIso_Partition1_AppleDriver()
    {
        var filePath = Path.Combine("Samples", "test.iso");
        using var stream = File.OpenRead(filePath);
        var map = new ApplePartitionMap(stream, 0);

        var partition = map[1];

        Assert.Equal(ApplePartitionMapEntry.BlockSignature, partition.Signature);
        Assert.Equal(4u, partition.MapEntryCount);
        Assert.Equal("Macintosh", partition.Name.ToString());
        Assert.Equal("Apple_Driver", partition.Type.ToString());
        Assert.Equal(64u, partition.PartitionStartBlock);
        Assert.Equal(32u, partition.PartitionBlockCount);
        Assert.Equal(0u, partition.DataStartBlock);
        Assert.Equal(32u, partition.DataBlockCount);
        Assert.Equal(ApplePartitionMapStatus.Valid | ApplePartitionMapStatus.Allocated | ApplePartitionMapStatus.InUse | ApplePartitionMapStatus.Bootable | ApplePartitionMapStatus.Readable | ApplePartitionMapStatus.Writable | ApplePartitionMapStatus.BootCodePositionIndependent, partition.StatusFlags);
    }

    [Fact]
    public void Indexer_TestIso_Partition2_AppleHFS()
    {
        var filePath = Path.Combine("Samples", "test.iso");
        using var stream = File.OpenRead(filePath);
        var map = new ApplePartitionMap(stream, 0);

        var partition = map[2];

        Assert.Equal(ApplePartitionMapEntry.BlockSignature, partition.Signature);
        Assert.Equal(4u, partition.MapEntryCount);
        Assert.Equal("MacOS", partition.Name.ToString());
        Assert.Equal("Apple_HFS", partition.Type.ToString());
        Assert.Equal(96u, partition.PartitionStartBlock);
        Assert.Equal(20000u, partition.PartitionBlockCount);
        Assert.Equal(0u, partition.DataStartBlock);
        Assert.Equal(20000u, partition.DataBlockCount);
        Assert.Equal(ApplePartitionMapStatus.Valid | ApplePartitionMapStatus.Allocated | ApplePartitionMapStatus.InUse | ApplePartitionMapStatus.Readable | ApplePartitionMapStatus.Writable | ApplePartitionMapStatus.OSSpecific1, partition.StatusFlags);
    }

    [Fact]
    public void Indexer_TestIso_Partition3_AppleFree()
    {
        var filePath = Path.Combine("Samples", "test.iso");
        using var stream = File.OpenRead(filePath);
        var map = new ApplePartitionMap(stream, 0);

        var partition = map[3];

        Assert.Equal(ApplePartitionMapEntry.BlockSignature, partition.Signature);
        Assert.Equal(4u, partition.MapEntryCount);
        Assert.Equal("Extra", partition.Name.ToString());
        Assert.Equal("Apple_Free", partition.Type.ToString());
        Assert.Equal(20096u, partition.PartitionStartBlock);
        Assert.Equal(136274u, partition.PartitionBlockCount);
        Assert.Equal(0u, partition.DataStartBlock);
        Assert.Equal(136274u, partition.DataBlockCount);
        Assert.Equal(ApplePartitionMapStatus.Valid | ApplePartitionMapStatus.Allocated | ApplePartitionMapStatus.InUse | ApplePartitionMapStatus.Readable | ApplePartitionMapStatus.Writable, partition.StatusFlags);
    }
}
