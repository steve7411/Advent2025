namespace Advent2025;

public static class StreamExtensions {
    public static void SeekToLine(this Stream stream, int lineIndex, int lineLength) {
        stream.Seek(0, SeekOrigin.Begin);
        //Read past byte order mark (BOM), if present
        while (stream.Position < stream.Length & !IsPrintableAsciiCharacter(stream.ReadByte())) ;
        var bytesPerLine = lineLength + Environment.NewLine.Length;
        stream.Seek(stream.Position - 1 + lineIndex * bytesPerLine, SeekOrigin.Begin);
    }

    public static (int charsInLine, int linesInFile) GetLineInfoForRegularFile(this Stream stream) {
        long startingPosition = stream.Position;
        var (lineLength, firstCharPosition) = stream.GetRemainingCharacterCountInLine();
        stream.Seek(-1, SeekOrigin.End);
        var endsInNewLine = stream.ReadByte() == Environment.NewLine[^1];
        stream.Seek(startingPosition, SeekOrigin.Begin);
        var beginBytes = (int)firstCharPosition;
        return (lineLength, (int)(stream.Length - beginBytes + (endsInNewLine ? 0 : Environment.NewLine.Length)) / (lineLength + Environment.NewLine.Length));
    }

    public static (int lineLength, long firstCharacterPosition) GetRemainingCharacterCountInLine(this Stream stream) {
        long startingPosition = stream.Position;
        stream.Seek(0, SeekOrigin.Begin);

        //Read past byte order mark (BOM), if present
        while (stream.Position < stream.Length && !IsPrintableAsciiCharacter(stream.ReadByte())) ;
        var firstCharPosition = stream.Position - 1;
        int lastChar;
        while ((lastChar = stream.ReadByte()) is not (-1 or '\r' or '\n')) ;
        var foundNewLine = lastChar != -1;
        //Read to end of newline sequence if it's more than one byte
        if (lastChar == Environment.NewLine[0]) {
            var newLineIndex = 1;
            while (stream.Position < stream.Length && (lastChar = stream.ReadByte()) != -1 && newLineIndex < Environment.NewLine.Length && lastChar == Environment.NewLine[newLineIndex++]) ;
        }
        var newPosition = stream.Position - (foundNewLine ? 1 : 0);
        var lineLength = (int)(newPosition - firstCharPosition);
        stream.Seek(startingPosition, SeekOrigin.Begin);
        return (lineLength - (foundNewLine ? Environment.NewLine.Length : 0), firstCharPosition);
    }

    private static bool IsPrintableAsciiCharacter(int x) => x is (>= ' ' and <= '~');
}
