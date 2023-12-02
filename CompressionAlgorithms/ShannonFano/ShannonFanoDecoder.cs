
using System.Text;

namespace CompressionAlgorithms.ShannonFano;

public static class ShannonFanoDecoder
{
    public static async Task DecodeAsync(string filePath, string outputFilePath)
    {
        await using var fileReader = new FileStream(filePath, FileMode.Open);
        await using var bitReader = new BitStream(fileReader);
        
        await using var fileWriter = new FileStream(outputFilePath, FileMode.Create);
        await using var bitWriter = new BitStream(fileWriter);

        var (wordLength, bitsAddedToFormLastByte, bitsAddedToLastWord) = ShannonFanoUtils.ParseHeader(bitReader);
        bitReader.IgnoreNLastBitsWhenEOF(bitsAddedToFormLastByte);
        
        var bufferSize = 1024 * wordLength;
        var buffer = new byte[bufferSize];
        var leftovers = Array.Empty<byte>();

        var tree = new ShannonFanoParserTree(bitReader, wordLength);

        int readBitsCount;

        while ((readBitsCount = await bitReader.ReadAtLeastAsync(buffer, minimumBytes: buffer.Length, throwOnEndOfStream: false)) > 0 || leftovers.Length > 0)
        {
            var decodedText = new StringBuilder();

            var offset = 0;
            var index = 0;
            string? decodedWord;

            leftovers = leftovers.Concat(buffer[..readBitsCount]).ToArray();

            while ((decodedWord = tree.GetWord(leftovers, offset, ref index)) is not null)
            {
                decodedText.Append(decodedWord);
                offset += index;
                index = 0;
            }

            if (bitReader.IsEOF())
            {
                decodedText.Length -= bitsAddedToLastWord;
            }
            
            var bytesToWrite = decodedText.ToString().ToBytes();

            await bitWriter.WriteAsync(bytesToWrite);
            leftovers = leftovers[offset..];
        }
    }
}
