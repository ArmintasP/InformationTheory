using System.Collections;

namespace CompressionAlgorithms;

public sealed class BitStream : Stream
{
    private readonly Stream _stream;

    private int _bitBufferPosition = 0;
    private int _bitBufferFilledLength = 0;
    private readonly byte[] _bitBuffer;
    private readonly byte[] _byteBuffer;

    private readonly bool[] _writeBoolBuffer;
    private readonly byte[] _writeByteBuffer;
    private int _writeBoolBufferPosition = 0;

    public BitStream(Stream stream, int bitBufferSize = 65_536)
    {
        _stream = stream;

        _bitBuffer = new byte[bitBufferSize];
        _byteBuffer = new byte[bitBufferSize / 8];

        _writeBoolBuffer = new bool[bitBufferSize];
        _writeByteBuffer = new byte[bitBufferSize / 8];
    }

    public override bool CanRead => true;

    public override bool CanWrite => true;

    /// <summary>
    /// Reads from underlying stream. Returned buffer contains only '0' or '1' char values converted to bytes.
    /// </summary>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_bitBufferPosition >= _bitBufferFilledLength)
            FillBitBuffer();

        var indexOfLastReadBit = _bitBufferPosition + count > _bitBufferFilledLength
            ? _bitBufferFilledLength
            : _bitBufferPosition + count;

        var numberOfReadBits = indexOfLastReadBit - _bitBufferPosition;

        _bitBuffer[_bitBufferPosition..indexOfLastReadBit].CopyTo(buffer, offset);
        _bitBufferPosition = indexOfLastReadBit;

        return numberOfReadBits;
    }

    private void FillBitBuffer()
    {
        var bytesReadCount = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
        var bitArray = new BitArray(_byteBuffer[0..bytesReadCount]);

        _bitBufferPosition = 0;
        _bitBufferFilledLength = 0;

        foreach (bool bit in bitArray)
        {
            _bitBuffer[_bitBufferFilledLength] = bit ? (byte)'1' : (byte)'0';
            _bitBufferFilledLength++;
        }
    }

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

    protected override void Dispose(bool disposing)
    {
        Flush();

        if (disposing)
        {
            _stream.Dispose();
        }
    }

    public override bool CanSeek => throw new NotImplementedException();

    public override long Length => throw new NotImplementedException();

    public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }
}
