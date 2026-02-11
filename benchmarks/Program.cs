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
    public int EnumerateEntries_ZeroAlloc()
    {
        _stream.Position = 0;
        var map = new ApplePartitionMap(_stream, 0);
        int count = 0;
        foreach (var partition in map)
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public int EnumeratePartitionsWithAccess_ZeroAlloc()
    {
        _stream.Position = 0;
        var map = new ApplePartitionMap(_stream, 0);
        
        int count = 0;
        Span<char> nameBuffer = stackalloc char[32];
        Span<char> typeBuffer = stackalloc char[32];
        foreach (var partition in map)
        {
            count++;
            partition.Name.TryFormat(nameBuffer, out _);
            partition.Type.TryFormat(typeBuffer, out _);
            _ = partition.Status;
        }
        return count;
    }

    [Benchmark]
    public int EnumeratePartitionsWithAccess_Allocating()
    {
        _stream.Position = 0;
        var map = new ApplePartitionMap(_stream, 0);
        
        int count = 0;
        foreach (var partition in map)
        {
            count++;
            _ = partition.Name.ToString();
            _ = partition.Type.ToString();
            _ = partition.Status;
        }
        return count;
    }
}
