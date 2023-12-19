using CompressionAlgorithms;

var compressionAlgorithm = SimpleUI.GetChoosenAlgorithmNumber(); 

bool continueWork = true;
while (continueWork)
{
   

    var actionNum = (UserAction)SimpleUI.GetActionNumber();

    switch (actionNum)
    {
        case UserAction.EncodeFile:
            await SimpleUI.EncodeFile(compressionAlgorithm);
            break;
        case UserAction.DecodeFile:
            await SimpleUI.DecodeFile(compressionAlgorithm);
            break;
        case UserAction.UseProvidedExamples:
            await SimpleUI.UseProvidedExamples(compressionAlgorithm);
            break;
        case UserAction.ChangeAlgorithm:
            compressionAlgorithm = SimpleUI.GetChoosenAlgorithmNumber();
            break;
        case UserAction.Exit:
            continueWork = false;
            break;
        default:
            continue;
    }
}
