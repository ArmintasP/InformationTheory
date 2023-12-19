namespace CompressionAlgorithms.LZSS;

internal class LZSSMetadata
{ 
    public int BitsAddedToFormLastByteCount { get; set; }

    public required int MaxHistoryLength { get; set; }
    public required int MaxMatchLenght { get; set; }
}
