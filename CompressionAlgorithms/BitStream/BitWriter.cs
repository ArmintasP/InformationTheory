using System.Collections;

namespace CompressionAlgorithms.BitStream;

public sealed class BitWriter : Stream
{
    public Stream BaseStream => _stream;

    private readonly Stream _stream;

    private readonly bool[] _writeBoolBuffer;
    private readonly byte[] _writeByteBuffer;
    private int _writeBoolBufferPosition = 0;

    public BitWriter(Stream stream, int bitBufferSize = 8 * 1024 * 16)
    {
        _stream = stream;

        _writeBoolBuffer = new bool[bitBufferSize];
        _writeByteBuffer = new byte[bitBufferSize / 8];
    }
    
    public override bool CanWrite => true;

    /// <summary>
    /// Groups buffer bits of 8 to bytes. Buffer must only contain char values of '1' or '0'.
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        var bitsAsBools = buffer
            .Skip(offset)
            .Take(count)
            .Select(bit => bit == (byte)'1')
            .ToArray();

        while (bitsAsBools.Length - (_writeBoolBuffer.Length - _writeBoolBufferPosition) > 0)
        {
            var remainingSpaceInWriteBuffer = _writeBoolBuffer.Length - _writeBoolBufferPosition;

            bitsAsBools[0..remainingSpaceInWriteBuffer].CopyTo(_writeBoolBuffer, _writeBoolBufferPosition);
            bitsAsBools = bitsAsBools[remainingSpaceInWriteBuffer..];

            _writeBoolBufferPosition += remainingSpaceInWriteBuffer;
            WriteToUnderlyingStream();
        }

        bitsAsBools.CopyTo(_writeBoolBuffer, _writeBoolBufferPosition);
        _writeBoolBufferPosition += bitsAsBools.Length;
    }

    /// <summary>
    /// It's impossible to save 1/2/3/4/5/6/7 bits in a file. So we fill the remaining bits with 0 values.
    /// Returns how many bits were filled.
    /// </summary>
    public int FillRemainingBitsToFormAByte()
    {
        var filledBitsCount = _writeBoolBufferPosition % 8;
        if (filledBitsCount is 0)
            return 0;

        var bytesToWriteAsBits = Enumerable.Repeat<byte>(0, 8 - filledBitsCount).ToArray();
        Write(bytesToWriteAsBits);

        return 8 - filledBitsCount;
    }

    private void WriteToUnderlyingStream()
    {
        if (_writeBoolBufferPosition % 8 != 0)
            throw new InvalidOperationException("Amount of bits written should be divisible by 8.");

        var valuesToWrite = _writeBoolBuffer[0.._writeBoolBufferPosition];
        new BitArray(valuesToWrite).CopyTo(_writeByteBuffer, 0);

        _stream.Write(_writeByteBuffer, 0, _writeBoolBufferPosition / 8);
        _writeBoolBufferPosition = 0;
    }

    public override void Flush()
    {
        WriteToUnderlyingStream();
        _stream.Flush();
    }

    public long SeekToBeginning()
    {
        Flush();
        return _stream.Seek(0, SeekOrigin.Begin);
    }

    protected override void Dispose(bool disposing)
    {
        Flush();
        if (disposing)
            _stream.Dispose();
    }
    
    public override bool CanRead => throw new NotImplementedException();

    public override bool CanSeek => throw new NotImplementedException();

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }
}