using CompressionAlgorithms;

Console.WriteLine("Choose compresion algorithm: ");
Console.WriteLine("\t1 - Shanon-Fano\n");

bool continueWork = true;
while (continueWork)
{
    var actionNum = SimpleUI.GetActionNumber();

    switch(actionNum)
    {
        case 1:
            await SimpleUI.EncodeFileShanonFano();
            break;
        case 2:
            await SimpleUI.DecodeFileShanonFano();
            break;
        case 3:
            await SimpleUI.UseProvidedExamples();
            break;          
        case 4:
            continueWork = false;
            break;
        default:
            continue;   
    }
}
