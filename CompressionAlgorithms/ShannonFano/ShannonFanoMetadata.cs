namespace CompressionAlgorithms.ShannonFano;

public struct ShannonFanoMetadata
{
    public required int WordLength { get; set; }
    public required int BitsAddedToLastWordCount { get; set; }
    public int BitsAddedToFormLastByteCount { get; set; }
    public required Dictionary<byte[], byte[]> Codes { get; set; }
    public required ShannonFanoParserNode ParserTree { get; set; }
    
}
