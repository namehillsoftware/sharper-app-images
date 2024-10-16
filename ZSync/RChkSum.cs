namespace ZSync;

public class RcksumState
{
    public zs_blockid Blocks { get; set; }
    public nuint Blocksize { get; set; }
    public int RsumBytes { get; set; }
    public uint ChecksumBytes { get; set; }
    public bool RequireConsecutiveMatches { get; set; }
    public int Context { get; set; }
    public string Filename { get; set; }
    public int GotBlocks { get; set; }
    public RcksumStats Stats { get; set; }
    public int? FileDescriptor { get; set; }
    public uint[] Blockhashes { get; set; }

    public RcksumState(
        zs_blockid nblocks, 
        nuint blocksize,
        int rsumBytes,
        uint checksumBytes, 
        bool requireConsecutiveMatches = false)
    {
        Blocks = nblocks;
        Blocksize = blocksize;
        RsumBytes = rsumBytes;
        ChecksumBytes = checksumBytes;
        RequireConsecutiveMatches = requireConsecutiveMatches;

        Context = Blocksize * (RequireConsecutiveMatches ? 1 : 0);

        Filename = "rcksum-XXXXXX";

        GotBlocks = 0;
        Stats = new RcksumStats();

        if ((Blocksize & (Blocksize - 1)) != 0 || string.IsNullOrEmpty(Filename) || Blocks <= 0) return;
        FileDescriptor = Path.GetTempFileName("rcksum-", ".tmp");

        for (int i = 0; i < 32; i++)
            if ((uint)Blocksize == (1u << i))
                Blocksize = i;

        int length = Blocks + RequireConsecutiveMatches;
        Blockhashes = new uint[length];
    }
}

public class RcksumStats
{
    public int Gotblocks { get; set; }
}

public class RSum
{
    public void AddTargetBlock(int blockId, RSum rSum, byte[] checksum)
    {
        if (blockId < Blocks)
        {
            var blockHashes = BlockHashes[blockId];
        
            // Enter checksums
            Array.Copy(checksum, 0, blockHashes.Checksum, 0, checksum.Length);
            blockHashes.R = new RSum { A = rSum.A & RSumAMask };
            blockHashes.R.B = rSum.B;

            if (RSumHash != null)
            {
                RSumHash = null;
                BitHash = null;
            }
        }
    }
    
    public void print_hashstats(RcksumState z)
    {
        int i;
        {
            int num_bits_set = 0;
            for (i = 0; i <= z.Bithashmask; i++)
            {
                num_bits_set += (z.Bithash[i] & 1) != 0 ? 1 : 0;
            }
            Console.WriteLine($"Hash load factor: {num_bits_set} / {(z.Bithashmask + 1)}");
        }

        int hash_load_factor = 0, bithash_load_factor = 0;
        for (i = 0; i <= z.Hashmask; i++)
        {
            if ((z.Rsum_hash[i] != null) && (z.Rsum_hash[i].next != null))
                hash_load_factor++;
            if (((z.Bithash[i >> 3] & (1 << (i % 8))) != 0)
                bithash_load_factor++;
        }
        Console.WriteLine($"Hash load factor: {hash_load_factor} / {(z.Hashmask + 1)}, Bithash load factor: {bithash_load_factor}");
    }

    public bool build_hash_table(RcksumState z)
    {
        int hash_bits = (int)Math.Log(z.Blocks + 1, 2);
        while ((1U << (hash_bits - 1)) > z.Blocks && hash_bits > 5) hash_bits--;
        z.Hashmask = (1U << hash_bits) - 1;

        z.Rsum_hash = new HashEntry[z.Hashmask + 1];
        for (int i = 0; i <= z.Hashmask; i++)
            z.Rsum_hash[i] = null;
        if (z.Rsum_hash == null)
        {
            return false;
        }

        int bithash_bits = Math.Min(hash_bits + BITHASHBITS, z.AvailableBits);
        z.Bithashmask = (1U << bithash_bits) - 1;
        z.Bithash = new byte[z.Bithashmask + 1];
        for (int i = 0; i <= z.Bithashmask; i++)
            z.Bithash[i] = 0;

        if (z.Seq_matches > 1 && z.AvailableBits < 24)
        {
            z.Hash_func_shift = Math.Max(0, hash_bits - (z.AvailableBits / 2));
        }
        else
        {
            z.Hash_func_shift = Math.Max(0, hash_bits - (z.AvailableBits - 16));
        }

        for (int id = z.Blocks; id > 0;)
        {
            int h = calc_rhash(z, &(z.Blockhashes[--id]));
            struct hash_entry *e = &(z.Rsum_hash[h & z.Hashmask]);
            if (e == null) continue;
            e->next = z.Rsum_hash[h & z.Hashmask];
            z.Rsum_hash[h & z.Hashmask] = e;

            z.Bithash[(h & z.Bithashmask) >> 3] |= 1 << (h % 8);
        }

        print_hashstats(z);
        return true;
    }
}