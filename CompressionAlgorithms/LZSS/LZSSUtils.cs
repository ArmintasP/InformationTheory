namespace CompressionAlgorithms.LZSS;

internal class LZSSUtils
{
    //numberOfMatchesToFind = 0: find longest match
    //numberOfMatchesToFind = 1: find first match
    //numberOfMatchesToFind > 1: find k-th matches and return longest
    public static void GetMatch(byte[] history, byte[] buffer, int numberOfMatchesToFind, out int offset, out int length)
    {
        offset = 0;
        length = 0;
        var kCommonSubstrings = new List<(int, int)>();
        var longestSubstring = (0, 0);

        if (history.Length <= 1)
            return;

        bool isMatch = true;
        while (isMatch)
        {
            //Find start of k-th substring (starts from end of last substring found)
            isMatch = false;
            for (int i = offset + length; i < history.Length; i++)
            {
                if (history[i] == buffer[0])
                {
                    isMatch = true;
                    length = 1;
                    offset = i;
                    break;
                }
            }

            //Find length of k-th substring 
            if (isMatch && buffer.Length > 1)
            {
                int j = 1;
                while (offset + j < history.Length)
                {
                    if (j < buffer.Length && history[offset + j] == buffer[j])
                    {
                        length++;
                    }
                    else
                    {
                        kCommonSubstrings.Add((offset, length));
                        break;
                    }
                    j++;
                }
            }

            if (numberOfMatchesToFind != 0 && kCommonSubstrings.Count == numberOfMatchesToFind)
            {
                break;
            }
        }

        //Find longest of substrings:
        if (kCommonSubstrings.Count != 0)
        {
            longestSubstring = kCommonSubstrings.OrderByDescending(t => t.Item2).First();
            offset = longestSubstring.Item1;
            length = longestSubstring.Item2;
        }
        return;
    }

    public static byte[] ConvertToBitArray(byte myByte)
    {
        byte[] bits = new byte[8];

        for (int i = 7; i >= 0; i--)
        {
            bits[i] = (byte)((myByte >> (7 - i)) & 1);
        }
        return bits;
    }
    public static int ConvertToNumber(byte[] numberInBits)
    {
        int result = 0;

        for (int i = 0; i < numberInBits.Length; i++)
        {
            result <<= 1;

            if (numberInBits[i] != 0)
            {
                result |= 1;
            }
        }
        return result;
    }
}