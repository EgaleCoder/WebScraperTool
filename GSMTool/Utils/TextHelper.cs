using HtmlAgilityPack;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace GSMTool.Utils
{
    /// <summary>
    /// Utility methods for converting HTML nodes to clean plain text.
    /// </summary>
    public static class TextHelper
    {
        /// <summary>
        /// Converts an HtmlNode to clean, human-readable plain text.
        /// Decodes HTML entities, collapses whitespace, and removes blank lines.
        /// </summary>
        public static string ExtractCleanText(HtmlNode rootNode)
        {
            var raw = rootNode.InnerText;

            // Decode HTML entities (e.g. &nbsp; → space)
            raw = System.Net.WebUtility.HtmlDecode(raw);

            // Collapse runs of tabs/spaces into a single space
            raw = Regex.Replace(raw, @"[ \t]+", " ");

            // Split on newlines, trim each line, drop blanks
            var lines = raw.Split('\n')
                           .Select(l => l.Trim())
                           .Where(l => l.Length > 0);

            return string.Join(Environment.NewLine, lines);
        }
    }
}
