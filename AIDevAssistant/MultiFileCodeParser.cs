using System.Collections.Generic;
using System.Text.RegularExpressions;

// 负责把 AI 返回结果拆成多个文件块
public static class MultiFileCodeParser
{
    // 匹配格式：
    // ===FILE: ShopPanel.cs===
    // ...代码...
    private static readonly Regex FileBlockRegex = new Regex(
        @"===FILE:\s*(.+?\.cs)\s*===\s*([\s\S]*?)(?=(===FILE:\s*.+?\.cs\s*===)|\z)",
        RegexOptions.Compiled);

    public static List<GeneratedCodeFile> Parse(string rawText)
    {
        List<GeneratedCodeFile> result = new List<GeneratedCodeFile>();

        if (string.IsNullOrWhiteSpace(rawText))
        {
            return result;
        }

        string text = DeepSeekService.StripCodeFence(rawText).Trim();
        MatchCollection matches = FileBlockRegex.Matches(text);

        foreach (Match match in matches)
        {
            if (!match.Success || match.Groups.Count < 3)
            {
                continue;
            }

            string fileName = match.Groups[1].Value.Trim();
            string content = match.Groups[2].Value.Trim();

            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            result.Add(new GeneratedCodeFile(fileName, content));
        }

        return result;
    }

    public static bool LooksLikeFileBlockFormat(string rawText)
    {
        return !string.IsNullOrWhiteSpace(rawText) && rawText.Contains("===FILE:");
    }
}