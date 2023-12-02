using CompressionAlgorithms.ShannonFano;
using System.Diagnostics;

var s = Stopwatch.StartNew();

//Directory.CreateDirectory("Resources/output");
//foreach (var filePath in Directory.EnumerateFiles("Resources/canterbury"))
//{
//    var fileName = Path.GetFileName(filePath);
//    ShannonFanoEncoder.Encode(filePath, $"Resources/output/{fileName}", wordLength: 8);
//}

//Directory.CreateDirectory("Resources/decoded");
//foreach (var filePath in Directory.EnumerateFiles("Resources/output"))
//{
//    var fileName = Path.GetFileName(filePath);
//    await ShannonFanoDecoder.DecodeAsync(filePath, $"Resources/decoded/{fileName}");
//}


ShannonFanoEncoder.Encode($"Resources/canterbury/spy.mkv", $"Resources/output/spy.mkv", wordLength: 8);
await ShannonFanoDecoder.DecodeAsync($"Resources/output/spy.mkv", $"Resources/decoded/spy.mkv");

Console.WriteLine(s.Elapsed);
Console.WriteLine(s.Elapsed);
