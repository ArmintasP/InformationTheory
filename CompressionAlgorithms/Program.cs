using CompressionAlgorithms.ShannonFano;


Directory.CreateDirectory("Resources/output");
foreach (var filePath in Directory.EnumerateFiles("Resources/canterbury"))
{
    var fileName = Path.GetFileName(filePath);
    ShannonFanoEncoder.Encode(filePath, $"Resources/output/{fileName}", wordLength: 400);
}

Directory.CreateDirectory("Resources/decoded");
foreach (var filePath in Directory.EnumerateFiles("Resources/output"))
{
    var fileName = Path.GetFileName(filePath);
    await ShannonFanoDecoder.DecodeAsync(filePath, $"Resources/decoded/{fileName}");
}

//ShannonFanoEncoder.Encode($"Resources/canterbury/aaa.txt", $"Resources/output/aaa.txt", wordLength: 8);
//await ShannonFanoDecoder.DecodeAsync($"Resources/output/aaa.txt", $"Resources/decoded/aaa.txt");