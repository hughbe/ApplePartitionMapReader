namespace ApplePartitionMapReader;

/// <summary>
/// Provides well-known partition type identifiers for Apple Partition Map entries.
/// Types beginning with "Apple_" are reserved for assignment by Apple.
/// </summary>
public static class ApplePartitionMapIdentifiers
{
    /// <summary>
    /// Boot partition for Mac OS X on New World Macs (Open Firmware 3.0+).
    /// Used when the file system on the main partition is not supported by Open Firmware.
    /// Contains BootX on an HFS filesystem.
    /// </summary>
    public const string AppleBoot = "Apple_Boot";

    /// <summary>
    /// RAID boot partition.
    /// </summary>
    public const string AppleBootRaid = "Apple_Boot_RAID";

    /// <summary>
    /// NewWorld bootblock partition used by yaboot and GRUB for loading PowerPC Linux.
    /// Must be HFS formatted for Open Firmware access. Will not automount under Mac OS X.
    /// </summary>
    public const string AppleBootstrap = "Apple_Bootstrap";

    /// <summary>
    /// Classic Mac OS device driver partition.
    /// </summary>
    public const string AppleDriver = "Apple_Driver";

    /// <summary>
    /// SCSI Manager 4.3 device driver partition for Classic Mac OS.
    /// </summary>
    public const string AppleDriver43 = "Apple_Driver43";

    /// <summary>
    /// SCSI CD-ROM device driver partition for Classic Mac OS.
    /// </summary>
    public const string AppleDriver43CD = "Apple_Driver43_CD";

    /// <summary>
    /// ATA device driver partition for Classic Mac OS.
    /// </summary>
    public const string AppleDriverATA = "Apple_Driver_ATA";

    /// <summary>
    /// ATAPI device driver partition for Classic Mac OS.
    /// </summary>
    public const string AppleDriverATAPI = "Apple_Driver_ATAPI";

    /// <summary>
    /// I/O Kit driver partition for Classic Mac OS.
    /// </summary>
    public const string AppleDriverIOKit = "Apple_Driver_IOKit";

    /// <summary>
    /// Open Firmware driver partition.
    /// </summary>
    public const string AppleDriverOpenFirmware = "Apple_Driver_OpenFirmware";

    /// <summary>
    /// Unused partition map entry.
    /// </summary>
    public const string AppleExtra = "Apple_Extra";

    /// <summary>
    /// Free space partition entry.
    /// </summary>
    public const string AppleFree = "Apple_Free";

    /// <summary>
    /// FireWire device driver partition for Classic Mac OS.
    /// </summary>
    public const string AppleFWDriver = "Apple_FWDriver";

    /// <summary>
    /// Hierarchical File System (HFS or HFS+) partition.
    /// Can also contain an MS-DOS formatted file system (FAT).
    /// </summary>
    public const string AppleHFS = "Apple_HFS";

    /// <summary>
    /// HFS Plus partition without an HFS wrapper.
    /// Introduced with Mac OS X 10.3, used for case-sensitive HFS+.
    /// Standard partition type on Intel-based Macs (which use GPT instead of APM).
    /// </summary>
    public const string AppleHFSX = "Apple_HFSX";

    /// <summary>
    /// Secondary loader partition for Old World Macs.
    /// Contains BootX machine code in XCOFF format. Discontinued with Mac OS X 10.3.
    /// </summary>
    public const string AppleLoader = "Apple_Loader";

    /// <summary>
    /// Firmware partition used by iPod to load the firmware/OS.
    /// </summary>
    public const string AppleMDFW = "Apple_MDFW";

    /// <summary>
    /// Macintosh File System (MFS) partition.
    /// Introduced with the Macintosh 128K in 1984.
    /// </summary>
    public const string AppleMFS = "Apple_MFS";

    /// <summary>
    /// The partition map itself.
    /// </summary>
    public const string ApplePartitionMap = "Apple_partition_map";

    /// <summary>
    /// Mac OS classic patch partition.
    /// </summary>
    public const string ApplePatches = "Apple_Patches";

    /// <summary>
    /// ProDOS file system partition.
    /// </summary>
    public const string AppleProDOS = "Apple_PRODOS";

    /// <summary>
    /// Mac OS X software RAID partition.
    /// Normally contains HFS/HFS+ or UFS. Requires separate Apple_Boot partition.
    /// </summary>
    public const string AppleRAID = "Apple_RAID";

    /// <summary>
    /// Unix File System (UFS) partition for Apple Rhapsody and Mac OS X Server 1.0-1.2v3.
    /// </summary>
    public const string AppleRhapsodyUFS = "Apple_Rhapsody_UFS";

    /// <summary>
    /// Empty/scratch partition.
    /// </summary>
    public const string AppleScratch = "Apple_Scratch";

    /// <summary>
    /// Second stage bootloader partition.
    /// </summary>
    public const string AppleSecond = "Apple_Second";

    /// <summary>
    /// Unix File System (UFS) partition for Mac OS X and Mac OS X Server 10.0+.
    /// </summary>
    public const string AppleUFS = "Apple_UFS";

    /// <summary>
    /// A/UX and Unix partition (System V Release 2).
    /// Standard partition identifier for many Unix-like operating systems including Linux and NetBSD.
    /// </summary>
    public const string AppleUnixSVR2 = "Apple_UNIX_SVR2";

    /// <summary>
    /// ISO9660 padding partition.
    /// A dummy partition map entry to ensure correct partition alignment on bootable media.
    /// </summary>
    public const string AppleVoid = "Apple_Void";

    /// <summary>
    /// Be File System (BFS) partition, normally used by BeOS.
    /// </summary>
    public const string BeBFS = "Be_BFS";

    /// <summary>
    /// TiVo Media File System partition.
    /// Used for the proprietary Media File System on TiVo hard drives.
    /// </summary>
    public const string MFS = "MFS";
}
