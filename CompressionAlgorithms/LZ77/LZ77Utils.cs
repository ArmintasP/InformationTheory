using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;

namespace CompressionAlgorithms.LZ77;

internal class LZ77Utils
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
        for(int i = 0; i < buffer.Length; i ++)
        {
            bool isMatch = false;
            //find start of substring
            for(int j = 0; j < history.Length; j ++)
            {
                if (history[j] == buffer[i]) 
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
            if (isMatch)
            {
                for (int k = i+1; k < buffer.Length; k++)
                {
                    for (int m = offset+k; m < history.Length; m++) //Careful with where you start search!
                    {
                        if (history[m] == buffer[k])
                        {
                            length++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                return; //Search is fin.
            }
            
        }
    }
}