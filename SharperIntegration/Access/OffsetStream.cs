namespace SharperIntegration.Access;

public class OffsetStream(Stream streamImplementation) : Stream
{
    private readonly long _initialOffset = streamImplementation.Position;

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
        return streamImplementation.Seek(offset + _initialOffset, origin);
    }

    public override void SetLength(long value)
    {
        streamImplementation.SetLength(value + _initialOffset);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        streamImplementation.Write(buffer, offset, count);
    }

    public override bool CanRead => streamImplementation.CanRead;

    public override bool CanSeek => streamImplementation.CanSeek;

    public override bool CanWrite => streamImplementation.CanWrite;

    public override long Length => streamImplementation.Length - _initialOffset;

    public override long Position
    {
        get => streamImplementation.Position - _initialOffset;
        set => streamImplementation.Position = value + _initialOffset;
    }
}
