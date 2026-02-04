# ApplePartitionMapReader

ApplePartitionMapReader is a lightweight .NET library for reading Apple Partition Maps from classic Macintosh disk images. It provides a simple, **zero-allocation** API to detect partition maps, enumerate partitions, and access partition metadata.

---

## Features

- Detect Apple Partition Maps in disk images
- Enumerate all partitions with detailed metadata
- Access partition properties: name, type, start block, block count, status flags
- Strongly-typed status flags with `ApplePartitionMapStatus` enum
- **Zero-allocation API** using struct enumerators and span-based operations
- High-performance parsing using stack-allocated buffers

---

## Installation

Install from NuGet:

```sh
dotnet add package ApplePartitionMapReader
```

Or reference the project directly:

```sh
dotnet add reference ../src/ApplePartitionMapReader.csproj
```

---

## Quick Start Example

```csharp
using ApplePartitionMapReader;

// Open a disk image containing an Apple Partition Map
using var stream = File.OpenRead("disk.iso");

// Check if the stream contains an Apple Partition Map
if (ApplePartitionMap.IsApplePartitionMap(stream, 0))
{
    var map = new ApplePartitionMap(stream, 0);
    
    // Enumerate all partitions (zero allocation)
    foreach (var partition in map)
    {
        Console.WriteLine($"Name: {partition.Name}");
        Console.WriteLine($"Type: {partition.Type}");
        Console.WriteLine($"Start: {partition.PartitionStartBlock}");
        Console.WriteLine($"Size: {partition.PartitionBlockCount} blocks");
        Console.WriteLine($"Status: {partition.StatusFlags}");
        Console.WriteLine();
    }
    
    // Or access by index
    var firstPartition = map[0];
    Console.WriteLine($"First partition: {firstPartition.Name}");
}
```

---

## Zero-Allocation API

The library is designed for high-performance scenarios with zero heap allocations on the hot path:

### Zero-Allocation Enumeration

```csharp
// The foreach loop uses a struct enumerator - no allocation!
foreach (var partition in map)
{
    // Process partition
}

// Get count without enumerating
int count = map.EntryCount;
```

### Zero-Allocation String Formatting

`String16` and `String32` types support span-based formatting to avoid string allocations:

```csharp
Span<char> buffer = stackalloc char[32];
if (partition.Name.TryFormat(buffer, out int charsWritten))
{
    ReadOnlySpan<char> name = buffer[..charsWritten];
    // Use name without allocation
}

// Get length without allocating
int length = partition.Name.Length;
```

### Zero-Allocation String Comparison

Compare partition names/types directly without allocating strings:

```csharp
// Compare with a string span - no allocation
if (partition.Type.Equals("Apple_HFS".AsSpan()))
{
    // Handle HFS partition
}

// Compare with ASCII bytes
if (partition.Type.Equals("Apple_HFS"u8))
{
    // Handle HFS partition
}
```

### Zero-Allocation Driver Descriptor Map Access

```csharp
// Use TryGet pattern to avoid nullable struct boxing
if (map.TryGetDriverDescriptorMap(out var ddm))
{
    Console.WriteLine($"Block size: {ddm.BlockSize}");
}
```

---

## API Overview

### ApplePartitionMap

| Member | Description |
|--------|-------------|
| `ApplePartitionMap(Stream, int)` | Constructs a partition map reader from a seekable, readable stream at the specified offset |
| `static bool IsApplePartitionMap(Stream, int)` | Checks if the stream contains a valid Apple Partition Map at the given offset |
| `int EntryCount` | Gets the number of partition entries in the map |
| `Enumerator GetEnumerator()` | Returns a struct enumerator for zero-allocation `foreach` support |
| `ApplePartitionMapEntry this[int]` | Gets the partition entry at the specified index |
| `DriverDescriptorMap? DriverDescriptorMap` | Gets the driver descriptor map, or `null` if the block is blank |
| `bool TryGetDriverDescriptorMap(out DriverDescriptorMap)` | Zero-allocation alternative to get the driver descriptor map |

### ApplePartitionMapEntry

Represents a single partition in the map with the following properties:

| Property | Type | Description |
|----------|------|-------------|
| `Signature` | `ushort` | Partition signature (0x504D = 'PM') |
| `MapEntryCount` | `uint` | Total number of entries in the partition map |
| `Name` | `String32` | Partition name |
| `Type` | `String32` | Partition type (e.g., `Apple_HFS`, `Apple_Driver`) |
| `PartitionStartBlock` | `uint` | First block of the partition |
| `PartitionBlockCount` | `uint` | Number of blocks in the partition |
| `DataStartBlock` | `uint` | First logical block of data area |
| `DataBlockCount` | `uint` | Number of blocks in data area |
| `StatusFlags` | `ApplePartitionMapStatus` | Partition status flags |
| `BootCodeStartBlock` | `uint` | First logical block of boot code |
| `BootCodeBlockCount` | `uint` | Size of boot code in bytes |
| `BootCodeAddress` | `uint` | Boot code load address |
| `BootCodeEntryPoint` | `uint` | Boot code entry point |
| `BootCodeChecksum` | `uint` | Boot code checksum |
| `ProcessorType` | `String16` | Processor type (e.g., "68000") |

### String16 / String32

Fixed-size string types with zero-allocation APIs:

| Member | Description |
|--------|-------------|
| `int Length` | Gets the string length (excluding null terminator) |
| `bool TryFormat(Span<char>, out int)` | Formats the string into a span without allocation |
| `bool Equals(ReadOnlySpan<char>)` | Compares with a character span without allocation |
| `bool Equals(ReadOnlySpan<byte>)` | Compares with ASCII bytes without allocation |
| `ReadOnlySpan<byte> AsReadOnlySpan()` | Gets the raw bytes without copying |
| `string ToString()` | Converts to a string (allocates) |
| `implicit operator string` | Implicit conversion to string (allocates) |

### ApplePartitionMapStatus

Flags enum representing partition status:

| Flag | Value | Description |
|------|-------|-------------|
| `Valid` | 0x00000001 | Entry is valid |
| `Allocated` | 0x00000002 | Entry is allocated |
| `InUse` | 0x00000004 | Entry in use |
| `Bootable` | 0x00000008 | Contains boot information |
| `Readable` | 0x00000010 | Partition is readable |
| `Writable` | 0x00000020 | Partition is writable |
| `BootCodePositionIndependent` | 0x00000040 | Boot code is position independent |
| `OSSpecific1` | 0x00000080 | OS-specific flag |
| `ChainCompatibleDriver` | 0x00000100 | Contains chain-compatible driver |
| `RealDriver` | 0x00000200 | Contains a real driver |
| `ChainDriver` | 0x00000400 | Contains a chain driver |
| `AutoMount` | 0x40000000 | Automatically mount at startup |
| `StartupPartition` | 0x80000000 | The startup partition |

---

## Common Partition Types

| Type | Description |
|------|-------------|
| `Apple_partition_map` | The partition map itself |
| `Apple_Driver` | Device driver partition |
| `Apple_Driver43` | SCSI driver partition |
| `Apple_HFS` | HFS filesystem partition |
| `Apple_Free` | Free/unused space |
| `Apple_Scratch` | Scratch partition |

---

## License

MIT License - see [LICENSE](LICENSE) for details.

## Related Projects

- [AppleDiskImageReader](https://github.com/hughbe/AppleDiskImageReader) - Reader for Apple II universal disk (.2mg) images
- [AppleIIDiskReader](https://github.com/hughbe/AppleIIDiskReader) - Reader for Apple II DOS 3.3 disk (.dsk) images
- [ProDosVolumeReader](https://github.com/hughbe/ProDosVolumeReader) - Reader for ProDOS (.po) volumes
- [WozDiskImageReader](https://github.com/hughbe/WozDiskImageReader) - Reader for WOZ (.woz) disk images
- [DiskCopyReader](https://github.com/hughbe/DiskCopyReader) - Reader for Disk Copy 4.2 (.dc42) images
- [MfsReader](https://github.com/hughbe/MfsReader) - Reader for MFS (Macintosh File System) volumes
- [HfsReader](https://github.com/hughbe/HfsReader) - Reader for HFS (Hierarchical File System) volumes
- [ResourceForkReader](https://github.com/hughbe/ResourceForkReader) - Reader for Macintosh resource forks
- [BinaryIIReader](https://github.com/hughbe/BinaryIIReader) - Reader for Binary II (.bny, .bxy) archives
- [StuffItReader](https://github.com/hughbe/StuffItReader) - Reader for StuffIt (.sit) archives
- [ShrinkItReader](https://github.com/hughbe/ShrinkItReader) - Reader for ShrinkIt (.shk, .sdk) archives
