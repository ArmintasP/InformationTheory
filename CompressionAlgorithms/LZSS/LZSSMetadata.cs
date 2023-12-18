namespace CompressionAlgorithms.LZSS
{
    internal class LZSSMetadata
    { 
        //public required int BitsAddedToLastWordCount { get; set; } as the length of record is set, no need for this one.
        public int BitsAddedToFormLastByteCount { get; set; }

        public required int MaxHistoryLength { get; set; }
        public required int MaxMatchLenght { get; set; }
    }
}
