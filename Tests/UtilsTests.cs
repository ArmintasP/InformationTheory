using CompressionAlgorithms;
using FluentAssertions;
using System.Text;

namespace Tests;

public class UtilsTests
{
    [Theory]
    [InlineData(5, "00101")]
    [InlineData(1, "1")]
    [InlineData(100, "0000001100100")]
    public void Encode_number_in_Elias_gamma_code(int number, string expectedEncodedText)
    {
        // Act
        var encodedNumber = number.EliasGammaCode();

        // Assert
        encodedNumber.Should().Be(expectedEncodedText);
    }

    [Theory]
    [InlineData("00101", 5)]
    [InlineData("1", 1)]
    [InlineData("0000001100100", 100)]
    [InlineData("00101someothertext", 5)]
    public void Decode_number_from_Elias_gamma_code(string streamText, int expectedDecodedNumber)
    {
        // Assign
        var stream = new MemoryStream(Encoding.ASCII.GetBytes(streamText));

        // Act
        var decodedNumber = Utils.DecodeEliasGammaCode(stream);

        // Assert
        decodedNumber.Should().Be(expectedDecodedNumber);
    }
}
