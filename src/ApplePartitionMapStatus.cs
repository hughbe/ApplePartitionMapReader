namespace ApplePartitionMapReader;

/// <summary>
/// Represents the status flags for an Apple Partition Map entry.
/// </summary>
[Flags]
public enum ApplePartitionMapStatus : uint
{
    /// <summary>
    /// No flags set.
    /// </summary>
    None = 0x00000000,

    /// <summary>
    /// Entry is valid (A/UX).
    /// </summary>
    Valid = 0x00000001,

    /// <summary>
    /// Entry is allocated (A/UX).
    /// </summary>
    Allocated = 0x00000002,

    /// <summary>
    /// Entry in use (A/UX).
    /// </summary>
    InUse = 0x00000004,

    /// <summary>
    /// Entry contains boot information (A/UX).
    /// </summary>
    Bootable = 0x00000008,

    /// <summary>
    /// Partition is readable (A/UX).
    /// </summary>
    Readable = 0x00000010,

    /// <summary>
    /// Partition is writable (A/UX, Macintosh).
    /// </summary>
    Writable = 0x00000020,

    /// <summary>
    /// Boot code is position independent (A/UX).
    /// </summary>
    BootCodePositionIndependent = 0x00000040,

    /// <summary>
    /// OS specific flag 1.
    /// </summary>
    OSSpecific1 = 0x00000080,

    /// <summary>
    /// Partition contains chain-compatible driver (Macintosh).
    /// </summary>
    ChainCompatibleDriver = 0x00000100,

    /// <summary>
    /// Partition contains a real driver (Macintosh).
    /// </summary>
    RealDriver = 0x00000200,

    /// <summary>
    /// Partition contains a chain driver (Macintosh).
    /// </summary>
    ChainDriver = 0x00000400,

    /// <summary>
    /// Automatically mount at startup (Macintosh).
    /// </summary>
    AutoMount = 0x40000000,

    /// <summary>
    /// The startup partition (Macintosh).
    /// </summary>
    StartupPartition = 0x80000000,
}
