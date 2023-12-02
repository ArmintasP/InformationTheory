using System.Collections;

namespace CompressionAlgorithms.BitStream;

public sealed class BitWriter : Stream
{
    public Stream BaseStream => _stream;

    private readonly Stream _stream;

    private readonly byte[] _writeBitsBuffer;
    private readonly byte[] _writeByteBuffer;
    private int _writeBitsBufferPosition = 0;

    public BitWriter(Stream stream, int bitBufferSize = 8 * 1024 * 16)
    {
        _stream = stream;

        _writeBitsBuffer = new byte[bitBufferSize];
        _writeByteBuffer = new byte[bitBufferSize / 8];
    }

    public override bool CanWrite => true;

    /// <summary>
    /// Groups buffer bits of 8 to bytes. Buffer must only contain char values of '1' or '0'.
    /// </summary>
    public override void Write(byte[] buffer, int offset, int count)
    {
        var bits = buffer[offset..(offset + count)];

        while (bits.Length - (_writeBitsBuffer.Length - _writeBitsBufferPosition) > 0)
        {
            var remainingSpaceInWriteBuffer = _writeBitsBuffer.Length - _writeBitsBufferPosition;

            bits[0..remainingSpaceInWriteBuffer].CopyTo(_writeBitsBuffer, _writeBitsBufferPosition);
            bits = bits[remainingSpaceInWriteBuffer..];

            _writeBitsBufferPosition += remainingSpaceInWriteBuffer;
            WriteToUnderlyingStream();
        }

        bits.CopyTo(_writeBitsBuffer, _writeBitsBufferPosition);
        _writeBitsBufferPosition += bits.Length;
    }

    /// <summary>
    /// It's impossible to save 1/2/3/4/5/6/7 bits in a file. So we fill the remaining bits with 0 values.
    /// Returns how many bits were filled.
    /// </summary>
    public int FillRemainingBitsToFormAByte()
    {
        var filledBitsCount = _writeBitsBufferPosition % 8;
        if (filledBitsCount is 0)
            return 0;

        var bytesToWriteAsBits = Enumerable.Repeat<byte>(0, 8 - filledBitsCount).ToArray();
        Write(bytesToWriteAsBits);

        return 8 - filledBitsCount;
    }

    private void WriteToUnderlyingStream()
    {
        if (_writeBitsBufferPosition % 8 != 0)
            throw new InvalidOperationException("Amount of bits written should be divisible by 8.");

        var bitsToWrite = _writeBitsBuffer[0.._writeBitsBufferPosition];

        for (var i = 0; i < bitsToWrite.Length / 8; i++)
        {
            _writeByteBuffer[i] = (byte)
                (
                    bitsToWrite[i * 8 + 0] << 7 |
                    bitsToWrite[i * 8 + 1] << 6 |
                    bitsToWrite[i * 8 + 2] << 5 |
                    bitsToWrite[i * 8 + 3] << 4 |
                    bitsToWrite[i * 8 + 4] << 3 |
                    bitsToWrite[i * 8 + 5] << 2 |
                    bitsToWrite[i * 8 + 6] << 1 |
                    bitsToWrite[i * 8 + 7] << 0
                );
        }

        _stream.Write(_writeByteBuffer, 0, bitsToWrite.Length / 8);
        _writeBitsBufferPosition = 0;
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