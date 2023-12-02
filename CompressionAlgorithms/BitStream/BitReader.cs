namespace CompressionAlgorithms.BitStream;

using System.Collections;

public sealed class BitReader : Stream
{
    private readonly Stream _stream;
    private int _ignoreNLastBits = 0;

    private int _readBitBufferPosition = 0;
    private int _readBitBufferFilledLength = 0;
    private readonly byte[] _readBitBuffer;
    private readonly byte[] _readByteBuffer;

    public BitReader(Stream stream, int bitBufferSize = 8 * 1024 * 8)
    {
        _stream = stream;

        _readBitBuffer = new byte[bitBufferSize];
        _readByteBuffer = new byte[bitBufferSize / 8];
    }
    
    public override long Length => _stream.Length * 8;

    public override bool CanRead => true;

    /// <summary>
    /// Reads from underlying stream. Returned buffer contains only '0' or '1' char values converted to bytes.
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_readBitBufferPosition >= _readBitBufferFilledLength)
            ReadToBitBuffer();

        var indexOfLastReadBit = _readBitBufferPosition + count > _readBitBufferFilledLength
            ? _readBitBufferFilledLength
            : _readBitBufferPosition + count;

        var numberOfReadBits = indexOfLastReadBit - _readBitBufferPosition;

        _readBitBuffer[_readBitBufferPosition..indexOfLastReadBit].CopyTo(buffer, offset);
        _readBitBufferPosition = indexOfLastReadBit;

        return numberOfReadBits;
    }

    private void ReadToBitBuffer()
    {
        var bytesReadCount = _stream.Read(_readByteBuffer, 0, _readByteBuffer.Length);
        var bitArray = new BitArray(_readByteBuffer[0..bytesReadCount]);

        _readBitBufferPosition = 0;
        _readBitBufferFilledLength = 0;

        foreach (bool bit in bitArray)
        {
            _readBitBuffer[_readBitBufferFilledLength] = bit ? (byte)'1' : (byte)'0';
            _readBitBufferFilledLength++;
        }

        IgnoreNLastBitsWhenEOF();
    }

    public void IgnoreNLastBitsWhenEOF(int amountOfBitsToIgnoreWhenEOF)
    {
        _ignoreNLastBits = amountOfBitsToIgnoreWhenEOF;
        IgnoreNLastBitsWhenEOF();
    }

    private void IgnoreNLastBitsWhenEOF()
    {
        var peekByte = _stream.ReadByte();

        if (peekByte is not -1)
        {
            _stream.Seek(-1, SeekOrigin.Current);
            return;
        }
        if (_readBitBufferFilledLength > 0 && peekByte is -1)
            _readBitBufferFilledLength -= _ignoreNLastBits;

        return;
    }

    public bool IsEOF()
    {
        if (_readBitBufferPosition >= _readBitBufferFilledLength)
            ReadToBitBuffer();

        return _readBitBufferPosition >= _readBitBufferFilledLength;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _stream.Dispose();
    }

    public override bool CanSeek => throw new NotImplementedException();

    public override bool CanWrite => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override void Flush()
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

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}
