using CompressionAlgorithms.BitStream;
using System.Text;

namespace CompressionAlgorithms.ShannonFano;

public sealed class ShannonFanoParserTree
{
    private ShannonFanoParserTree? _left = null;
    private ShannonFanoParserTree? _right = null;

    private string? _word;

    public ShannonFanoParserTree(Dictionary<string, string> codes)
    {
        ConstructTree(codes);
    }

    public ShannonFanoParserTree(string header, int wordLength)
    {
        ConstructTree(header, wordLength);
    }

    public ShannonFanoParserTree(BitReader stream, int wordLength)
    {
        ConstructTree(stream, wordLength);
    }

    private ShannonFanoParserTree() { }

    public string? GetWord(byte[] text, int offset, ref int index)
    {
        if (_word != null)
            return _word;

        if (offset + index >= text.Length)
            return null;

        var bit = text[offset + index];
        index++;

        if (bit is (byte)'0')
            return _left!.GetWord(text, offset, ref index);
        if (bit is (byte)'1')
            return _right!.GetWord(text, offset, ref index);
        else
            throw new InvalidDataException($"'{bit}' is not 0 or 1");
    }

    private void ConstructTree(Dictionary<string, string> codes)
    {
        var currentNode = this;

        foreach (var (word, code) in codes)
        {
            foreach (var bit in code)
            {
                if (bit is '0')
                {
                    currentNode._left = currentNode._left is null ? new ShannonFanoParserTree() : currentNode._left;
                    currentNode = currentNode._left;
                }
                else if (bit is '1')
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

    private void ConstructTree(string header, int wordLength)
    {
        var stack = new Stack<ShannonFanoParserTree>();
        stack.Push(this);

        for (var i = 0; stack.Count > 0; i++)
        {
            var currentNode = stack.Pop();

            var bit = header[i];

            if (bit is '1')
            {
                currentNode._word = header[(1 + i)..(1 + i + wordLength)];
                i += wordLength;
            }
            else if (bit is '0')
            {
                currentNode._left = new ShannonFanoParserTree();
                currentNode._right = new ShannonFanoParserTree();

                stack.Push(currentNode._right);
                stack.Push(currentNode._left);
            }
        }
    }

    private void ConstructTree(Stream stream, int wordLength)
    {
        var buffer = new byte[wordLength];
        
        var stack = new Stack<ShannonFanoParserTree>();
        stack.Push(this);

        while (stack.Count > 0)
        {
            var currentNode = stack.Pop();

            var bit = (char)stream.ReadByte(); 

            if (bit is '1')
            {
                stream.ReadExactly(buffer);
                currentNode._word = buffer.ToString2();
            }
            else if (bit is '0')
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
            _left = new ShannonFanoParserTree();
            _left._word = _word;

            _word = null;
        }
    }

    public string ConstructTreeHeader()
    {
        var header = new StringBuilder();
        var startingNode = this;

        if (_word is null && _right is null && _left is not null)
            startingNode = _left;

        DepthFirstSearch(startingNode, node =>
        {
            if (node._word is not null)
                header.Append('1').Append(node._word);
            else
                header.Append('0');
        });

        return header.ToString();
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
