﻿using CompressionAlgorithms.BitStream;
namespace CompressionAlgorithms.LZ77;

public static  class LZ77Encoder
{
    // BufferSize should be at least 4096 bytes.
    private const int BufferSizeMultiplier = 2048;  // -> taking smaller so that the buffer would be smaller than history
    private const int WordLength = 8;         

    private static int MaxHistoryLengthInBits; 
    private static int MaxMatchLenghtInBits;  
    private static int MaxHistoryLength;
    private static int MaxMatchLength;
    private static int BreakEvenPoint;      
    public static async Task CompressAsync(string filePath, string outputFilePath, int maxHistoryLength, int maxMatchLength)
    {
        MaxHistoryLengthInBits = maxHistoryLength;
        MaxMatchLenghtInBits = maxMatchLength;
        BreakEvenPoint = (MaxHistoryLengthInBits + MaxMatchLenghtInBits) / 8;

        MaxHistoryLength = (int)Math.Pow(2, maxHistoryLength) - 1;
        MaxMatchLength = (int)Math.Pow(2, maxMatchLength) - 1;

        await using var fileReader = new FileStream(filePath, FileMode.Open);

        await using var fileWriter = new FileStream(outputFilePath, FileMode.Create);
        await using var bitWriter = new BitWriter(fileWriter);

        var header = new byte[WordLength];
        bitWriter.Write(header);


        var bufferSize = BufferSizeMultiplier * WordLength;
        var buffer = new byte[bufferSize];
        var history = new List<byte>();

        int readBytesCount;

        while ((readBytesCount = await fileReader.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false)) > 0) 
        {
            var bitCount = readBytesCount >= buffer.Length
                ? readBytesCount
                : readBytesCount;

            var text = buffer[..bitCount];

            var encodedText = Compress(history, text);
            bitWriter.Write(encodedText);
        }
        var bitsAddedToFormLastByteCount = bitWriter.FillRemainingBitsToFormAByte();
        await OverwriteHeaderAsync(header, bitsAddedToFormLastByteCount, bitWriter);
    }
    private static byte[] Compress(List<byte> history, byte[] buffer)
    {
        var encodedText = new List<byte>();

        var historySize = history.Count;
        var bufferSize = buffer.Length;

        var codingPos = 0;

        //1) history is empty, buffer full
        //2) try to find match
        //3) no match - encode as record type 1, (move buffer +1), add to history +1
        //4) match (longer than breakEvenPoint)- encode as record type 2, move buffer + matchLength, add to history + match length

        while (codingPos < bufferSize)
        {
            int matchOffset = 0;
            int matchLength = 0;
            int bufferViewEndIndex = (codingPos + MaxMatchLength) > bufferSize 
                ? bufferSize 
                : MaxMatchLength + codingPos;

            //having histo as list may not be a great idea
            LZ77Utils.GetLongestMatchStupid([..history], buffer[codingPos..bufferViewEndIndex], out matchOffset, out matchLength);

            if (matchLength <= BreakEvenPoint)
            {
                encodedText.AddRange(CreateType1Record(buffer[codingPos]));
                history.Add(buffer[codingPos]);
                codingPos++;
            }
            else 
            {
                //Console.WriteLine("FoundMatch!");
                //Console.WriteLine("Buffer VIEW is:");
                //foreach (byte b in buffer[codingPos..bufferViewEndIndex])
                //{
                //    Console.Write($"{b} ");
                //}
                //Console.WriteLine();

                //Console.WriteLine("History is:");
                //foreach (byte b in history)
                //{
                //    Console.Write($"{b} ");
                //}
                //Console.WriteLine();

                //Console.WriteLine("Match is:");
                //foreach (byte b in history[matchOffset..(matchOffset + matchLength)])
                //{
                //    Console.Write($"{b} ");
                //}
                //Console.WriteLine("\n");

                encodedText.AddRange(CreateType2Record(matchOffset, matchLength));
                history.AddRange(buffer[codingPos..(codingPos + matchLength)]);
                codingPos += matchLength;
            }

            if(history.Count == MaxHistoryLength)
            {
                history.Clear();
            }
        }
        return [.. encodedText];
    }


    private static byte[] CreateType1Record(byte symbol)
    {
        // Record1 is 9 bits -> (1, symbol) <1,8> in bits
        byte[] recordType = Utils.ToBase2(1, 1);
        byte[] symbolAsBits = ConvertToBitArray(symbol);
        return recordType.Concat(symbolAsBits).ToArray();
    }
    private static byte[] ConvertToBitArray(byte myByte)
    {
        byte[] bits = new byte[8];

        for (int i = 7; i >= 0; i--)
        {
            bits[i] = (byte)((myByte >> (7-i)) & 1); //no need to reverse this way
        }
        return bits;
    }

    private static byte[] CreateType2Record(int offset, int length)
    {
        // Record2 is x bits -> (0, offset, length) <1,MaxHistoryLenght,MaxMatchLength> in bits
        byte[] recordType = Utils.ToBase2(0, 1);
        byte[] binaryOffset = Utils.ToBase2(offset, MaxHistoryLengthInBits);
        byte[] binaryLenght = Utils.ToBase2(length, MaxMatchLenghtInBits);
        var record = recordType.Concat(binaryOffset).Concat(binaryLenght).ToArray();
        //Console.WriteLine($"Record length {record.Length} and record is:");
        //foreach (byte b in record)
        //{
        //    Console.Write($"{b}");
        //}
        //Console.WriteLine();
        return record;
    }

    //TODO: clean up code a bit - this can be moved to utils since both encoders use it.
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
}
