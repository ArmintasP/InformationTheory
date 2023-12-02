using CompressionAlgorithms;
using FluentAssertions;
using System.Text;

namespace Tests;

public class UtilsTests
{
    [Theory]
    [InlineData(4, "00101")]
    [InlineData(0, "1")]
    [InlineData(1, "010")]
    [InlineData(99, "0000001100100")]
    public void Encode_number_in_Elias_gamma_code(int number, string expectedEncodedText)
    {
        // Act
        var encodedNumber = number.EliasGammaCode();

        // Assert
        encodedNumber.Should().Be(expectedEncodedText);
    }

    [Theory]
    [InlineData("00101", 4)]
    [InlineData("1", 0)]
    [InlineData("010", 1)]
    [InlineData("0000001100100", 99)]
    [InlineData("00101someothertext", 4)]
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
