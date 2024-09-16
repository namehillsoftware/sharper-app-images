namespace SharperAppImages.Extraction;

public class OffsetStream(Stream streamImplementation) : Stream
{
    private readonly int initialOffset = (int)streamImplementation.Position;
    
    public override void Flush()
    {
        streamImplementation.Flush();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return streamImplementation.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return streamImplementation.Seek(offset + initialOffset, origin);
    }

    public override void SetLength(long value)
    {
        streamImplementation.SetLength(value + initialOffset);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        streamImplementation.Write(buffer, offset + initialOffset, count);
    }

    public override bool CanRead => streamImplementation.CanRead;

    public override bool CanSeek => streamImplementation.CanSeek;

    public override bool CanWrite => streamImplementation.CanWrite;

    public override long Length => streamImplementation.Length - initialOffset;

    public override long Position
    {
        get => streamImplementation.Position - initialOffset;
        set => streamImplementation.Position = value + initialOffset;
    }
}