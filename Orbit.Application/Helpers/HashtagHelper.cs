using System.Text.RegularExpressions;

namespace Orbit.Application.Helpers;

public static partial class HashtagHelper
{
    [GeneratedRegex(@"#(\w+)")]
    private static partial Regex HashtagPattern();

    public static HashSet<string> ExtractHashtags(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return [];

        var tags = new HashSet<string>();

        foreach (Match match in HashtagPattern().Matches(content))
        {
            var tag = match.Groups[1].Value.ToLowerInvariant();
            if (tag.Length > 0 && tag.Length <= 100 && tag.Any(char.IsLetter))
                tags.Add(tag);
        }

        return tags;
    }
}
