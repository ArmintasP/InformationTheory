using CompressionAlgorithms.BitStream;

namespace CompressionAlgorithms.LZSS;

internal class LZSSDecoder
{
    private const int WordLength = 8;
    private static int MaxHistoryLength;

    public static async Task DecodeAsync(string filePath, string outputFilePath)
    {
        await using var fileReader = new FileStream(filePath, FileMode.Open);
        await using var bitReader = new BitReader(fileReader);

        await using var fileWriter = new FileStream(outputFilePath, FileMode.Create);
        await using var bitWriter = new BitWriter(fileWriter);

        var metadata = GetMetadata(bitReader);
        MaxHistoryLength = ((int)Math.Pow(2, metadata.MaxHistoryLength) - 1) * WordLength;

        bitReader.IgnoreNLastBitsWhenEOF(metadata.BitsAddedToFormLastByteCount);

        var buffer = new byte[MaxHistoryLength * metadata.MaxMatchLenght];
        var history = new byte[MaxHistoryLength];
        var historyPos = 0;

        var leftovers = new List<byte>();


        int readBitsCount;

        while ((readBitsCount = await bitReader.ReadAtLeastAsync(buffer, minimumBytes: buffer.Length, throwOnEndOfStream: false)) > 0)
        {
            var text = buffer[..readBitsCount];

            if (leftovers.Count > 0)
            {
                text = leftovers.Concat(buffer[..readBitsCount]).ToArray();
            }
            var decodedText = Decode(text, history, ref historyPos, leftovers, metadata);

            await bitWriter.WriteAsync(decodedText.ToArray());
        }
    }
    private static List<byte> Decode(byte[] buffer, byte[] history, ref int historyPos, List<byte> leftovers, LZSSMetadata metadata)
    {
        var decodedText = new List<byte>();
        var decodingPos = 0;
        leftovers.Clear();

        //1) Get indicator bit 
        //2.a) indicator == 1 : decoded word is next 8 bits
        //2.b) indicator == 0 : next maxHistoryLength bits is offset, next maxMatchLenght bits is length write from history
        //3) If buffer does not contain all record. Method returns to get more from input stream.
        //4) Clear history if it reached maxHistoryLength
        //5) Add to history the word found.

        while (decodingPos < buffer.Length)
        {
            var indicator = buffer[decodingPos];
            decodingPos++;

            if (indicator == 1)
            {
                var record1Length = decodingPos + WordLength;
                if (record1Length > buffer.Length)
                {
                    leftovers.AddRange(buffer[(decodingPos-1)..buffer.Length]);
                    return decodedText;
                }

                var decodedWord = buffer[decodingPos..(decodingPos + WordLength)];

                if (historyPos + WordLength > MaxHistoryLength)
                {
                    Array.Clear(history);
                    historyPos = 0;
                }
                Array.Copy(buffer, decodingPos, history, historyPos, WordLength);
                decodingPos += WordLength;
                historyPos += WordLength;

                decodedText.AddRange(decodedWord);

            }
            else
            {
                var record2Length = decodingPos + metadata.MaxHistoryLength + metadata.MaxMatchLenght;
                if (record2Length > buffer.Length)
                {
                    leftovers.AddRange(buffer[(decodingPos-1)..buffer.Length]);
                    return decodedText;
                }

                var offset = buffer[decodingPos..(decodingPos + metadata.MaxHistoryLength)];
                var offsetIndex = LZSSUtils.ConvertToNumber(offset);
                decodingPos += metadata.MaxHistoryLength;

                var length = buffer[decodingPos..(decodingPos + metadata.MaxMatchLenght)];
                var matchLength = LZSSUtils.ConvertToNumber(length);                   
                decodingPos += metadata.MaxMatchLenght;

                var decodedWord = history[(offsetIndex * WordLength)..((offsetIndex + matchLength) * WordLength)];

                decodedText.AddRange(decodedWord);
                
                if (historyPos + (matchLength * WordLength) > MaxHistoryLength)
                {
                    Array.Clear(history);
                    historyPos = 0;
                }
                Array.Copy(decodedWord, 0, history, historyPos, decodedWord.Length);
                historyPos += WordLength * matchLength;
            }
        }
        if(historyPos == MaxHistoryLength)
        {
            Array.Clear(history);
            historyPos = 0;
        }
        return decodedText;
    }

    private static LZSSMetadata GetMetadata(BitReader bitReader)
    {
        var buffer = new byte[3];
        bitReader.ReadExactly(buffer);
        var bitsAddedToFormLastByteCount = buffer.ToInt32();

        var maxHistoryLength = Utils.DecodeEliasGammaCode(bitReader);
        var maxMatchLenght = Utils.DecodeEliasGammaCode(bitReader);

        return new LZSSMetadata
        {
            BitsAddedToFormLastByteCount = bitsAddedToFormLastByteCount,
            MaxHistoryLength = maxHistoryLength,
            MaxMatchLenght = maxMatchLenght
        };
    }
}
