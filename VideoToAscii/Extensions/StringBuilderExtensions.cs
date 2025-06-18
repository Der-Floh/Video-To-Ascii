using System.Text;

namespace VideoToAscii.Extensions;

public static class StringBuilderExtensions
{
    /// <summary>
    /// Removes all leading occurrences of the specified characters.
    /// </summary>
    public static StringBuilder TrimStart(this StringBuilder sb, params char[] trimChars)
    {
        ArgumentNullException.ThrowIfNull(sb);
        if (sb.Length == 0 || trimChars is null || trimChars.Length == 0)
            return sb;

        var i = 0;
        while (i < sb.Length && trimChars.Contains(sb[i]))
        {
            i++;
        }

        if (i > 0)
            sb.Remove(0, i);

        return sb;
    }

    /// <summary>
    /// Removes all trailing occurrences of the specified characters.
    /// </summary>
    public static StringBuilder TrimEnd(this StringBuilder sb, params char[] trimChars)
    {
        ArgumentNullException.ThrowIfNull(sb);
        if (sb.Length == 0 || trimChars is null || trimChars.Length == 0)
            return sb;

        var i = sb.Length - 1;
        while (i >= 0 && trimChars.Contains(sb[i]))
        {
            i--;
        }

        // If we moved, cut the tail off
        if (i < sb.Length - 1)
            sb.Length = i + 1;

        return sb;
    }

    /// <summary>
    /// Removes leading and trailing occurrences of the specified characters.
    /// </summary>
    public static StringBuilder Trim(this StringBuilder sb, params char[] trimChars)
        => sb.TrimEnd(trimChars).TrimStart(trimChars);
}
