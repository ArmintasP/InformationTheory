using CompressionAlgorithms.BitStream;

namespace CompressionAlgorithms.ShannonFano;

public sealed class ShannonFanoParserTree
{
    private ShannonFanoParserTree? _left = null;
    private ShannonFanoParserTree? _right = null;

    private byte[]? _word;

    public ShannonFanoParserTree(Dictionary<byte[], byte[]> codes)
    {
        ConstructTree(codes);
    }
    
    public ShannonFanoParserTree(Stream stream, int wordLength)
    {
        ConstructTree(stream, wordLength);
    }

    private ShannonFanoParserTree() { }

    public byte[]? GetWord(byte[] text, int offset, ref int index)
    {
        var currentNode = this;
        
        while (currentNode._word is null)
        {
            if (offset + index >= text.Length)
                return null;

            var bit = text[offset + index];
            index++;

            if (bit is 0)
                currentNode = currentNode._left!;
            else if (bit is 1)
                currentNode = currentNode._right!;
            else
                throw new InvalidDataException($"'{bit}' is not 0 or 1");
        }
     
        return currentNode._word;
    }

    private void ConstructTree(Dictionary<byte[], byte[]> codes)
    {
        var currentNode = this;

        foreach (var (word, code) in codes)
        {
            foreach (var bit in code)
            {
                if (bit is 0)
                {
                    currentNode._left = currentNode._left is null ? new ShannonFanoParserTree() : currentNode._left;
                    currentNode = currentNode._left;
                }
                else if (bit is 1)
                {
                    currentNode._right = currentNode._right is null ? new ShannonFanoParserTree() : currentNode._right;
                    currentNode = currentNode._right;
                }
                else
                    throw new InvalidDataException($"'{bit}' is not 0 or 1");
            }
            currentNode._word = word;
            currentNode = this;
        }
    }

    private void ConstructTree(Stream stream, int wordLength)
    {
        var stack = new Stack<ShannonFanoParserTree>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();

            var bit = stream.ReadByte();

            if (bit is 1)
            {
                var buffer = new byte[wordLength];
                stream.ReadExactly(buffer);
                currentNode._word = buffer;
            }
            else if (bit is 0)
            {
                currentNode._left = new ShannonFanoParserTree();
                currentNode._right = new ShannonFanoParserTree();

                stack.Push(currentNode._right);
                stack.Push(currentNode._left);
            }
        }

        // Means there is only one node, "0" -> someWord.
        if (_word is not null)
        {
            _left = new ShannonFanoParserTree
            {
                _word = _word
            };

            _word = null;
        }
    }

    public byte[] ConstructTreeHeader()
    {
        var header = new List<byte>();
        var startingNode = this;

        if (_word is null && _right is null && _left is not null)
            startingNode = _left;

        DepthFirstSearch(startingNode, node =>
        {
            if (node._word is not null)
            {
                header.Add(1);
                header.AddRange(node._word);
            }
            else
            {
                header.Add(0);
            }
        });

        return header.ToArray();
    }

    private static void DepthFirstSearch(ShannonFanoParserTree node, Action<ShannonFanoParserTree> callBack)
    {
        var stack = new Stack<ShannonFanoParserTree>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            node = stack.Pop();

            callBack(node);

            if (node._right is not null)
                stack.Push(node._right);
            if (node._left is not null)
                stack.Push(node._left);
        }
    }
}
