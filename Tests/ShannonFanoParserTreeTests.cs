using CompressionAlgorithms.ShannonFano;
using FluentAssertions;

namespace Tests;

public class ShannonFanoParserTreeTests
{
    [Fact]
    public void Construct_header()
    {
        // Assign
        var frequencies = new Dictionary<string, int>()
        {
            ["b"] = 5,
            ["a"] = 11,
            ["c"] = 2,
        };

        var codes = ShannonFanoUtils.ConstructCodes(frequencies);
        var tree = new ShannonFanoParserTree(codes);

        // Act
        var header = tree.ConstructTreeHeader();

        // Assert
        header.Should().Be("01a01b1c");
    }

    [Fact]
    public void Create_tree_from_header()
    {
        // Assign
        var initialHeader = "001b1c1a";
        var tree = new ShannonFanoParserTree(initialHeader, wordLength: 1);

        // Act
        var header = tree.ConstructTreeHeader();

        // Assert
        header.Should().Be("001b1c1a");
    }
}
