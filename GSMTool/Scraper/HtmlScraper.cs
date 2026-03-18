using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace GSMTool.Scraper
{
    public class HtmlScraper
    {
        private HttpClient _httpClient;

        public HtmlScraper()
        {
            _httpClient = new HttpClient();

            _httpClient.DefaultRequestHeaders.Add(
               "User-Agent",
               "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
               "AppleWebKit/537.36 (KHTML, like Gecko) " +
               "Chrome/141.0.0.0 Safari/537.36");
        }

        /* To Load HTML */
        public async Task<string> LoadHtml(string url)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);
                return html;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /* To Get Title */
        public async Task<string> GetTitle(string url)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var titleNode = doc.DocumentNode.SelectSingleNode("//title");

                if (titleNode != null)
                {
                    return titleNode.InnerText;
                }

                return "Title not found";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /* To Get Div Text (id) */
        public async Task<string> GetDivText(string url, string id)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var node = doc.DocumentNode.SelectSingleNode($"//div[@id='{id}']");

                if (node != null)
                {
                    return node.InnerText;
                }

                return "Div not found";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /* To Get Div Web Page */
        public async Task<string> GetDivHtml(string url, string id)
        {
            try
            {


                var html = await _httpClient.GetStringAsync(url);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // head
                var head = doc.DocumentNode.SelectSingleNode("//head");

                // div by id
                var node = doc.DocumentNode.SelectSingleNode($"//div[@id='{id}']");

                if (node != null)
                {
                    string baseTag = $"<base href='{url}' />";

                    string headHtml = head != null
                    ? head.InnerHtml
                    : "";

                    string newHtml =
                   "<html><head>" +
                    baseTag +
                    headHtml +
                    "</head><body>" +
                     node.OuterHtml +
                    "</body></html>";

                    return newHtml;
                }

                return "Div not found";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
