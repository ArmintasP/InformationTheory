using CompressionAlgorithms.ShannonFano;
using System.Diagnostics;

namespace CompressionAlgorithms;

internal class SimpleUI
{
    public static int GetActionNumber()
    {
        Console.WriteLine("\n\nChoose action:");
        Console.WriteLine("\t1 - Encode file\n" +
                          "\t2 - Decode file\n" +
                          "\t3 - Use provided examples\n" +
                          "\t4 - Finish work.\n");
        return GetValidInteger();
    }


    public static async Task EncodeFileShanonFano()
    {
        Directory.CreateDirectory("Resources/output");

        var fileName = GetFileName("Resources/input/");
        var wordLength = GetShanonFanoParameter();

        var time = Stopwatch.StartNew();
        await ShannonFanoEncoder.EncodeAsync($"Resources/input/{fileName}", $"Resources/output/{fileName}", wordLength);
        Console.WriteLine("Result is in Recources/output directory. Encoding took: " + time.Elapsed);
    }
    public static async Task DecodeFileShanonFano()
    {
        Directory.CreateDirectory("Resources/decoded");
        var fileName = GetFileName("Resources/output/");

        var time = Stopwatch.StartNew();
        await ShannonFanoDecoder.DecodeAsync($"Resources/output/{fileName}", $"Resources/decoded/{fileName}");
        Console.WriteLine("Result is in Recources/decoded directory. Decoding took: " + time.Elapsed);
    }

    public static async Task UseProvidedExamples()
    {
        Console.WriteLine("Encoding and decoding examples provided in Resources/caterbury directory...");
        var wordLength = GetShanonFanoParameter();
        var time = Stopwatch.StartNew();
        Directory.CreateDirectory("Resources/output");
        foreach (var filePath in Directory.EnumerateFiles("Resources/canterbury"))
        {
            var fileName = Path.GetFileName(filePath);
            await ShannonFanoEncoder.EncodeAsync(filePath, $"Resources/output/{fileName}", wordLength);
        }
        Console.WriteLine("Encoding took: " + time.Elapsed);

        time = Stopwatch.StartNew();
        Directory.CreateDirectory("Resources/decoded");
        foreach (var filePath in Directory.EnumerateFiles("Resources/output"))
        {
            var fileName = Path.GetFileName(filePath);
            await ShannonFanoDecoder.DecodeAsync(filePath, $"Resources/decoded/{fileName}");
        }
        Console.WriteLine("Decoding took: " + time.Elapsed);

        Console.WriteLine("Results are in Resources/output/ and Resources/decoded/ directories.\n");
    }

    public static int GetShanonFanoParameter()
    {
        Console.Write("Provide word length (in bits) parameter for Shanon-Fano encoder: ");
       
        return GetValidInteger();
    }

    public static string? GetFileName(string directory)
    {
        Console.WriteLine($"Provide filename: (file must be present in {directory} directory)");
        var input = Console.ReadLine();
        
        while(! File.Exists(directory + input) ) 
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
