# ApplePartitionMapReader

ApplePartitionMapReader is a lightweight .NET library for reading and writing Apple Partition Maps from classic Macintosh disk images. It provides a simple, **zero-allocation** reading API to detect partition maps, enumerate partitions, and access partition metadata, as well as a writing API to create new disk images with custom partitions.

---

## Features

- Detect Apple Partition Maps in disk images
- Enumerate all partitions with detailed metadata
- Access partition properties: name, type, start block, block count, status flags
- Strongly-typed status flags with `ApplePartitionMapStatusFlags` enum
- **Zero-allocation reading API** using struct enumerators and span-based operations
- High-performance parsing using stack-allocated buffers
- **Writing API** to create new Apple Partition Map disk images with custom partitions
- Roundtrip support: write a disk image and read it back

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

## Writing a Disk Image

Use `ApplePartitionMapWriter` to create a new disk image containing an Apple Partition Map:

```csharp
using ApplePartitionMapReader;

var writer = new ApplePartitionMapWriter();

// Add partitions with their raw filesystem data
byte[] hfsData = File.ReadAllBytes("hfs-volume.dsk");
byte[] mfsData = File.ReadAllBytes("mfs-volume.dsk");
writer.AddPartition("HFS Volume", "Apple_HFS", hfsData);
writer.AddPartition("MFS Volume", "Apple_MFS", mfsData);

// Write the complete disk image (DDM + partition map + data)
using var output = File.Create("combined.dsk");
writer.WriteTo(output);
```

The writer automatically:
- Creates the Driver Descriptor Map at block 0
- Creates a self-referencing `Apple_partition_map` entry as entry 0
- Lays out partition data sequentially after the map entries
- Pads partition data to 512-byte block boundaries

You can also specify custom status flags per partition:

```csharp
writer.AddPartition("Boot", "Apple_HFS", bootData,
    ApplePartitionMapStatusFlags.Valid |
    ApplePartitionMapStatusFlags.Allocated |
    ApplePartitionMapStatusFlags.InUse |
    ApplePartitionMapStatusFlags.Bootable |
    ApplePartitionMapStatusFlags.Readable);
```

---

## Zero-Allocation Reading API

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

### ApplePartitionMapWriter

| Member | Description |
|--------|-------------|
| `void AddPartition(string, string, byte[], ApplePartitionMapStatusFlags)` | Adds a partition with the given name, type, raw data, and optional status flags |
| `void WriteTo(Stream)` | Writes the complete disk image (DDM + partition map + partition data) to a stream |

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
| `StatusFlags` | `ApplePartitionMapStatusFlags` | Partition status flags |
| `BootCodeStartBlock` | `uint` | First logical block of boot code |
| `BootCodeBlockCount` | `uint` | Size of boot code in bytes |
| `BootCodeAddress` | `uint` | Boot code load address |
| `BootCodeEntryPoint` | `uint` | Boot code entry point |
| `BootCodeChecksum` | `uint` | Boot code checksum |
| `ProcessorType` | `String16` | Processor type (e.g., "68000") |

The entry also supports writing:

| Member | Description |
|--------|-------------|
| `ApplePartitionMapEntry(uint, uint, uint, String32, String32, uint, uint, ApplePartitionMapStatusFlags, ...)` | Constructs an entry with the specified field values for writing |
| `void WriteTo(Span<byte>)` | Writes the entry to a span in big-endian format |

### String16 / String32

Fixed-size string types with zero-allocation APIs:

| Member | Description |
|--------|-------------|
| `int Length` | Gets the string length (excluding null terminator) |
| `bool TryFormat(Span<char>, out int)` | Formats the string into a span without allocation |
| `bool Equals(ReadOnlySpan<char>)` | Compares with a character span without allocation |
| `bool Equals(ReadOnlySpan<byte>)` | Compares with ASCII bytes without allocation |
| `ReadOnlySpan<byte> AsReadOnlySpan()` | Gets the raw bytes without copying |
| `static FromString(ReadOnlySpan<char>)` | Creates a fixed-size string from an ASCII character span (for writing) |
| `string ToString()` | Converts to a string (allocates) |
| `implicit operator string` | Implicit conversion to string (allocates) |

### ApplePartitionMapStatusFlags

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

## Apple Partition Map (APM) Format

The Apple Partition Map (APM) is the partitioning scheme used on classic Macintosh disks and disk images. Key points:

- Block size is 512 bytes. The Driver Descriptor Map (if present) occupies block 0.
- The partition map entries begin at block 1. Each partition map entry is stored in a 512-byte block, with the entry structure occupying the first 136 bytes of the block.
- Each entry starts with a 2-byte signature `0x504D` (ASCII "PM"). Important fields include:
    - `pmMapBlkCnt` (map entry count)
    - `pmPyPartStart` (partition start block)
    - `pmPartBlkCnt` (partition block count)
    - `pmPartName` (32-byte name)
    - `pmParType` (32-byte type)

- Partition start and size are expressed in 512-byte blocks; to compute a partition's byte offset and length, multiply the start block and block count by 512.

For more detailed documentation see:

- CiderPress APM notes: https://ciderpress2.com/formatdoc/APM-notes.html
- libvsapm APM documentation: https://github.com/libyal/libvsapm/blob/main/documentation/Apple%20partition%20map%20(APM)%20format.asciidoc

This library follows these conventions when reading and writing partition metadata via the `ApplePartitionMap`, `ApplePartitionMapEntry`, and `ApplePartitionMapWriter` types.

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

## Command-line Dumper

The repository includes a small command-line utility in the `dumper` project that can list and extract partitions from disk images using the Apple Partition Map.

- Run the dumper from the repository root:

```bash
dotnet run --project dumper -- <command> [options]
```

- Extract partitions:

```bash
dotnet run --project dumper -- extract <input> --output <dir> [--offset <bytes>] [--index <n>]
```

Options:
- `--output` : Destination directory for extracted partitions (default: current directory).
- `--offset` : Byte offset within the input file where the partition map starts (default: 0).
- `--index`  : Zero-based partition index to extract a single partition.

Example usages:

```bash
dotnet run --project dumper -- extract tests/Samples/test.iso --output extracted-test
dotnet run --project dumper -- extract tests/Samples/test.iso --output extracted --index 2
dotnet run --project dumper -- extract <input> --offset 512 --output extracted
```

Extracted files are named `<index>-<partitionName>.img` (invalid filename characters are replaced with underscores).
