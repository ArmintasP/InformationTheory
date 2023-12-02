using CompressionAlgorithms.ShannonFano;


Directory.CreateDirectory("Resources/output");
foreach (var filePath in Directory.EnumerateFiles("Resources/canterbury"))
{
    var fileName = Path.GetFileName(filePath);
    ShannonFanoEncoder.Encode(filePath, $"Resources/output/{fileName}", wordLength: 17);
}

Directory.CreateDirectory("Resources/decoded");
foreach (var filePath in Directory.EnumerateFiles("Resources/output"))
{
    var fileName = Path.GetFileName(filePath);
    await ShannonFanoDecoder.DecodeAsync(filePath, $"Resources/decoded/{fileName}");
}

//using CompressionAlgorithms.ShannonFano;

//ShannonFanoEncoder.Encode($"Resources/canterbury/a.txt", $"Resources/output/a.txt", wordLength: 7);
//await ShannonFanoDecoder.DecodeAsync($"Resources/output/a.txt", $"Resources/decoded/a.txt");
