using CompressionAlgorithms.LZSS;
using System.Diagnostics;

var s = Stopwatch.StartNew();

Directory.CreateDirectory("Resources/output");
////foreach (var filePath in Directory.EnumerateFiles("Resources/canterbury"))
////{
////    var fileName = Path.GetFileName(filePath);
////    await ShannonFanoEncoder.EncodeAsync(filePath, $"Resources/output/{fileName}", wordLength: 17);
////}


foreach (var filePath in Directory.EnumerateFiles("Resources/canterbury"))
{
    var fileName = Path.GetFileName(filePath);
    await LZSSEncoder.CompressAsync(filePath, $"Resources/output/{fileName}", maxHistoryLength: 12, maxMatchLength: 4, searchDepth: 0);
    //First two parameters are given as degree of 2 (in bits)
}

Console.WriteLine(s.Elapsed);

var s2 = Stopwatch.StartNew();
Directory.CreateDirectory("Resources/decoded");
foreach (var filePath in Directory.EnumerateFiles("Resources/output"))
{
    var fileName = Path.GetFileName(filePath);
    await LZSSDecoder.DecodeAsync(filePath, $"Resources/decoded/{fileName}");
}
Console.WriteLine(s2.Elapsed);
