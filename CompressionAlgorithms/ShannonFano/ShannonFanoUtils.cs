using System.Text;

namespace CompressionAlgorithms.ShannonFano;

public static class ShannonFanoUtils
{
    public static (int wordLength, int bitsAddedToFormLastByte, int bitsAddedToLastWord) ParseHeader(Stream stream)
    {
        var buffer = new byte[3];
        stream.ReadExactly(buffer);
        var bitsAddedToFormLastByte = buffer.ToString2().ToInt32();

        var bitsAddedToLastWord = Utils.DecodeEliasGammaCode(stream);
        var wordLength = Utils.DecodeEliasGammaCode(stream);

        return (wordLength, bitsAddedToFormLastByte, bitsAddedToLastWord);
    }

    /// <summary>
    /// bitsToCutBeforeDecoding takes first 3 bits. Do not forget to update it.
    /// </summary>
    public static string ConstructHeader(int wordLength, int bitsAddedToLastWord, ShannonFanoParserTree tree, int bitsAddedToFormLastByte = 0)
    {
        return bitsAddedToFormLastByte.ToBase2(padding: 3) +
            bitsAddedToLastWord.EliasGammaCode() +
            wordLength.EliasGammaCode() +
            tree.ConstructTreeHeader();
    }

    public static string Encode(string text, Dictionary<string, string> codes)
    {
        var encodedText = new StringBuilder();
        var wordLength = codes.First().Key.Length;

        for (var i = 0; i < text.Length; i += wordLength)
        {
            var word = text[i..(i + wordLength)];
            encodedText.Append(codes[word]);
        }

        return encodedText.ToString();
    }

    public static Dictionary<string, string> ConstructCodes(Dictionary<string, int> frequencies)
    {
        if (frequencies.Count is 1)
        {
            return new Dictionary<string, string>
            {
                [frequencies.First().Key] = "0"
            };
        }

        var sortedFrequencies = frequencies
            .OrderByDescending(x => x.Value)
            .ToDictionary(x => x.Key, x => x.Value);

        var codes = new Dictionary<string, string>();
        ConstructCodes(sortedFrequencies, sortedFrequencies.Keys.ToList(), codes);
        return codes;
    }

    private static void ConstructCodes(Dictionary<string, int> sortedFrequencies, List<string> words, Dictionary<string, string> codes)
    {
        if (words.Count is 1)
            return;

        var middleIndex = FindMiddleIndex(words, sortedFrequencies);

        var firstHalf = words[0..middleIndex];
        var secondHalf = words[middleIndex..];

        foreach (var word in firstHalf)
            codes[word] = codes.TryGetValue(word, out var existingCode)
                ? existingCode + "0"
                : "0";

        foreach (var word in secondHalf)
            codes[word] = codes.TryGetValue(word, out var existingCode)
                ? existingCode + "1"
                : "1";

        ConstructCodes(sortedFrequencies, firstHalf, codes);
        ConstructCodes(sortedFrequencies, secondHalf, codes);
    }

    private static int FindMiddleIndex(List<string> words, Dictionary<string, int> sortedFrequencies)
    {
        var sum = words.Sum(word => sortedFrequencies[word]);
        var halfSum = sum / 2;
        var currentSum = 0;

        var i = 0;
        for (; i < words.Count; i++)
        {
            currentSum += sortedFrequencies[words[i]];

            if (currentSum >= halfSum)
                return i + 1;
        }
        return i + 1;
    }
}
