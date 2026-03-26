using System;
using System.Net.Http;
using System.Threading.Tasks;
using GSMTool.Utils;
using HtmlAgilityPack;

namespace GSMTool.Scraper
{
    public class HtmlScraper
    {
        private readonly HttpClient _httpClient;

        public HtmlScraper()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _httpClient.DefaultRequestHeaders.Add(
               "User-Agent",
               "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
               "AppleWebKit/537.36 (KHTML, like Gecko) " +
               "Chrome/124.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add(
               "Accept-Language", "en-US,en;q=0.9");
        }

        /* ──────────────────────────────────────────────────────────
         * Fetch URL once, then extract BOTH the rendered HTML snippet
         * and the clean plain-text from the same div[@id].
         * Returns (null, errorMessage) when the div is not found.
         * ────────────────────────────────────────────────────────── */
        public async Task<(string? divHtml, string text)> GetDivHtmlAndText(
            string url, string id)
        {
            try
            {
                var rawHtml = await _httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(rawHtml);

                var node = doc.DocumentNode.SelectSingleNode($"//div[@id='{id}']");

                if (node == null)
                    return (null, $"Element <div id=\"{id}\"> was not found on the page.");

                // ── Build styled preview HTML ──────────────────────
                var head = doc.DocumentNode.SelectSingleNode("//head");
                string headHtml = head?.InnerHtml ?? string.Empty;
                string baseTag = $"<base href='{url}' />";

                // ── Include all external scripts (for better rendering) ──

                var scriptNodes = doc.DocumentNode.SelectNodes("//script");

                string allScripts = "";
                if (scriptNodes != null)
                {
                    foreach (var script in scriptNodes)
                    {
                        string src = script.GetAttributeValue("src", null);
                        if (src != null)
                        {
                            // prepend https if needed
                            if (src.StartsWith("//"))
                                src = "https:" + src;

                            allScripts += $"<script src='{src}'></script>\n";
                        }
                    }
                }

                string divHtml =
                    "<html><head>" +
                    baseTag +
                    headHtml +
                    allScripts +
                    "</head><body>" +
                    node.OuterHtml +
                    "</body></html>";

                // ── Extract clean plain text via TextHelper (Utils) ──
                string text = TextHelper.ExtractCleanText(node);

                return (divHtml, text);
            }
            catch (TaskCanceledException)
            {
                return (null, "Request timed out. The server took too long to respond.");
            }
            catch (HttpRequestException ex)
            {
                return (null, $"HTTP error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (null, $"Unexpected error: {ex.Message}");
            }
        }

        /* ──────────────────────────────────────────────────────────
         * Load raw HTML string for a URL (simple fetch)
         * ────────────────────────────────────────────────────────── */
        public async Task<string> LoadHtml(string url)
        {
            try
            {
                return await _httpClient.GetStringAsync(url);
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /* ──────────────────────────────────────────────────────────
         * Get the page <title>
         * ────────────────────────────────────────────────────────── */
        public async Task<string> GetTitle(string url)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                return titleNode?.InnerText?.Trim() ?? "Title not found";
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

        /* ──────────────────────────────────────────────────────────
         * Fetch the page, extract the specs-list table and return it
         * as a formatted JSON string.
         * Returns (null, errorMessage) on failure.
         * ────────────────────────────────────────────────────────── */
        public async Task<(string? json, string message)> GetSpecsJson(string url)
        {
            try
            {
                var rawHtml = await _httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(rawHtml);

                // Use the page <title> as the device name (strip " - Full phone specifications" suffix)
                var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                string? deviceName = titleNode?.InnerText?.Trim();
                if (!string.IsNullOrEmpty(deviceName))
                {
                    int dashIdx = deviceName.LastIndexOf(" - ", StringComparison.Ordinal);
                    if (dashIdx > 0)
                        deviceName = deviceName.Substring(0, dashIdx).Trim();
                }

                return SpecsExtractor.ExtractToJson(doc, url, deviceName);
            }
            catch (TaskCanceledException)
            {
                return (null, "Request timed out. The server took too long to respond.");
            }
            catch (HttpRequestException ex)
            {
                return (null, $"HTTP error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (null, $"Unexpected error: {ex.Message}");
            }
        }
    }
}
