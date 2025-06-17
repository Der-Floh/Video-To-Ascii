namespace VideoToAscii.Utils;

/// <summary>
/// Utilities for the console
/// </summary>
public static class ConsoleUtils
{
    /// <summary>
    /// Displays a progress bar in the terminal
    /// </summary>
    public static void DisplayProgress(double percent, string? message = null)
    {
        Console.Write("\u001b[0;0H");
        if (!string.IsNullOrEmpty(message))
            Console.WriteLine(message);
        Console.Write(BuildProgress(percent));
    }

    /// <summary>
    /// Displays a progress bar in the terminal
    /// </summary>
    public static void DisplayProgress(int progress, int total, string? message = null)
    {
        Console.Write("\u001b[0;0H");
        if (!string.IsNullOrEmpty(message))
            Console.WriteLine(message);
        Console.Write(BuildProgress(progress, total));
    }

    private static string BuildProgress(int progress, int total)
    {
        var progressPercent = progress / (double)total * 100;
        return BuildProgress(progressPercent);
    }

    private static string BuildProgress(double progressPercent)
    {
        var adjustedSizePercent = 20.0 / 100 * progressPercent;
        var progressBar = new string('█', (int)adjustedSizePercent) + new string('░', 20 - (int)adjustedSizePercent);
        return $"  |{progressBar}| {Math.Round(progressPercent, 2):F2}%";
    }
}
