using CompressionAlgorithms;
using CompressionAlgorithms.BitStream;
using FluentAssertions;
using System.Collections;
using System.Text;

namespace Tests;

public class BitStreamTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    public async Task Read_bytes(int bufferSize)
    {
        // Assign
        var bytes = new byte[] { 6, 1, 4, 4, 3, 2 };
        var bits = new byte[bytes.Length * 8];

        await using var memoryStream = new MemoryStream(bytes);
        await using var bitStream = new BitReader(memoryStream, bufferSize);

        // Act
        await bitStream.ReadExactlyAsync(bits, offset: 0, bits.Length);

        // Assert
        var bitsAsBools = bits.Select(b => b == (byte)'1').ToArray();
        var bitsConvertedToBytes = new byte[bytes.Length];
        new BitArray(bitsAsBools).CopyTo(bitsConvertedToBytes, 0);

        bitsConvertedToBytes.Should().BeEquivalentTo(bytes);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    public async Task Write_bytes(int bufferSize)
    {
        // Assign
        var bytes = new byte[] { 6, 1, 4, 4, 3, 2 };
        var bits = new byte[bytes.Length * 8];

        var bitArray = new BitArray(bytes);
        for (var i = 0; i < bitArray.Length; i++)
            bits[i] = bitArray[i] ? (byte)'1' : (byte)'0';

        await using var memoryStream = new MemoryStream(capacity: bytes.Length);
        await using var bitStream = new BitWriter(memoryStream, bufferSize);

        // Act
        await bitStream.WriteAsync(bits);
        await bitStream.FlushAsync();

        // Assert
        memoryStream.Seek(0, SeekOrigin.Begin);
        var writtenBytes = new byte[bytes.Length];

        await memoryStream.ReadExactlyAsync(writtenBytes, offset: 0, bytes.Length);

        writtenBytes.Should().BeEquivalentTo(bytes);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    public async Task Write_byte(int bufferSize)
    {
        // Assign

        // Letter 'a' represented in binary numbers as string type.
        var word = "10000110";
        // Each byte represents '1' or '0'. In other words, a byte represents a bit.
        var bits = Encoding.ASCII.GetBytes(word);

        await using var memoryStream = new MemoryStream(capacity: 1);
        await using var bitStream = new BitWriter(memoryStream, bufferSize);

        // Act
        await bitStream.WriteAsync(bits);
        await bitStream.FlushAsync();

        // Assert
        memoryStream.Seek(0, SeekOrigin.Begin);
        var writtenBytes = new byte[1];

        await memoryStream.ReadExactlyAsync(writtenBytes, offset: 0, 1);

        writtenBytes.Should().BeEquivalentTo("a"u8.ToArray());
    }
}