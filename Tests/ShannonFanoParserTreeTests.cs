using CompressionAlgorithms.ShannonFano;
using FluentAssertions;

namespace Tests;

public class ShannonFanoParserTreeTests
{
    [Fact]
    public void Construct_header()
    {
        // Assign
        var frequencies = new Dictionary<byte[], int>()
        {
            [[0, 1, 1, 0, 0, 0, 1, 0]] = 5,
            [[0, 1, 1, 0, 0, 0, 0, 1]] = 11,
            [[0, 1, 1, 0, 0, 0, 1, 1]] = 2,
        };

        var codes = ShannonFanoUtils.ConstructCodes(frequencies);
        var tree = new ShannonFanoParserNode(codes);

        // Act
        var header = tree.ConstructTreeHeader();

        // Assert
        header.Should().Equal(0, 1, 0, 1, 1, 0, 0, 0, 0, 1, 0, 1, 0, 1, 1, 0, 0, 0, 1, 0, 1, 0, 1, 1, 0, 0, 0, 1, 1);
    }

    [Fact]
    public void Create_tree_from_header()
    {
        // Assign
        var expectedHeader = new byte[] { 0, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0 };
        var stream = new MemoryStream(expectedHeader);

        var tree = new ShannonFanoParserNode(stream, wordLength: 2);

        // Act
        var header = tree.ConstructTreeHeader();
        
        // Assert
        header.Should().Equal(header);
    }
}
