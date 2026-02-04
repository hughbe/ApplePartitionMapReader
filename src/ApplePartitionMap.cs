using System.Buffers.Binary;
using System.Diagnostics;
using ApplePartitionMapReader.Utilities;

namespace ApplePartitionMapReader;

/// <summary>
/// Represents a disk containing an Apple Partition Map.
/// </summary>
public readonly struct ApplePartitionMap
{
    private readonly Stream _stream;
    private readonly int _streamStartOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplePartitionMap"/> struct.
    /// </summary>
    /// <param name="stream">The stream containing the HFS volume data.</param>
    /// <param name="volumeStartOffset">The start offset of the volume within the stream.</param>
    public ApplePartitionMap(Stream stream, int volumeStartOffset)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        _stream = stream;
        _streamStartOffset = volumeStartOffset;

        if (!IsApplePartitionMap(_stream, _streamStartOffset))
        {
            throw new InvalidDataException("The stream does not contain a valid Apple Partition Map at the specified offset.");
        }
    }

    /// <summary>
    /// Gets the number of partition map entries.
    /// </summary>
    public int Count => (int)this[0].MapEntryCount;

    /// <summary>
    /// Gets an enumerator for the partition map entries.
    /// This method enables foreach support without allocations.
    /// </summary>
    /// <returns>An enumerator for the partition map entries.</returns>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerates the partition map entries without allocating.
    /// </summary>
    public struct Enumerator
    {
        private readonly ApplePartitionMap _map;
        private readonly int _count;
        private int _index;

        internal Enumerator(ApplePartitionMap map)
        {
            _map = map;
            _count = map.Count;
            _index = -1;
        }

        /// <summary>
        /// Gets the current partition map entry.
        /// </summary>
        public ApplePartitionMapEntry Current => _map[_index];

        /// <summary>
        /// Advances the enumerator to the next entry.
        /// </summary>
        /// <returns>true if the enumerator was successfully advanced; false if the enumerator has passed the end.</returns>
        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }
    }

    /// <summary>
    /// Gets the partition map entry at the specified index.
    /// </summary>
    /// <param name="index">The index of the partition map entry.</param>
    /// <returns>The partition map entry at the specified index.</returns>
    public ApplePartitionMapEntry this[int index]
    {
        get
        {
            Span<byte> blockBuffer = stackalloc byte[512];
            _stream.Seek(_streamStartOffset + 512 + (index * 512), SeekOrigin.Begin);

            if (_stream.Read(blockBuffer) != blockBuffer.Length)
            {
                throw new InvalidDataException($"Unable to read Apple Partition Map Entry {index}.");
            }

            return new ApplePartitionMapEntry(blockBuffer[..ApplePartitionMapEntry.Size]);
        }
    }

    /// <summary>
    /// Determines whether the specified stream contains an Apple Partition Map at the given volume start offset.
    /// </summary>
    /// <param name="stream">The stream to check.</param>
    /// <param name="volumeStartOffset">The start offset of the volume within the stream.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown if the stream is not seekable or readable.</exception>
    public static bool IsApplePartitionMap(Stream stream, int volumeStartOffset)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek || !stream.CanRead)
        {
            throw new ArgumentException("Stream must be seekable and readable.", nameof(stream));
        }

        var position = stream.Position;
        try
        {
            stream.Seek(volumeStartOffset + 512, SeekOrigin.Begin);

            Span<byte> bufffer = stackalloc byte[2];
            if (stream.Read(bufffer) != bufffer.Length)
            {
                return false;
            }

            var signature = BinaryPrimitives.ReadUInt16BigEndian(bufffer);
            return signature == ApplePartitionMapEntry.BlockSignature;
        }
        finally
        {
            stream.Seek(position, SeekOrigin.Begin);
        }
    }

    /// <summary>
    /// Reads the Driver Descriptor Map at the start of the volume (block 0).
    /// Returns null if the block is blank (signature == 0x0000).
    /// </summary>
    public DriverDescriptorMap? DriverDescriptorMap
    {
        get
        {
            TryGetDriverDescriptorMap(out var ddm);
            return ddm;
        }
    }

    /// <summary>
    /// Attempts to read the Driver Descriptor Map at the start of the volume (block 0).
    /// This method avoids allocations by using an out parameter instead of returning a nullable struct.
    /// </summary>
    /// <param name="driverDescriptorMap">When this method returns, contains the Driver Descriptor Map if present; otherwise, the default value.</param>
    /// <returns>true if the Driver Descriptor Map was successfully read; false if the block is blank (signature == 0x0000).</returns>
    /// <exception cref="InvalidDataException">Thrown when the Driver Descriptor Map block cannot be read.</exception>
    public bool TryGetDriverDescriptorMap(out DriverDescriptorMap driverDescriptorMap)
    {
        Span<byte> blockBuffer = stackalloc byte[Utilities.DriverDescriptorMap.Size];
        var position = _stream.Position;
        try
        {
            _stream.Seek(_streamStartOffset, SeekOrigin.Begin);

            if (_stream.Read(blockBuffer) != blockBuffer.Length)
            {
                throw new InvalidDataException("Unable to read Driver Descriptor Map block.");
            }

            var sig = BinaryPrimitives.ReadUInt16BigEndian(blockBuffer);
            if (sig == 0x0000)
            {
                driverDescriptorMap = default;
                return false;
            }

            driverDescriptorMap = new DriverDescriptorMap(blockBuffer);
            return true;
        }
        finally
        {
            _stream.Seek(position, SeekOrigin.Begin);
        }
    }
}
