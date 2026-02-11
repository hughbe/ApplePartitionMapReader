using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using ApplePartitionMapReader;
using ApplePartitionMapReader.Utilities;

public sealed class Program
{
    public static int Main(string[] args)
    {
        var app = new CommandApp<DumpCommand>();
        app.Configure(config =>
        {
            config.SetApplicationName("apm-dumper");
            config.ValidateExamples();
            config.AddCommand<ExtractCommand>("extract");
        });

        return app.Run(args);
    }
}

sealed class ExtractSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    [Description("The input disk image file to read.")]
    public required string Input { get; init; }

    [CommandOption("--offset")]
    [Description("The byte offset within the input file where the partition map starts (default: 0).")]
    public int VolumeOffset { get; init; } = 0;

    [CommandOption("--output")]
    [Description("Output directory to write extracted partitions to (default: current directory).")]
    public string OutputDirectory { get; init; } = ".";

    [CommandOption("--index")]
    [Description("If specified, extract only the partition at this index (zero-based).")]
    public int? Index { get; init; }
}

sealed class ExtractCommand : Command<ExtractSettings>
{
    public override int Execute(CommandContext context, ExtractSettings settings, CancellationToken cancellationToken)
    {
        var input = new FileInfo(settings.Input);
        if (!input.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Input file not found[/]: {input.FullName}");
            return -1;
        }

        Directory.CreateDirectory(settings.OutputDirectory);

        using var stream = input.OpenRead();

        if (!ApplePartitionMap.IsApplePartitionMap(stream, settings.VolumeOffset))
        {
            AnsiConsole.MarkupLine($"[red]Not a valid Apple Partition Map[/] at offset {settings.VolumeOffset}");
            return -1;
        }

        var map = new ApplePartitionMap(stream, settings.VolumeOffset);

        int partitionIndex = 0;
        foreach (var p in map)
        {
            if (settings.Index.HasValue && settings.Index.Value != partitionIndex)
            {
                partitionIndex++;
                continue;
            }

            var partName = p.Name.ToString();
            foreach (var c in Path.GetInvalidFileNameChars()) partName = partName.Replace(c, '_');
            if (string.IsNullOrWhiteSpace(partName)) partName = "partition" + partitionIndex;

            var destFile = Path.Combine(settings.OutputDirectory, $"{partitionIndex:00}-{partName}.img");

            long partOffset = (long)settings.VolumeOffset + (long)p.PartitionStartBlock * 512L;
            long partLength = (long)p.PartitionBlockCount * 512L;

            AnsiConsole.MarkupLine($"[green]Extracting[/] partition {partitionIndex} -> {destFile} ({partLength} bytes)");

            stream.Seek(partOffset, SeekOrigin.Begin);
            using var outStream = new FileStream(destFile, FileMode.Create, FileAccess.Write);

            byte[] buffer = new byte[81920];
            long remaining = partLength;
            while (remaining > 0)
            {
                int toRead = (int)Math.Min(buffer.Length, remaining);
                int read = stream.Read(buffer, 0, toRead);
                if (read == 0) break;
                outStream.Write(buffer, 0, read);
                remaining -= read;
            }

            AnsiConsole.MarkupLine($"[blue]Wrote[/] {destFile}");

            if (settings.Index.HasValue)
            {
                return 0;
            }

            partitionIndex++;
        }

        return 0;
    }
}

sealed class DumpSettings : CommandSettings
{
    [CommandArgument(0, "<input>")]
    [Description("The input disk image file to read.")]
    public required string Input { get; init; }

    [CommandOption("--offset")]
    [Description("The byte offset within the input file where the partition map starts (default: 0).")]
    public int VolumeOffset { get; init; } = 0;
}

sealed class DumpCommand : Command<DumpSettings>
{
    public override int Execute(CommandContext context, DumpSettings settings, CancellationToken cancellationToken)
    {
        var input = new FileInfo(settings.Input);
        if (!input.Exists)
        {
            AnsiConsole.MarkupLine($"[red]Input file not found[/]: {input.FullName}");
            return -1;
        }

        using var stream = input.OpenRead();

        if (!ApplePartitionMap.IsApplePartitionMap(stream, settings.VolumeOffset))
        {
            AnsiConsole.MarkupLine($"[red]Not a valid Apple Partition Map[/] at offset {settings.VolumeOffset}");
            return -1;
        }

        var map = new ApplePartitionMap(stream, settings.VolumeOffset);

        AnsiConsole.MarkupLine($"[green]Apple Partition Map[/]: {input.Name}");
        AnsiConsole.MarkupLine($"[blue]Total partitions[/]: {map.Count}");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("#");
        table.AddColumn("Name");
        table.AddColumn("Type");
        table.AddColumn("Start Block");
        table.AddColumn("Block Count");
        table.AddColumn("Size");
        table.AddColumn("Status");

        int partitionIndex = 0;
        foreach (var p in map)
        {
            var sizeBytes = (long)p.PartitionBlockCount * 512;
            var sizeStr = FormatSize(sizeBytes);

            table.AddRow(
                partitionIndex.ToString(),
                p.Name.ToString(),
                p.Type.ToString(),
                p.PartitionStartBlock.ToString(),
                p.PartitionBlockCount.ToString(),
                sizeStr,
                p.Status.ToString()
            );
            partitionIndex++;
        }

        AnsiConsole.Write(table);

        var dd = map.DriverDescriptorMap;
        if (dd is not null)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[yellow]Driver Descriptor Map[/]");
            AnsiConsole.MarkupLine($"[blue]Block size[/]: {dd.Value.BlockSize}");
            AnsiConsole.MarkupLine($"[blue]Block count[/]: {dd.Value.BlockCount}");
            AnsiConsole.MarkupLine($"[blue]Driver entries[/]: {dd.Value.DriverCount}");

            var entries = dd.Value.Entries.AsSpan();
            for (int i = 0; i < dd.Value.DriverCount; i++)
            {
                var e = entries[i];
                AnsiConsole.MarkupLine($"Driver {i}: Start={e.StartBlock}, Blocks={e.BlockCount}, Type={e.Type}");
            }
        }

        return 0;
    }

    private static string FormatSize(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }

        return $"{size:0.##} {suffixes[suffixIndex]}";
    }
}

