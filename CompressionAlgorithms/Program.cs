using CompressionAlgorithms.LZ77;
using CompressionAlgorithms.ShannonFano;
using System.Diagnostics;

var s = Stopwatch.StartNew();

Directory.CreateDirectory("Resources/output");
//foreach (var filePath in Directory.EnumerateFiles("Resources/canterbury"))
//{
//    var fileName = Path.GetFileName(filePath);
//    await ShannonFanoEncoder.EncodeAsync(filePath, $"Resources/output/{fileName}", wordLength: 17);
//}

foreach (var filePath in Directory.EnumerateFiles("Resources/easy"))
{
    var fileName = Path.GetFileName(filePath);
    await LZ77Encoder.CompressAsync(filePath, $"Resources/output/{fileName}", maxHistoryLength: 12, maxMatchLength: 4);
    //parameters are given as degree of 2 (in bits)
}

//Directory.CreateDirectory("Resources/decoded");
//foreach (var filePath in Directory.EnumerateFiles("Resources/output"))
//{
//    var fileName = Path.GetFileName(filePath);
//    await ShannonFanoDecoder.DecodeAsync(filePath, $"Resources/decoded/{fileName}");
//}

Console.WriteLine(s.Elapsed);
