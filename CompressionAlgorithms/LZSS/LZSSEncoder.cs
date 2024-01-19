using CompressionAlgorithms.BitStream;
namespace CompressionAlgorithms.LZSS;

internal class EncoderParameters
{
    public int MaxHistoryLengthInBits;
    public int MaxMatchLengthInBits;
    public int MaxHistoryLength;
    public int MaxMatchLength;
    public int BreakEvenPoint;
    public int SearchDepth;
}
public static class LZSSEncoder
{
    private const int InputStreamChunckSize = 4096;
    private const int WordLength = 8;

    private static EncoderParameters EncoderParameters = new();
    public static async Task CompressAsync(string filePath, string outputFilePath, int maxHistoryLength, int maxMatchLength, int searchDepth)
    {
        EncoderParameters = CalculateParameters(maxHistoryLength, maxMatchLength, searchDepth);

        await using var fileReader = new FileStream(filePath, FileMode.Open);

        await using var fileWriter = new FileStream(outputFilePath, FileMode.Create);
        await using var bitWriter = new BitWriter(fileWriter);

        var header = ConstructHeader(maxHistoryLength, maxMatchLength);
        bitWriter.Write(header);

        var inputStreamChunck = new byte[InputStreamChunckSize];
        var history = new byte[EncoderParameters.MaxHistoryLength];
        var historyPos = 0;

        int readBytesCount;

        while ((readBytesCount = await fileReader.ReadAtLeastAsync(inputStreamChunck, InputStreamChunckSize, throwOnEndOfStream: false)) > 0)
        {
            var text = inputStreamChunck[..readBytesCount];

            var encodedText = Compress(history, ref historyPos, text);

            bitWriter.Write(encodedText);
        }
        var bitsAddedToFormLastByteCount = bitWriter.FillRemainingBitsToFormAByte();
        await Utils.OverwriteHeaderAsync(header, bitsAddedToFormLastByteCount, bitWriter);
    }
    private static byte[] Compress(byte[] history, ref int historyPos, byte[] buffer)
    {
        var encodedText = new List<byte>();
        var bufferSize = buffer.Length;

        var codingPos = 0;

        //1) history is empty, buffer full
        //2) try to find match
        //3) no match - encode as record type 1, (move buffer +1), (add to history +1)
        //4) found match (and longer than breakEvenPoint) - encode as record type 2,
        //move buffer + matchLength, add to history + match length

        while (codingPos < bufferSize)
        {

            int bufferViewEndIndex = (codingPos + EncoderParameters.MaxMatchLength) > bufferSize
                ? bufferSize
                : EncoderParameters.MaxMatchLength + codingPos;

            LZSSUtils.GetMatch(history[..historyPos], buffer[codingPos..bufferViewEndIndex], EncoderParameters.SearchDepth,
                               out int matchOffset, out int matchLength);

            if (matchLength <= EncoderParameters.BreakEvenPoint)
            {
                encodedText.AddRange(CreateType1Record(buffer[codingPos]));

                if (historyPos + 1 > EncoderParameters.MaxHistoryLength)
                {
                    Array.Copy(history, 1, history, 0, EncoderParameters.MaxHistoryLength - 1);
                    historyPos--;
                }
                
                Array.Copy(buffer, codingPos, history, historyPos, 1);
                historyPos++;
                codingPos++;
            }
            else
            {
                encodedText.AddRange(CreateType2Record(matchOffset, matchLength));

                if (historyPos + matchLength > EncoderParameters.MaxHistoryLength)
                {
                    var missingSpace = historyPos + matchLength - EncoderParameters.MaxHistoryLength;
                    Array.Copy(history, missingSpace, history, 0, EncoderParameters.MaxHistoryLength - missingSpace);
                    historyPos -= missingSpace;
                }
                
                Array.Copy(buffer, codingPos, history, historyPos, matchLength);
                codingPos += matchLength;
                historyPos += matchLength;
            }
        }
        return [.. encodedText];
    }

    private static byte[] CreateType1Record(byte symbol)
    {
        // Record1 is 9 bits -> (1,8)
        byte[] recordType = Utils.ToBase2(1, 1);
        byte[] symbolAsBits = LZSSUtils.ConvertToBitArray(symbol);
        return [.. recordType, .. symbolAsBits];
    }

    private static byte[] CreateType2Record(int offset, int length)
    {
        // Record2 is x bits -> (0, MaxHistoryLenght, MaxMatchLength)
        byte[] recordType = Utils.ToBase2(0, 1);
        byte[] binaryOffset = Utils.ToBase2(offset, EncoderParameters.MaxHistoryLengthInBits);
        byte[] binaryLenght = Utils.ToBase2(length, EncoderParameters.MaxMatchLengthInBits);

        return [.. recordType, .. binaryOffset, .. binaryLenght];
    }

    /// <summary>
    /// bitsAddedToFormLastByte takes first 3 bits but cannot be known before encoding is done. Do not forget to overwrite those first 3 bits.
    /// </summary>
    private static byte[] ConstructHeader(int maxHistoryLength, int maxMatchLength)
    {
        return
        [
            .. new byte[] { 0, 0, 0 },
            .. maxHistoryLength.EliasGammaCode(),
            .. maxMatchLength.EliasGammaCode(),
        ];
    }

    private static EncoderParameters CalculateParameters(int maxHistoryLength, int maxMatchLength, int searchDepth)
    {
        double record2Length = maxHistoryLength + maxMatchLength + 1;
        return new EncoderParameters
        {
            MaxHistoryLengthInBits = maxHistoryLength,
            MaxMatchLengthInBits = maxMatchLength,
            MaxHistoryLength = (int)Math.Pow(2, maxHistoryLength) - 1,
            MaxMatchLength = (int)Math.Pow(2, maxMatchLength) - 1,
            BreakEvenPoint = (int)Math.Ceiling(record2Length / 9),
            SearchDepth = searchDepth
        };
    }
}

