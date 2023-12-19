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
        MaxHistoryLength = (int)Math.Pow(2, metadata.MaxHistoryLength) - 1;

        bitReader.IgnoreNLastBitsWhenEOF(metadata.BitsAddedToFormLastByteCount);

        var buffer = new byte[MaxHistoryLength * WordLength];
        var history = new byte[MaxHistoryLength * WordLength];
        var historyPos = 0;

        var leftovers = new List<byte>();


        int readBitsCount;

        while ((readBitsCount = await bitReader.ReadAtLeastAsync(buffer, minimumBytes: buffer.Length, throwOnEndOfStream: false)) > 0)
        {
            var bitCount = readBitsCount >= buffer.Length
            ? readBitsCount
            : readBitsCount;

            var text = buffer[..bitCount];

            if (leftovers.Count > 0)
            {
                text = leftovers.Concat(buffer[..bitCount]).ToArray();
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

        while (decodingPos < buffer.Length)
        {
            var indicator = buffer[decodingPos]; //gets first BIT
            decodingPos++;

            if (indicator == (byte)1)
            {
                //deal with buffer 'leftovers' -> end of record is still not read:
                if (decodingPos + WordLength > buffer.Length)
                {
                    leftovers.AddRange(buffer[(decodingPos-1)..buffer.Length]);
                    return decodedText;
                }

                var decodedWord = buffer[decodingPos..(decodingPos + WordLength)];

                if (historyPos + WordLength > (MaxHistoryLength * WordLength))
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
                if (decodingPos + metadata.MaxHistoryLength + metadata.MaxMatchLenght  > buffer.Length)
                {
                    leftovers.AddRange(buffer[(decodingPos-1)..buffer.Length]);
                    return decodedText;
                }

                var offset = buffer[decodingPos..(decodingPos + metadata.MaxHistoryLength)]; //read offset in bits
                var offsetIndex = ConvertToNumber(offset);
                decodingPos += metadata.MaxHistoryLength;

                var length = buffer[decodingPos..(decodingPos + metadata.MaxMatchLenght)]; //read length in bits
                var matchLength = ConvertToNumber(length);                   
                decodingPos += metadata.MaxMatchLenght;

                var decodedWord = history[(offsetIndex * WordLength)..((offsetIndex + matchLength) * WordLength)];

                //Console.WriteLine($"({offsetIndex},{matchLength})");
                //Console.WriteLine("Word to insert:");
                //for (int i = 0; i < decodedWord.Length; i += WordLength)
                //{
                //    Console.Write(ConvertToChar(decodedWord[i..(i + WordLength)]));
                //}
                decodedText.AddRange(decodedWord);
                
                if (historyPos + (matchLength * WordLength) > (MaxHistoryLength * WordLength))
                {
                    Array.Clear(history);
                    historyPos = 0;
                }
                Array.Copy(decodedWord, 0, history, historyPos, decodedWord.Length);
                historyPos += WordLength * matchLength;
                //Console.WriteLine("History is:");
                //for (int i = 0; i < historyPos; i += WordLength)
                //{
                //    Console.Write(ConvertToChar(history[i..(i + WordLength)]));
                //}
                //Console.WriteLine();

            }
        }
        if(historyPos == MaxHistoryLength*WordLength)
        {
            //Console.WriteLine("History is:");
            //for (int i = 0; i < historyPos; i += WordLength)
            //{
            //    Console.Write(ConvertToChar(history[i..(i + WordLength)]));
            //}
            //Console.WriteLine();
            Array.Clear(history);
            historyPos = 0;
        }
        return decodedText;
    }
    private static int ConvertToNumber(byte[] numberInBits)
    {
        int result = 0;

        for (int i = 0; i < numberInBits.Length; i++)
        {
            // Shift the current result to the left by one bit
            result <<= 1;

            // If the current bit is 1, set the least significant bit of the result to 1
            if (numberInBits[i] != 0) 
            {
                result |= 1;
            }
        }

        return result;

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
