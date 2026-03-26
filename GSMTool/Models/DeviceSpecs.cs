using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GSMTool.Models
{
    /// <summary>
    /// Root document stored in MongoDB.
    ///
    /// Shape:
    /// {
    ///   "deviceName": "Apple iPhone 17 Pro Max",
    ///   "url":        "https://www.gsmarena.com/...",
    ///   "scrapedAt":  "2026-03-27T04:20:40Z",
    ///   "specs": {
    ///     "network": {
    ///       "technology": "GSM / HSPA / LTE / 5G",
    ///       "net_2gBands": "GSM 850 / 900 / 1800 / 1900"
    ///     },
    ///     "display": { ... }
    ///   }
    /// }
    ///
    /// This flat nested-object layout lets MongoDB queries use simple dot-notation:
    ///   db.devices.find({ "specs.network.technology": /5G/ })
    /// </summary>
    public class DeviceSpecs
    {
        /// <summary>Human-readable device name derived from the page &lt;title&gt;.</summary>
        [JsonPropertyName("deviceName")]
        public string? DeviceName { get; set; }

        /// <summary>Source URL of the scraped page.</summary>
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        /// <summary>UTC timestamp when the data was scraped (ISO-8601).</summary>
        [JsonPropertyName("scrapedAt")]
        public string ScrapedAt { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        /// <summary>
        /// All device specifications.
        /// Outer key  = camelCase category name  (e.g. "network", "display").
        /// Inner key  = camelCase spec field name (e.g. "technology", "size").
        /// Inner value = cleaned spec value string.
        /// Multiple continuation rows for the same key are joined with " / ".
        /// </summary>
        [JsonPropertyName("specs")]
        public Dictionary<string, Dictionary<string, string>> Specs { get; set; } = new();

        /// <summary>Serialises the document to indented JSON ready for mongoimport.</summary>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                // Preserve the casing we set via JsonPropertyName
                PropertyNamingPolicy = null
            };
            return JsonSerializer.Serialize(this, options);
        }
    }
}
