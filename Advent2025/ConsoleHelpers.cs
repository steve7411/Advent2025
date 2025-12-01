using System;
namespace Advent2025;

public static class ConsoleHelpers {
    public static void WriteBackgroundColor(ConsoleColor color) {
        var currentGBColor = Console.BackgroundColor;
        var currentFGColor = Console.ForegroundColor;
        Console.BackgroundColor = color;
        Console.ForegroundColor = color;

        char character = color == ConsoleColor.Black ? ' ' : 'X';
        Console.Write(character);
        Console.BackgroundColor = currentGBColor;
        Console.ForegroundColor = currentFGColor;
    }
}
