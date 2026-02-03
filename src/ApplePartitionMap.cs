using System.Buffers.Binary;
using System.Diagnostics;

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
    /// Gets the collection of partition map entries.
    /// </summary>
    public IEnumerable<ApplePartitionMapEntry> Entries
    {
        get
        {
            var firstEntry = this[0];
            for (int i = 0; i < firstEntry.MapEntryCount; i++)
            {
                yield return this[i];
            }
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
}
