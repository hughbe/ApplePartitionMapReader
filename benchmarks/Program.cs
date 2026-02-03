using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ApplePartitionMapReader;

BenchmarkRunner.Run<ApplePartitionMapBenchmarks>();

[MemoryDiagnoser]
public class ApplePartitionMapBenchmarks
{
    private byte[] _diskData = null!;
    private MemoryStream _stream = null!;

    [GlobalSetup]
    public void Setup()
    {
        _diskData = File.ReadAllBytes(Path.Combine("Samples", "test.iso"));
        _stream = new MemoryStream(_diskData);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _stream.Dispose();
    }

    [Benchmark]
    public bool IsApplePartitionMap()
    {
        _stream.Position = 0;
        return ApplePartitionMap.IsApplePartitionMap(_stream, 0);
    }

    [Benchmark]
    public ApplePartitionMap CreatePartitionMap()
    {
        _stream.Position = 0;
        return new ApplePartitionMap(_stream, 0);
    }

    [Benchmark]
    public List<ApplePartitionMapEntry> EnumerateEntries()
    {
        _stream.Position = 0;
        var map = new ApplePartitionMap(_stream, 0);
        return map.Entries.ToList();
    }

    [Benchmark]
    public int EnumeratePartitionsWithAccess()
    {
        _stream.Position = 0;
        var map = new ApplePartitionMap(_stream, 0);
        
        int count = 0;
        foreach (var partition in map.Entries)
        {
            count++;
            _ = partition.Name.ToString();
            _ = partition.Type.ToString();
            _ = partition.StatusFlags;
        }
        return count;
    }
}
