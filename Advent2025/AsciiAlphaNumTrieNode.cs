using System.Diagnostics;

namespace Advent2025;

// CASE INSENSITIVE!!!
public struct AsciiAlphaNumTrieNode {
    private const int DIGITS_START = 27;

    private readonly AsciiAlphaNumTrieNode[] children;
    private bool isEnd = false;
    
    private readonly bool IsInitialized => children != null;

    private static int GetChildIndex(char ch) => char.IsAsciiDigit(ch) ? DIGITS_START + (ch & 0xF) : (ch & 0x1F);

    public AsciiAlphaNumTrieNode() => children = new AsciiAlphaNumTrieNode[1 + 26 + 10];

    public AsciiAlphaNumTrieNode(IEnumerable<string> strings) : this() {
        foreach (var s in strings)
            Add(s);
    }

    private readonly ref AsciiAlphaNumTrieNode GetNextNode(in ReadOnlySpan<char> value) {
        ref var next = ref children[GetChildIndex(value[0])];
        if (!next.IsInitialized)
            next = new();
        return ref next;
    }

    public void Add(ReadOnlySpan<char> value) {
        ArgumentOutOfRangeException.ThrowIfZero(value.Length);
        PrivateAdd(value);
    }
    private void PrivateAdd(in ReadOnlySpan<char> value) {
        Debug.Assert(IsInitialized, $"{nameof(AsciiAlphaNumTrieNode)} has not been initialized");
        if (value.Length == 0) {
            isEnd = true;
            return;
        }
        GetNextNode(value).PrivateAdd(value[1..]);
    }

    public readonly bool Contains(in ReadOnlySpan<char> value) {
        Debug.Assert(IsInitialized, $"{nameof(AsciiAlphaNumTrieNode)} has not been initialized");
        if (value.Length == 0)
            return isEnd;
        
        ref var next = ref children[GetChildIndex(value[0])];
        return next.IsInitialized && next.Contains(value[1..]);
    }

    public readonly int Match(ReadOnlySpan<char> chars) {
        ref readonly var node = ref this;
        var i = 0;
        for (; i < chars.Length && !node.isEnd; ++i) {
            node = ref node.children[GetChildIndex(chars[i])];
            if (!node.IsInitialized)
                return 0;
        }
        return node.isEnd ? i : 0;
    }
}
