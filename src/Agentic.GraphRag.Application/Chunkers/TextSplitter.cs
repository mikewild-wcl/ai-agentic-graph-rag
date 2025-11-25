using System.Text.RegularExpressions;

namespace Agentic.GraphRag.Application.Chunkers;

public static class TextSplitter
{
    public static string[] SplitTextByTitles(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        /* 
         Match lines starting with one or more digits,
         an optional uppercase letter, followed by a dot, a space, and then up to 60 characters.
         Allow spaces before the number and newlines before the title.
        */
        var titlePattern = new Regex(@"\n*\s*\d+[A-Za-z]?\.\s{1,3}.{0,60}\n", RegexOptions.Multiline);
        var titles = titlePattern.Matches(text);

        // Split the text at these titles
        var sections = titlePattern.Split(text);
        var sectionsWithTitles = new List<string>();

        if (sections.Length > 0 && sections[0].Length > 0 && !string.IsNullOrWhiteSpace(sections[0]))
        {
            sectionsWithTitles.Add(sections[0].Trim());
        }

        for (int i = 1; i <= titles.Count; i++)
        {
            var sectionText = titles[i - 1]?.ToString().Trim() + "\n" + sections[i]?.Trim();
            sectionsWithTitles.Add(sectionText);
        }

        return [.. sectionsWithTitles];
    }
}
