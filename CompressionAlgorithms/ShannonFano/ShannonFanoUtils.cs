using CompressionAlgorithms.BitStream;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Text;

namespace CompressionAlgorithms.ShannonFano;

public static class ShannonFanoUtils
{
    public static (int wordLength, int bitsAddedToFormLastByte, int bitsAddedToLastWord) ParseHeader(BitReader bitReader)
    {
        var buffer = new byte[3];
        bitReader.ReadExactly(buffer);
        var bitsAddedToFormLastByte = buffer.ToInt32();

        var bitsAddedToLastWord = Utils.DecodeEliasGammaCode(bitReader);
        var wordLength = Utils.DecodeEliasGammaCode(bitReader);

        return (wordLength, bitsAddedToFormLastByte, bitsAddedToLastWord);
    }

    /// <summary>
    /// bitsToCutBeforeDecoding takes first 3 bits. Do not forget to update it.
    /// </summary>
    public static byte[] ConstructHeader(int wordLength, int bitsAddedToLastWord, ShannonFanoParserTree tree, int bitsAddedToFormLastByte = 0)
    {
        return bitsAddedToFormLastByte.ToBase2(padding: 3)
            .Concat(bitsAddedToLastWord.EliasGammaCode())
            .Concat(wordLength.EliasGammaCode())
            .Concat(tree.ConstructTreeHeader())
            .ToArray();
    }

    public static byte[] Encode(byte[] text, Dictionary<byte[], byte[]> codes)
    {
        var encodedText = new List<byte>();
        var wordLength = codes.First().Key.Length;

        for (var i = 0; i < text.Length; i += wordLength)
        {
            var word = text[i..(i + wordLength)];
            encodedText.AddRange(codes[word]);
        }

        return encodedText.ToArray();
    }

    public static Dictionary<byte[], byte[]> ConstructCodes(Dictionary<byte[], int> frequencies)
    {
        var equalityComparer = new ByteArrayEqualityComprarer();

        if (frequencies.Count is 1)
        {
            return new Dictionary<byte[], byte[]>(new ByteArrayEqualityComprarer())
            {
                [frequencies.First().Key] = [0]
            };
        }

        var sortedFrequencies = frequencies
            .OrderByDescending(x => x.Value)
            .ToDictionary(x => x.Key, x => x.Value);
        
        var codes = new Dictionary<byte[], List<byte>>(equalityComparer);
        ConstructCodes(sortedFrequencies, sortedFrequencies.Keys.ToList(), codes);

        return codes.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToArray(),
            equalityComparer);
    }

    private static void ConstructCodes(Dictionary<byte[], int> sortedFrequencies, List<byte[]> words, Dictionary<byte[], List<byte>> codes)
    {
        if (words.Count is 1)
            return;

        var middleIndex = FindMiddleIndex(words, sortedFrequencies);

        var firstHalf = words[0..middleIndex];
        var secondHalf = words[middleIndex..];

        foreach (var word in firstHalf)
        {
            codes.TryGetValue(word, out var code);

            if (code is null)
                codes[word] = [];

            codes[word].Add(0);
        }

        foreach (var word in secondHalf)
        {
            codes.TryGetValue(word, out var code);

            if (code is null)
                codes[word] = [];

            codes[word].Add(1);
        }

        ConstructCodes(sortedFrequencies, firstHalf, codes);
        ConstructCodes(sortedFrequencies, secondHalf, codes);
    }

    private static int FindMiddleIndex(List<byte[]> words, Dictionary<byte[], int> sortedFrequencies)
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
    
    public sealed class ByteArrayEqualityComprarer : IEqualityComparer<byte[]>
    {
        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x is null || y is null)
                return false;
            
            return x.SequenceEqual(y);
        }

        public int GetHashCode([DisallowNull] byte[] obj)
        {
            return unchecked((int)XxHash32.HashToUInt32(obj));
        }
    }
}
