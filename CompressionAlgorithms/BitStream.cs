using System.Collections;

namespace CompressionAlgorithms;

public sealed class BitStream : Stream
{
    public Stream BaseStream => _stream;
 
    private readonly Stream _stream;
    private int _ignoreNLastBits = 0;

    private int _readBitBufferPosition = 0;
    private int _readBitBufferFilledLength = 0;
    private readonly byte[] _readBitBuffer;
    private readonly byte[] _readByteBuffer;

    private readonly bool[] _writeBoolBuffer;
    private readonly byte[] _writeByteBuffer;
    private int _writeBoolBufferPosition = 0;

    public BitStream(Stream stream, int bitBufferSize = 8 * 1024 * 16)
    {
        _stream = stream;

        _readBitBuffer = new byte[bitBufferSize];
        _readByteBuffer = new byte[bitBufferSize / 8];

        _writeBoolBuffer = new bool[bitBufferSize];
        _writeByteBuffer = new byte[bitBufferSize / 8];
    }

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

    public bool IsEOF()
    {
        if (_readBitBufferPosition >= _readBitBufferFilledLength)
            ReadToBitBuffer();

        return _readBitBufferPosition >= _readBitBufferFilledLength;
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
        if (_readBitBufferFilledLength > 0 && peekByte is -1 )
            _readBitBufferFilledLength -= _ignoreNLastBits;

        return;
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

    /// <summary>
    /// It's impossible to save 1/2/3/4/5/6/7 bits to a file. So we fill the remaining bits with 0 values.
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

    /// <summary>
    /// Sometimes we want to write to the original stream directly. Should be used only for reading the header.
    /// </summary>
    public void ReadDirectlyFromUnderlyingStreamNotAsBits(Action<Stream> callBack)
    {
        callBack(_stream);
    }

    /// <summary>
    /// Sometimes we want to write bytes and not bits. Should be used only for writing the header.
    /// </summary>
    public void WriteDirectlyToUnderlyingStreamNotAsBits(ReadOnlySpan<byte> buffer)
    {
        _stream.Write(buffer);
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

    public override long Position
    {
        get
        {
            ResetBufferParameters();
            return _stream.Position * 8;
        }

        set
        {
            ResetBufferParameters();
            _stream.Position = value / 8;
        }
    }

    public long SeekToBeginning()
    {
        ResetBufferParameters();
        return _stream.Seek(0, SeekOrigin.Begin);
    }

    public override long Length
    {
        get
        {
            ResetBufferParameters();
            return _stream.Length * 8;
        }
    }

    private void ResetBufferParameters()
    {
        if (_stream.CanWrite)
            Flush();
        else if (_stream.CanRead)
        {
            _readBitBufferPosition = 0;
            _readBitBufferFilledLength = 0;
        }
    }

    public override bool CanRead => true;
    
    public override bool CanWrite => true;

    public override bool CanSeek => false;

    public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

    public override void SetLength(long value) => throw new NotImplementedException();
}
