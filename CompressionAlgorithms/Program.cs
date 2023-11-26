using CompressionAlgorithms;

using var a = new FileStream("Resources/video.mp4", FileMode.Open);
using var bitReader = new BitStream(a);

using var c = new FileStream("Resources/video_reconstructed.mp4", FileMode.Create);
using var bitWriter = new BitStream(c);

var buffer = new byte[8000];
int readBytesCount;

while ((readBytesCount = await bitReader.ReadAsync(buffer)) > 0)
{
    await bitWriter.WriteAsync(buffer, 0, readBytesCount);
}