using System.Text;

namespace CompressionAlgorithms;

public static class Utils
{
    public static void CalculateFrequencies(this string text, int wordLength, Dictionary<string, int> frequencies)
    {
        for (var i = 0; i < text.Length; i += wordLength)
        {
            var word = text[i..(i + wordLength)];

            frequencies[word] = frequencies.TryGetValue(word, out var frequency)
                ? frequency + 1
                : 1;
        }
    }

    public static string EliasGammaCode(this int number)
    {
        // We want to decode zero, so we have to increment by one.
        number = number + 1;
        var zerosCount = (int)Math.Log(number, newBase: 2);
        return new string('0', zerosCount) + number.ToBase2();
    }

    public static string ToBase2(this int number, int padding = 0)
    {
        var numberAsBinaryString = Convert.ToString(number, toBase: 2);
        return numberAsBinaryString.PadLeft(padding, '0');
    }

    public static int ToInt32(this string numberAsBinaryString)
    {
        return Convert.ToInt32(numberAsBinaryString, fromBase: 2);
    }

    public static int DecodeEliasGammaCode(Stream stream)
    {
        byte bit;
        var zerosCount = 0;

        while ((bit = (byte)stream.ReadByte()) is not (byte)'1')
            zerosCount += 1;

        var buffer = new byte[zerosCount + 1];
        buffer[0] = bit;
        stream.ReadExactly(buffer, offset: 1, count: zerosCount);

        var numberAsBinaryString = Encoding.ASCII.GetString(buffer, 0, zerosCount + 1);
        return numberAsBinaryString.ToInt32() - 1;
    }

    public static string ToString2(this byte[] buffer, int offset, int count)
    {
        return Encoding.ASCII.GetString(buffer, offset, count);
    }

    public static string ToString2(this byte[] buffer)
    {
        return Encoding.ASCII.GetString(buffer);
    }

    public static string ToString2(this Span<byte> buffer, int offset, int count)
    {
        return Encoding.ASCII.GetString(buffer.Slice(offset, count));
    }

    public static byte[] ToBytes(this string text)
    {
        return Encoding.ASCII.GetBytes(text);
    }
}
