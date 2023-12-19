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
            for (int j = offset + length; j < history.Length; j++)
            {
                if (history[j] == buffer[0]) //always search from the START of buffer
                {
                    isMatch = true;
                    length = 1;
                    offset = j;
                    break;
                }
            }

            //Find length of substring found
            if (isMatch && buffer.Length > 1) //feels like I could make this simpler.
            {
                int k = 1;
                while (offset + k < history.Length)
                {
                    if (k < buffer.Length && history[offset + k] == buffer[k])
                    {
                        length++;
                    }
                    else
                    {
                        //End of k-th substring is here.
                        kCommonSubstrings.Add((offset, length));
                        break;
                    }
                    k++;
                }
            }

            if (numberOfMatchesToFind != 0 && kCommonSubstrings.Count == numberOfMatchesToFind)
            {
                break;
            }
        }

        //Search is done:
        if (kCommonSubstrings.Count != 0)
        {
            longestSubstring = kCommonSubstrings.OrderByDescending(t => t.Item2).First();
            offset = longestSubstring.Item1;
            length = longestSubstring.Item2;
        }
        return;
    }
}