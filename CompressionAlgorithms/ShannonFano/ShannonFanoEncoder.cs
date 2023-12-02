namespace CompressionAlgorithms.ShannonFano;

public static class ShannonFanoEncoder
{
    public static void Encode(string filePath, string outputFilePath, int wordLength)
    {
        var bufferSize = 1024 * wordLength;
        byte[] buffer = new byte[bufferSize];

        var frequencies = GetFrequencies(filePath, buffer, wordLength);
        var codes = ShannonFanoUtils.ConstructCodes(frequencies);
        var tree = new ShannonFanoParserTree(codes);
        
        using var fileReader = new FileStream(filePath, FileMode.Open);
        using var bitReader = new BitStream(fileReader);

        using var fileWriter = new FileStream(outputFilePath, FileMode.Create);
        using var bitWriter = new BitStream(fileWriter);

        var amountOfBitsFromLastWord = wordLength > bitReader.Length
            ? (int)bitReader.Length
            : (int)(bitReader.Length % wordLength);
        
        var bitsAddedToLastWord = amountOfBitsFromLastWord != 0
            ? wordLength - amountOfBitsFromLastWord
            : 0;

        // For now, we set bitsToCutBeforeDecoding to 0. Only later will we find that out and change this number.
        var header = ShannonFanoUtils.ConstructHeader(wordLength, bitsAddedToLastWord, tree, bitsAddedToFormLastByte: 0);
        bitWriter.Write(header.ToBytes());
        
        int readBitsCount;

        while ((readBitsCount = bitReader.ReadAtLeast(buffer, minimumBytes: buffer.Length, throwOnEndOfStream: false)) > 0)
        {
            var bitCount = readBitsCount >= buffer.Length
                ? readBitsCount
                : readBitsCount + bitsAddedToLastWord;

            var text = buffer.ToString2(offset: 0, bitCount);
            var encodedText = ShannonFanoUtils.Encode(text, codes);

            var bytesToWrite = encodedText.ToBytes();
            bitWriter.Write(bytesToWrite);
        }
        
        var bitsAddedToFormLastByte = bitWriter.FillRemainingBitsToFormAByte();
        OverwriteHeader(header, bitsAddedToFormLastByte, bitWriter);
    }
    
    private static void OverwriteHeader(string header, int bitsAddedToFormLastByte, BitStream bitWriter)
    {
        var bitsAddedToFormLastByteAsBinaryString = bitsAddedToFormLastByte.ToBase2(padding: 3);
        
        // Only first byte needs to be overriden.
        var firstHeaderByte = bitsAddedToFormLastByteAsBinaryString + header[3..8];

        bitWriter.SeekToBeginning();
        bitWriter.Write(firstHeaderByte.ToBytes());
    }

    private static Dictionary<string, int> GetFrequencies(string filePath, Span<byte> buffer, int wordLength)
    {
        var frequencies = new Dictionary<string, int>();

        using var fileReader = new FileStream(filePath, FileMode.Open);
        using var bitReader = new BitStream(fileReader);

        var amountOfBitsFromLastWord = wordLength > bitReader.Length
            ? (int)bitReader.Length
            : (int)(bitReader.Length % wordLength);

        var bitsAddedToLastWord = amountOfBitsFromLastWord != 0
            ? wordLength - amountOfBitsFromLastWord
            : 0;

        int readBitsCount;

        while ((readBitsCount = bitReader.ReadAtLeast(buffer, minimumBytes: buffer.Length, throwOnEndOfStream: false)) > 0)
        {
            // This means this is the end of a file and the last word is not complete. So we add some fictive bits.
            var bitCount = readBitsCount >= buffer.Length
                ? readBitsCount
                : readBitsCount + bitsAddedToLastWord;

            var text = buffer.ToString2(offset: 0, bitCount);
            text.CalculateFrequencies(wordLength, frequencies);
        }

        return frequencies;
    }
}
