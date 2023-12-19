using CompressionAlgorithms.BitStream;

namespace CompressionAlgorithms;

public static class Utils
{
    public static void CalculateFrequencies(this byte[] text, int wordLength, Dictionary<byte[], int> frequencies)
    {
        for (var i = 0; i < text.Length; i += wordLength)
        {
            var word = text[i..(i + wordLength)];

            frequencies[word] = frequencies.TryGetValue(word, out var frequency)
                ? frequency + 1
                : 1;
        }
    }

    public static byte[] EliasGammaCode(this int number)
    {
        // We want to decode zero, so we have to increment by one.
        number += 1;

        var zerosCount = (int)Math.Log(number, newBase: 2);
        var binaryNumber = number.ToBase2();

        return Enumerable
            .Repeat((byte)0, zerosCount)
            .Concat(binaryNumber)
            .ToArray();
    }

    private static string ToBase2AsString(this int number, int padding = 0)
    {
        var numberAsBinaryString = Convert.ToString(number, toBase: 2);
        return numberAsBinaryString.PadLeft(padding, '0');
    }

    public static byte[] ToBase2(this int number, int padding = 0)
    {
        string numberAsBinaryString = number.ToBase2AsString(padding);

        return numberAsBinaryString
            .Select(x => x is '0' ? (byte)0 : (byte)1)
            .ToArray();
    }

    public static int ToInt32(this byte[] numberAsBinary)
    {
        var numberAsBinaryChars = numberAsBinary
            .Select(b => b is 0 ? '0' : '1')
            .ToArray();

        var numberAsBinaryString = new string(numberAsBinaryChars);

        return Convert.ToInt32(numberAsBinaryString, fromBase: 2);
    }

    public static int DecodeEliasGammaCode(Stream stream)
    {
        byte bit;
        var zerosCount = 0;

        while ((bit = (byte)stream.ReadByte()) is not 1)
            zerosCount += 1;

        var buffer = new byte[zerosCount + 1];
        buffer[0] = bit;
        stream.ReadExactly(buffer, offset: 1, count: zerosCount);

        return buffer.ToInt32() - 1;
    }

    public static async Task OverwriteHeaderAsync(byte[] header, int bitsAddedToFormLastByteCount, BitWriter bitWriter)
    {
        var bitsAddedToFormLastByteInBinary = bitsAddedToFormLastByteCount.ToBase2(padding: 3);

        // Only 3 bits are needed for `bitsAddedToFormLastByteInBinary`.
        header[0] = bitsAddedToFormLastByteInBinary[0];
        header[1] = bitsAddedToFormLastByteInBinary[1];
        header[2] = bitsAddedToFormLastByteInBinary[2];

        // Only first byte needs to be overwritten.
        var firstHeaderByte = header[0..8];

        bitWriter.SeekToBeginning();
        await bitWriter.WriteAsync(firstHeaderByte);
    }
}
