namespace CompressionAlgorithms.LZSS;

internal class LZSSUtils
{
    //0: ieskom ilgiausio sutapimo
    //1: ieskom pirmo sutapimo -> GetLongestMatchStupid()
    //2: ieskom 2 sutapimu ir imam ilgesni is ju
    public static void GetLongestMatchStupid(byte[] history, byte[] buffer, out int offset, out int length)
    {
        offset = 0;
        length = 0;
        
        if (history.Length == 0)
            return;

        bool isMatch = false;
        //Find start of substring
        for(int j = 0; j < history.Length; j++)
        {
            if (history[j] == buffer[0]) 
            { 
                isMatch = true; 
                length++;
                offset = j;
                break; 
            }
        }
            
        // if after 1 pass no match found - there will be no at all
        if (!isMatch)
            return;


        //Find length of substring found
        if (isMatch && buffer.Length > 1)
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
                    return;
                }
                k++;
            }
        }
    }
}