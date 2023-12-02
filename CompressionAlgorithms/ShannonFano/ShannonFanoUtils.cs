using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;

namespace CompressionAlgorithms.ShannonFano;

public static class ShannonFanoUtils
{
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
