using CompressionAlgorithms.BitStream;

namespace CompressionAlgorithms.ShannonFano;

public static class ShannonFanoEncoder
{
    // BufferSize should be at least 4096 bytes.
    private const int BufferSizeMultiplier = 4096;

    public static async Task EncodeAsync(string filePath, string outputFilePath, int wordLength)
    {
        await using var fileReader = new FileStream(filePath, FileMode.Open);
        await using var bitReader = new BitReader(fileReader);

        await using var fileWriter = new FileStream(outputFilePath, FileMode.Create);
        await using var bitWriter = new BitWriter(fileWriter);

        var bufferSize = BufferSizeMultiplier * wordLength;
        byte[] buffer = new byte[bufferSize];

        var metadata = await GetMetadataAsync(bitReader, buffer, wordLength);

        var header = ConstructHeader(metadata);
        bitWriter.Write(header);

        int readBitsCount;

        while ((readBitsCount = await bitReader.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false)) > 0)
        {
            var bitCount = readBitsCount >= buffer.Length
                ? readBitsCount
                : readBitsCount + metadata.BitsAddedToLastWordCount;

            var text = buffer[..bitCount];
            var encodedText = Encode(text, metadata.Codes);

            bitWriter.Write(encodedText);
        }

        var bitsAddedToFormLastByteCount = bitWriter.FillRemainingBitsToFormAByte();
        await OverwriteHeaderAsync(header, bitsAddedToFormLastByteCount, bitWriter);
    }

    private static async Task<ShannonFanoMetadata> GetMetadataAsync(BitReader bitReader, byte[] buffer, int wordLength)
    {
        var frequencies = await GetFrequenciesAsync(bitReader, buffer, wordLength);
        var codes = ShannonFanoUtils.ConstructCodes(frequencies);

        return new ShannonFanoMetadata
        {
            WordLength = wordLength,
            Codes = codes,
            ParserTree = new ShannonFanoParserNode(codes),
            BitsAddedToLastWordCount = GetBitsAddedToLastWordCount(wordLength, bitReader.Length)
        };
    }

    private static async Task OverwriteHeaderAsync(byte[] header, int bitsAddedToFormLastByteCount, BitWriter bitWriter)
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

    private static async Task<Dictionary<byte[], int>> GetFrequenciesAsync(BitReader bitReader, byte[] buffer, int wordLength)
    {
        var frequencies = new Dictionary<byte[], int>(new ShannonFanoUtils.ByteArrayEqualityComprarer());

        var bitsAddedToLastWordCount = GetBitsAddedToLastWordCount(wordLength, bitReader.Length);
        int readBitsCount;

        while ((readBitsCount = await bitReader.ReadAtLeastAsync(buffer, minimumBytes: buffer.Length, throwOnEndOfStream: false)) > 0)
        {
            // This means this is the end of a file and the last word is not complete. So we add some fictive bits.
            var bitCount = readBitsCount >= buffer.Length
                ? readBitsCount
                : readBitsCount + bitsAddedToLastWordCount;

            buffer[..bitCount].CalculateFrequencies(wordLength, frequencies);
        }

        bitReader.SeekToBeginning();

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

    private static byte[] Encode(byte[] text, Dictionary<byte[], byte[]> codes)
    {
        var encodedText = new List<byte>();
        var wordLength = codes.First().Key.Length;

        for (var i = 0; i < text.Length; i += wordLength)
        {
            var word = text[i..(i + wordLength)];
            encodedText.AddRange(codes[word]);
        }

        return [.. encodedText];
    }

    /// <summary>
    /// bitsAddedToFormLastByte takes first 3 bits but cannot be known before encoding is done. Do not forget to overwrite those first 3 bits.
    /// </summary>
    private static byte[] ConstructHeader(ShannonFanoMetadata metadata)
    {
        return
        [
            .. new byte[] { 0, 0, 0 },
            .. metadata.BitsAddedToLastWordCount.EliasGammaCode(),
            .. metadata.WordLength.EliasGammaCode(),
            .. metadata.ParserTree.ConstructTreeHeader(),
        ];
    }
}
