using CompressionAlgorithms.BitStream;
using FluentAssertions;

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
        bits.Should().Equal(
            0, 0, 0, 0, 0, 1, 1, 0, // 6
            0, 0, 0, 0, 0, 0, 0, 1, // 1
            0, 0, 0, 0, 0, 1, 0, 0, // 4
            0, 0, 0, 0, 0, 1, 0, 0, // 4
            0, 0, 0, 0, 0, 0, 1, 1, // 3
            0, 0, 0, 0, 0, 0, 1, 0  // 2
        );
    }

    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    public async Task Write_bytes(int bufferSize)
    {
        // Assign
        var bits = new byte[]
        {
            0, 0, 0, 0, 0, 1, 1, 0, // 6
            0, 0, 0, 0, 0, 0, 0, 1, // 1
            0, 0, 0, 0, 0, 1, 0, 0, // 4
            0, 0, 0, 0, 0, 1, 0, 0, // 4
            0, 0, 0, 0, 0, 0, 1, 1, // 3
            0, 0, 0, 0, 0, 0, 1, 0  // 2
        };

        await using var memoryStream = new MemoryStream(capacity: bits.Length / 8);
        await using var bitStream = new BitWriter(memoryStream, bufferSize);

        // Act
        await bitStream.WriteAsync(bits);
        await bitStream.FlushAsync();

        // Assert
        memoryStream.Seek(0, SeekOrigin.Begin);
        var writtenBytes = new byte[bits.Length / 8];

        await memoryStream.ReadExactlyAsync(writtenBytes, offset: 0, bits.Length / 8);

        writtenBytes.Should().Equal(6, 1, 4, 4, 3, 2);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(32)]
    [InlineData(64)]
    public async Task Write_byte(int bufferSize)
    {
        // Assign

        // Letter 'a' represented in binary.
        var bits = new byte[] { 0, 1, 1, 0, 0, 0, 0, 1 };

        await using var memoryStream = new MemoryStream(capacity: 1);
        await using var bitStream = new BitWriter(memoryStream, bufferSize);

        // Act
        await bitStream.WriteAsync(bits);
        await bitStream.FlushAsync();

        // Assert
        memoryStream.Seek(0, SeekOrigin.Begin);
        var writtenBytes = new byte[1];

        await memoryStream.ReadExactlyAsync(writtenBytes, offset: 0, 1);
        writtenBytes.Should().Equal("a"u8.ToArray());
    }
}