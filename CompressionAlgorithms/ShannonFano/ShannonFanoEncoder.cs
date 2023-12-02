using CompressionAlgorithms.BitStream;

namespace CompressionAlgorithms.ShannonFano;

public static class ShannonFanoEncoder
{
    public static void Encode(string filePath, string outputFilePath, int wordLength)
    {
        var bufferSize = 4096 * wordLength;
        byte[] buffer = new byte[bufferSize];

        var frequencies = GetFrequencies(filePath, buffer, wordLength);
        var codes = ShannonFanoUtils.ConstructCodes(frequencies);
        var tree = new ShannonFanoParserTree(codes);

        using var fileReader = new FileStream(filePath, FileMode.Open);
        using var bitReader = new BitReader(fileReader);

        using var fileWriter = new FileStream(outputFilePath, FileMode.Create);
        using var bitWriter = new BitWriter(fileWriter);

        var bitsAddedToLastWordCount = GetBitsAddedToLastWordCount(wordLength, bitReader.Length);

        // For now, we set bitsToCutBeforeDecoding to 0. Only later will we find that out and change this number.
        var header = ShannonFanoUtils.ConstructHeader(wordLength, bitsAddedToLastWordCount, tree, bitsAddedToFormLastByte: 0);
        bitWriter.Write(header);

        int readBitsCount;

        while ((readBitsCount = bitReader.ReadAtLeast(buffer, minimumBytes: buffer.Length, throwOnEndOfStream: false)) > 0)
        {
            var bitCount = readBitsCount >= buffer.Length
                ? readBitsCount
                : readBitsCount + bitsAddedToLastWordCount;

            var text = buffer[.. bitCount];
            var encodedText = ShannonFanoUtils.Encode(text, codes);

            bitWriter.Write(encodedText);
        }

        var bitsAddedToFormLastByte = bitWriter.FillRemainingBitsToFormAByte();
        OverwriteHeader(header, bitsAddedToFormLastByte, bitWriter);
    }

    private static void OverwriteHeader(byte[] header, int bitsAddedToFormLastByte, BitWriter bitWriter)
    {
        var bitsAddedToFormLastByteInBinary = bitsAddedToFormLastByte.ToBase2(padding: 3);

        // Only 3 bits are needed for `bitsAddedToFormLastByteInBinary`.
        header[0] = bitsAddedToFormLastByteInBinary[0];
        header[1] = bitsAddedToFormLastByteInBinary[1];
        header[2] = bitsAddedToFormLastByteInBinary[2];

        // Only first byte needs to be overriden.
        var firstHeaderByte = header[0..8];

        bitWriter.SeekToBeginning();
        bitWriter.Write(firstHeaderByte);
    }

    private static Dictionary<byte[], int> GetFrequencies(string filePath, Span<byte> buffer, int wordLength)
    {
        var frequencies = new Dictionary<byte[], int>(new ShannonFanoUtils.ByteArrayEqualityComprarer());

        using var fileReader = new FileStream(filePath, FileMode.Open);
        using var bitReader = new BitReader(fileReader);

        var bitsAddedToLastWordCount = GetBitsAddedToLastWordCount(wordLength, bitReader.Length);
        int readBitsCount;

        while ((readBitsCount = bitReader.ReadAtLeast(buffer, minimumBytes: buffer.Length, throwOnEndOfStream: false)) > 0)
        {
            // This means this is the end of a file and the last word is not complete. So we add some fictive bits.
            var bitCount = readBitsCount >= buffer.Length
                ? readBitsCount
                : readBitsCount + bitsAddedToLastWordCount;

            buffer[..bitCount].CalculateFrequencies(wordLength, frequencies);
        }

        return frequencies;
    }

    private static int GetBitsAddedToLastWordCount(int wordLength, long fileLength)
    {
        var bitsFromLastWordCount = wordLength > fileLength
            ? (int)fileLength
            : (int)(fileLength % wordLength);

        var bitsAddedToLastWordCount = bitsFromLastWordCount != 0
            ? wordLength - bitsFromLastWordCount
            : 0;

        return bitsAddedToLastWordCount;
    }
}
