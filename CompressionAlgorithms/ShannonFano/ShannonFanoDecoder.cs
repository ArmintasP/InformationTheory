using CompressionAlgorithms.BitStream;

namespace CompressionAlgorithms.ShannonFano;

public static class ShannonFanoDecoder
{
    public static async Task DecodeAsync(string filePath, string outputFilePath)
    {
        await using var fileReader = new FileStream(filePath, FileMode.Open);
        await using var bitReader = new BitReader(fileReader);

        await using var fileWriter = new FileStream(outputFilePath, FileMode.Create);
        await using var bitWriter = new BitWriter(fileWriter);

        var metadata = GetMetadata(bitReader);

        bitReader.IgnoreNLastBitsWhenEOF(metadata.BitsAddedToFormLastByteCount);

        var bufferSize = 1024 * metadata.WordLength;
        var buffer = new byte[bufferSize];
        var leftovers = Array.Empty<byte>();

        var decodedText = new List<byte>();
        int readBitsCount;

        while ((readBitsCount = await bitReader.ReadAtLeastAsync(buffer, minimumBytes: buffer.Length, throwOnEndOfStream: false)) > 0 || leftovers.Length > 0)
        {
            var offset = 0;
            var index = 0;
            byte[]? decodedWord;

            leftovers = [.. leftovers, .. buffer[..readBitsCount]];

            while ((decodedWord = metadata.ParserTree.GetWord(leftovers, offset, ref index)) is not null)
            {
                decodedText.AddRange(decodedWord);
                offset += index;
                index = 0;
            }

            if (bitReader.IsEOF())
            {
                decodedText.RemoveRange(decodedText.Count - metadata.BitsAddedToLastWordCount, metadata.BitsAddedToLastWordCount);
            }

            await bitWriter.WriteAsync(decodedText.ToArray());
            decodedText.Clear();

            leftovers = leftovers[offset..];
        }
    }

    private static ShannonFanoMetadata GetMetadata(BitReader bitReader)
    {
        var buffer = new byte[3];
        bitReader.ReadExactly(buffer);
        var bitsAddedToFormLastByteCount = buffer.ToInt32();

        var bitsAddedToLastWordCount = Utils.DecodeEliasGammaCode(bitReader);
        var wordLength = Utils.DecodeEliasGammaCode(bitReader);

        var tree = new ShannonFanoParserNode(bitReader, wordLength);

        return new ShannonFanoMetadata
        {
            WordLength = wordLength,
            BitsAddedToLastWordCount = bitsAddedToLastWordCount,
            BitsAddedToFormLastByteCount = bitsAddedToFormLastByteCount,
            ParserTree = tree,
            Codes = [],
        };
    }
}
