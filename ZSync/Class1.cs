using System.Text;
using System.Text.RegularExpressions;

namespace ZSync;

public class ZSyncState
{
    public int FileLen { get; set; }
    public string Filename { get; set; }
    public string ZFilename { get; set; }
    public string Url { get; set; }
    public string ZUrl { get; set; }
    public long BlockSize { get; set; }
    public int Blocks { get; set; }
    public long MTime { get; set; }
    public string GzHead { get; set; }
    public string GzOpts { get; set; }
    public string Checksum { get; set; }
    public string Safelines { get; set; }

    public ZSyncState()
    {
        FileLen = -1;
        MTime = -1;
    }
}

public partial class Program
{
    private static readonly Regex BlockSizeRegex = GeneratedBlockSizeRegex();
    private static readonly Regex HashLengthsRegex = GeneratedHashLengthsRegex();

    public static async Task<ZSyncState?> Begin(FileInfo file)
    {
        int checksumBytes = 16;
        int rsumBytes = 4;
        int seqMatches = 1;

        string safelines = null;

        var zs = new ZSyncState();
        
        await using var stream = file.OpenRead();
        using var textReader = new StreamReader(stream, Encoding.ASCII);

        if (ReadControlFile(textReader, ref zs))
            return zs;

        // Read until the end of file or a blank line is reached
        while (await textReader.ReadLineAsync() is { } line)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line))
                break;

            var entry = line.Split(":", 2, StringSplitOptions.TrimEntries);

            if (entry.Length == 2)
            {
                var tag = entry[0];
                var value = entry[1];

                switch (tag.ToLower())
                {
                    case "zsync":
                        CheckZSyncVersion(zs, stream, value);
                        break;
                    case "min-version":
                        if (string.Compare(value, nameof(ZSyncState), StringComparison.OrdinalIgnoreCase) > 0)
                            return null;
                        break;
                    case "filelen":
                        zs.FileLen = int.Parse(value);
                        break;
                    case "blocksize":
                        zs.BlockSize = long.Parse(value);
                        break;
                    case "mtime":
                        zs.MTime = long.Parse(value);
                        break;
                    case "gzhead":
                        zs.GzHead = value;
                        break;
                    case "gzopts":
                        if (value.Length > 0)
                            zs.GzOpts = value;
                        break;
                    case "checksum":
                        zs.Checksum = value;
                        break;
                    case "safelines":
                        safelines = value;
                        break;
                }
            }

            // Hash-lengths, Blocksize and Length lines are required
            if (zs.FileLen != -1 && zs.BlockSize != -1)
                zs.Blocks = (int)((zs.FileLen + zs.BlockSize - 1) / zs.BlockSize);

            if (safelines == null || !safelines.Contains(tag))
            {
                Console.WriteLine($"Unrecognised tag {tag}.");
                return null;
            }
        }

        if (zs.FileLen != -1 && zs.BlockSize != -1)
        {
            var blockSumResult = ReadBlockSums(stream, ref zs);

            if (blockSumResult != 0)
            {
                Console.WriteLine("Not a zsync file.");
                return null;
            }
        }

        return zs;
    }

    private static bool CheckZSyncVersion(ZSyncState zs, Stream stream, string value)
    {
        Console.WriteLine($"Warning: Version mismatch for ZSync ({value}).");
        return true;
    }

    private static int ReadBlockSums(Stream stream, ref ZSyncState zs)
    {
        // Define constants for checksum size and rsum bytes
        const int CHECKSUM_SIZE = 16; // Assuming 128-bit checksums
        const int RSUM_BYTES = 4;

        try
        {
            // Create an instance of the checksum state
            var rs = new RcksumState(zs.Blocks, zs.BlockSize, RSUM_BYTES);

            int seqMatches = 1;

            // Loop over each block
            for (var id = 0; id < zs.Blocks; id++)
            {
                var r = new RSum(0, 0);
                byte[] checksum = new byte[CHECKSUM_SIZE];

                // Read the checksum data
                if (stream.Read(r.Data, r.Data.Length - RSUM_BYTES, 1) != 1 ||
                        stream.Read(checksum, 0, CHECKSUM_SIZE) != 1)
                {
                    Console.WriteLine("Short read on control file.");
                    return -1;
                }

                // Convert the checksum data to host byte order
                r.ConvertToHostEndian();

                // Add the target block to the checksum state
                rs.AddTargetBlock(id, r, checksum);
            }

            return 0; // Success
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error reading block sums: " + ex.Message);
            return -1;
        }
    }

    private static bool ReadControlFile(StreamReader streamReader, ref ZSyncState zs)
    {
        var match = BlockSizeRegex.Match(streamReader.ReadToEnd());

        if (match.Success)
            zs.BlockSize = long.Parse(match.Groups[1].Value);

        return true;
    }

    [GeneratedRegex(@"Blocksize:(\d+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex GeneratedBlockSizeRegex();
    [GeneratedRegex(@"Hash-Lengths:(\d+),(\d+),(\d+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex GeneratedHashLengthsRegex();
}