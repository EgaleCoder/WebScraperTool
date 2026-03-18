using GSMTool.Scraper;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GSMTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HtmlScraper _scraper;
        public MainWindow()
        {
            InitializeComponent();

            _scraper = new HtmlScraper();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitBrowser();
        }

        private async Task InitBrowser()
        {
            await BrowserBox.EnsureCoreWebView2Async(null);

            BrowserBox.NavigateToString(
                "<html><body style='background:white'></body></html>");
        }

        private async void LoadHtml_Click(object sender, RoutedEventArgs e)
        {

            /* Load HTML */

            //string url = UrlTextBox.Text;
            //var html = await _scraper.LoadHtml(url);
            //OutputWebBox.Text = html;

            /* Load HTML */
            //string url = UrlTextBox.Text;
            //var title = await _scraper.GetTitle(url);
            //OutputWebBox.Text = title;


            /* Load Div */

            string url = UrlTextBox.Text;

            if (string.IsNullOrWhiteSpace(url))
                return;

            /* load full page */
            BrowserBox.CoreWebView2.Navigate(url);
            await Task.Delay(2500);

            var text = await _scraper.GetDivText(url, "specs-list");

            // show only specs-list div
            await ShowOnlySpecsDiv();

            OutputDataBox.Text = text;
        }


        // Need to fix 19/03/2026: Show full page for a bit then show only specs-list div and can be replace via getdiv method in scrapper?
        private async Task ShowOnlySpecsDiv()
        {
            string script = @"
                (function(){

                    let div = document.querySelector('#specs-list');

                    if(!div) return;

                    document.body.innerHTML = '';

                    document.body.appendChild(div);

                    document.body.style.background = 'white';

                })();
            ";

            await BrowserBox.ExecuteScriptAsync(script);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            BrowserBox.NavigateToString(
                "<html><body style='background:white'></body></html>");

            OutputDataBox.Clear();

            UrlTextBox.Text = "";
        }

    }
}