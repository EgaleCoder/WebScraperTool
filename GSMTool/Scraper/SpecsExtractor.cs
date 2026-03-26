using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GSMTool.Models;
using HtmlAgilityPack;

namespace GSMTool.Scraper
{
    /// <summary>
    /// Parses the GSMArena specs-list tables into a MongoDB-friendly <see cref="DeviceSpecs"/>
    /// document and serialises it to JSON.
    ///
    /// Output shape (queryable with dot-notation in MongoDB):
    ///   db.devices.find({ "specs.network.technology": /5G/ })
    ///   db.devices.find({ "specs.display.size": /6\.9/ })
    /// </summary>
    public static class SpecsExtractor
    {
        // ── Public entry point ────────────────────────────────────────────────

        /// <summary>
        /// Extracts all spec tables from <paramref name="doc"/> and returns a
        /// pretty-printed JSON string, or <c>null</c> with an error message on failure.
        /// </summary>
        public static (string? json, string message) ExtractToJson(
            HtmlDocument doc, string url, string? deviceName)
        {
            var specsDiv = doc.DocumentNode.SelectSingleNode("//div[@id='specs-list']");

            if (specsDiv == null)
                return (null, "No specs-list found on this page.");

            var tables = specsDiv.SelectNodes(".//table");
            if (tables == null || tables.Count == 0)
                return (null, "No specification tables found inside specs-list.");

            var deviceSpecs = new DeviceSpecs
            {
                Url        = url,
                DeviceName = deviceName
            };

            foreach (var table in tables)
            {
                var categoryHeader = table.SelectSingleNode(".//th");
                if (categoryHeader == null) continue;

                // camelCase the category name so it becomes a clean object key
                string rawCategory  = categoryHeader.InnerText.Trim();
                string categoryKey  = ToCamelCase(rawCategory);

                // Each category is a flat { fieldName: value } object
                var categoryDict = new Dictionary<string, string>();

                var specRows = table.SelectNodes(".//tr[td[@class='ttl']]");
                if (specRows != null)
                {
                    string lastKey = string.Empty;

                    foreach (var row in specRows)
                    {
                        var nameCell  = row.SelectSingleNode(".//td[@class='ttl']");
                        var valueCell = row.SelectSingleNode(".//td[@class='nfo']");

                        if (nameCell == null || valueCell == null) continue;

                        // ── Build the field key ───────────────────────────────
                        string rawName = System.Net.WebUtility.HtmlDecode(
                                            nameCell.InnerText.Trim());
                        string fieldKey = ToCamelCase(rawName);

                        // Continuation rows (empty name cell) reuse the previous key
                        if (string.IsNullOrWhiteSpace(fieldKey))
                            fieldKey = !string.IsNullOrEmpty(lastKey) ? lastKey : categoryKey;
                        else
                            lastKey = fieldKey;

                        // ── Clean the value ───────────────────────────────────
                        string specValue = Regex.Replace(
                            System.Net.WebUtility.HtmlDecode(valueCell.InnerText.Trim()),
                            @"\s+", " ").Trim();

                        // ── Insert / append duplicate keys ────────────────────
                        // GSMArena sometimes has multiple rows with the same label
                        // (e.g. several "bands" rows).  Joining with " / " keeps all
                        // values in one field and avoids overwriting data silently.
                        if (categoryDict.TryGetValue(fieldKey, out string? existing))
                            categoryDict[fieldKey] = existing + " / " + specValue;
                        else
                            categoryDict[fieldKey] = specValue;
                    }
                }

                if (categoryDict.Count > 0)
                    deviceSpecs.Specs[categoryKey] = categoryDict;
            }

            if (deviceSpecs.Specs.Count == 0)
                return (null, "No specifications found in specs-list.");

            return (deviceSpecs.ToJson(), $"Extracted {deviceSpecs.Specs.Count} categories.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Converts an arbitrary spec label to camelCase.
        /// Rules:
        ///   - Strip non-alphanumeric characters (except spaces/underscores used as word separators).
        ///   - If the original label contains a digit it is prefixed with "band_" so the key
        ///     never starts with a number (invalid in most query contexts).
        ///   - First word is all-lowercase; subsequent words are Title-cased.
        /// </summary>
        private static string ToCamelCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Decode any remaining HTML entities
            string decoded = System.Net.WebUtility.HtmlDecode(input);

            // If the label starts with a digit, prefix it
            bool startsWithDigit = decoded.Length > 0 && char.IsDigit(decoded[0]);

            // Split into words (drop punctuation, keep alphanumeric + spaces/underscores)
            var words = Regex.Replace(decoded, @"[^\w\s]", "")
                             .Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return string.Empty;

            string camel = string.Concat(
                words.Select((w, i) =>
                    i == 0
                        ? w.ToLowerInvariant()
                        : char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant()));

            // Prefix keys that lead with a digit
            if (startsWithDigit)
                camel = "band_" + camel;

            return camel;
        }
    }
}
