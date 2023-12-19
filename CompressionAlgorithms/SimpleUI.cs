using CompressionAlgorithms.LZSS;
using CompressionAlgorithms.ShannonFano;
using System.Diagnostics;

namespace CompressionAlgorithms;

public enum CompressionAlgorithm
{
    None = 0,
    ShanonFano,
    LZSS
}
public enum UserAction
{
    None = 0,
    EncodeFile,
    DecodeFile,
    UseProvidedExamples,
    ChangeAlgorithm,
    Exit
}

internal class SimpleUI
{
    public static CompressionAlgorithm GetChoosenAlgorithmNumber()
    {
        Console.WriteLine("Choose compresion algorithm: ");
        Console.WriteLine("\t1 - Shanon-Fano");
        Console.WriteLine("\t2 - LZSS\n");

        int choice = GetValidInteger();
        while (choice != 1 && choice != 2)
        {
            Console.WriteLine("Enter 1 or 2.");
            choice = GetValidInteger();
        }
        return (CompressionAlgorithm)choice;
    }

    public static int GetActionNumber()
    {
        Console.WriteLine("\n\nChoose action:");
        Console.WriteLine("\t 1 - Encode file\n" +
                          "\t 2 - Decode file\n" +
                          "\t 3 - Use provided examples\n" +
                          "\t 4 - Change algorithm\n" +
                          "\t 5 - Finish work.");
        return GetValidInteger();
    }


    public static async Task EncodeFile(CompressionAlgorithm algorithm)
    {
        Directory.CreateDirectory("Resources/output");
        var fileName = GetFileName(presentInDirectory: "Resources/input/");

        if(algorithm == CompressionAlgorithm.ShanonFano)
        {
            var wordLength = GetShanonFanoParameter();

            var time = Stopwatch.StartNew();
            await ShannonFanoEncoder.EncodeAsync($"Resources/input/{fileName}", $"Resources/output/{fileName}", wordLength);
            Console.WriteLine("Result is in Recources/output directory. Encoding took: " + time.Elapsed);
        }
       else
        {
            var parameters = GetLZSSParameters();
            var time = Stopwatch.StartNew();
            await LZSSEncoder.CompressAsync($"Resources/input/{fileName}", $"Resources/output/{fileName}", 
                                             maxHistoryLength: parameters.Item1, maxMatchLength: parameters.Item2, searchDepth: parameters.Item3);

            Console.WriteLine("Result is in Recources/output directory. Compression took: " + time.Elapsed);
        }
    }
    public static async Task DecodeFile(CompressionAlgorithm algorithm)
    {
        Directory.CreateDirectory("Resources/decoded");
        var fileName = GetFileName(presentInDirectory: "Resources/output/");

        var time = Stopwatch.StartNew();
        if(algorithm == CompressionAlgorithm.ShanonFano)
        {
            await ShannonFanoDecoder.DecodeAsync($"Resources/output/{fileName}", $"Resources/decoded/{fileName}");
        }
        else
        {
            await LZSSDecoder.DecodeAsync($"Resources/output/{fileName}", $"Resources/decoded/{fileName}");
        }
        Console.WriteLine("Result is in Recources/decoded directory. Decoding took: " + time.Elapsed);
    }

    public static async Task UseProvidedExamples(CompressionAlgorithm algorithm)
    {
        Console.WriteLine("Encoding and decoding examples provided in Resources/caterbury directory...");

        if (Directory.Exists("Resources/output"))
            Directory.Delete("Resources/output", true);

        var wordLength = 0;
        var parameters = (0, 0, 0);

        if (algorithm == CompressionAlgorithm.ShanonFano)
            wordLength = GetShanonFanoParameter();
        else
            parameters = GetLZSSParameters();

        var time = Stopwatch.StartNew();

        Directory.CreateDirectory("Resources/output");
        foreach (var filePath in Directory.EnumerateFiles("Resources/canterbury"))
        {
            var fileName = Path.GetFileName(filePath);

            if(algorithm == CompressionAlgorithm.ShanonFano)
                await ShannonFanoEncoder.EncodeAsync(filePath, $"Resources/output/{fileName}", wordLength);
            else
                await LZSSEncoder.CompressAsync(filePath, $"Resources/output/{fileName}",
                                              maxHistoryLength: parameters.Item1, maxMatchLength: parameters.Item2, searchDepth: parameters.Item3);
        }
        Console.WriteLine("Encoding took: " + time.Elapsed);

        if (Directory.Exists("Resources/decoded"))
            Directory.Delete("Resources/decoded", true);

        time = Stopwatch.StartNew();
        Directory.CreateDirectory("Resources/decoded");
        foreach (var filePath in Directory.EnumerateFiles("Resources/output"))
        {
            var fileName = Path.GetFileName(filePath);

            if(algorithm == CompressionAlgorithm.ShanonFano)
                await ShannonFanoDecoder.DecodeAsync(filePath, $"Resources/decoded/{fileName}");
            else
                await LZSSDecoder.DecodeAsync(filePath, $"Resources/decoded/{fileName}");
        }
        Console.WriteLine("Decoding took: " + time.Elapsed);
       
        Console.WriteLine("Results are in Resources/output/ and Resources/decoded/ directories.\n");
    }

    public static int GetShanonFanoParameter()
    {
        Console.Write("Provide word length (in bits) parameter for Shanon-Fano encoder: ");
       
        return GetValidInteger();
    }

    public static (int,int,int) GetLZSSParameters()
    {
        Console.Write("Provide max history length as degree of base 2: ");
        var maxHistoryLength  = GetValidInteger();

        Console.Write("Provide max match length as degree of base 2: ");
        var maxMatchLength = GetValidInteger();

        Console.WriteLine("Choose search depth: ");
        Console.WriteLine("\t 0 - search for longest match\n" +
                          "\t 1 - take first match found\n" +
                          "\t n > 1 - find n matches and take longest.\n");

        var searchDepth = GetValidInteger();

        return (maxHistoryLength, maxMatchLength, searchDepth);
    }

    public static string? GetFileName(string presentInDirectory)
    {
        Console.WriteLine($"Provide filename: (file must be present in {presentInDirectory} directory)");
        var input = Console.ReadLine();
        
        while(! File.Exists(presentInDirectory + input) ) 
        {
            Console.WriteLine("File not found. Check if filename is correct and provide filename again");
            input = Console.ReadLine();
        }
        return input;
    }

    private static int GetValidInteger()
    {
        var input = Console.ReadLine();
        int goodInput;
        while (!int.TryParse(input, out goodInput))
        {
            Console.Write("Invalid input. Please enter an integer value: ");
            input = Console.ReadLine();
        }
        return goodInput;
    }
}
