using System;
using System.Threading.Tasks;
using System.Windows;
using GSMTool.Helpers;
using GSMTool.Scraper;
using Microsoft.Web.WebView2.Core;

namespace GSMTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string GsmArenaBase = "https://www.gsmarena.com/";
        private const string UrlPlaceholder = "Please Enter GSM Arena Links";
        private const string SpecsDivId = "specs-list";

        // CSS injected after each navigation to isolate the specs div visually
        // while leaving the full DOM (and its JS event listeners) intact.
        private const string IsolateCss = @"
            (function() {
                var style = document.createElement('style');
                style.textContent = [
                    'body > *:not(#specs-list) { display:none!important; }',
                    '#specs-list { display:block!important; }',
                    'html,body { background:#fff!important; margin:0!important; padding:12px!important; font-family:Arial,sans-serif!important; }'
                ].join('');
                document.head.appendChild(style);
            })()";

        private readonly HtmlScraper _scraper;
        private bool _browserReady = false;
        private bool _isScraping = false;

        public MainWindow()
        {
            InitializeComponent();
            _scraper = new HtmlScraper();
            Loaded += MainWindow_Loaded;
        }

        // ─── Initialisation ────────────────────────────────────────

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitBrowser();
            ResetUrlPlaceholder();
        }

        private async Task InitBrowser()
        {
            try
            {
                await BrowserBox.EnsureCoreWebView2Async(null);

                // Match the placeholder background so minimize/restore
                // does not flash a white background over the grey panel.
                BrowserBox.DefaultBackgroundColor = System.Drawing.Color.FromArgb(0xF5, 0xF5, 0xF5);

                // Block pop-ups opened by page scripts
                BrowserBox.CoreWebView2.NewWindowRequested += (s, args) =>
                    args.Handled = true;

                // Only intercept external navigations triggered by clicks inside the WebView
                BrowserBox.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

                _browserReady = true;
            }
            catch (Exception ex)
            {
                UIHelper.SetStatus(StatusBar, StatusText,
                    $"WebView2 init error: {ex.Message}", isError: true);
            }
        }

        private void CoreWebView2_NavigationStarting(
            object? sender,
            CoreWebView2NavigationStartingEventArgs e)
        {
            // If our scraper is active the navigation was triggered by NavigateToString — allow it.
            // Otherwise, block any real HTTP(S) page navigation the user might trigger inside the view.
            if (!_isScraping &&
                (e.Uri.StartsWith("http://") || e.Uri.StartsWith("https://")))
            {
                e.Cancel = true;
            }
        }

        // ─── URL TextBox Placeholder behaviour ────────────────────

        private void ResetUrlPlaceholder()
        {
            UrlTextBox.Text = UrlPlaceholder;
            UrlTextBox.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void UrlTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (UrlTextBox.Text == UrlPlaceholder)
            {
                UrlTextBox.Text = string.Empty;
                UrlTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void UrlTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UrlTextBox.Text))
                ResetUrlPlaceholder();
        }

        // ─── Button Handlers ───────────────────────────────────────

        private async void LoadHtml_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text?.Trim() ?? string.Empty;

            // Treat placeholder text as empty
            if (url == UrlPlaceholder)
                url = string.Empty;

            // ── Validate: must be a GSMArena device page ──
            if (string.IsNullOrWhiteSpace(url))
            {
                ShowGsmArenaOnlyDialog("Please enter a URL before clicking Fetch Specs.");
                return;
            }

            if (!url.StartsWith(GsmArenaBase, StringComparison.OrdinalIgnoreCase))
            {
                ShowGsmArenaOnlyDialog(
                    $"The URL you entered does not belong to GSMArena.\n\n" +
                    $"This tool only works with pages from:\n{GsmArenaBase}\n\n" +
                    $"Example:\nhttps://www.gsmarena.com/apple_iphone_17_pro_max-13964.php");
                return;
            }

            if (!_browserReady)
            {
                UIHelper.SetStatus(StatusBar, StatusText,
                    "Browser is still initialising, please wait...", isError: true);
                return;
            }

            // ── Fetch ──
            UIHelper.SetLoading(LoadingBar, StatusText, true);
            GetButton.IsEnabled = false;

            try
            {
                // ── 1. Fetch the rendered HTML for the WebView preview ──
                _isScraping = true;
                var (divHtml, _) = await _scraper.GetDivHtmlAndText(url, SpecsDivId);
                _isScraping = false;

                if (divHtml == null)
                {
                    UIHelper.SetStatus(StatusBar, StatusText,
                        "Could not find the specs section. Is this a valid GSMArena device page?",
                        isError: true);
                    return;
                }

                // Show WebView preview
                BrowserPlaceholder.Visibility = Visibility.Collapsed;
                BrowserBox.Visibility = Visibility.Visible;
                BrowserBox.NavigateToString(divHtml);

                // ── 2. Extract specs as JSON and display in the text box ──
                var (json, message) = await _scraper.GetSpecsJson(url);

                if (json != null)
                {
                    OutputDataBox.Text = json;
                    UIHelper.SetStatus(StatusBar, StatusText,
                        $"{message} — {url}", isError: false);
                }
                else
                {
                    OutputDataBox.Text = message;
                    UIHelper.SetStatus(StatusBar, StatusText, message, isError: true);
                }
            }
            catch (Exception ex)
            {
                _isScraping = false;
                UIHelper.SetStatus(StatusBar, StatusText,
                    $"Error: {ex.Message}", isError: true);
            }
            finally
            {
                UIHelper.SetLoading(LoadingBar, StatusText, false);
                GetButton.IsEnabled = true;
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            if (_browserReady)
                BrowserBox.NavigateToString(
                    "<html><body style='background:#ffffff'></body></html>");

            BrowserBox.Visibility = Visibility.Collapsed;
            BrowserPlaceholder.Visibility = Visibility.Visible;

            OutputDataBox.Clear();
            ResetUrlPlaceholder();

            UIHelper.SetStatus(StatusBar, StatusText,
                "Ready - enter a GSMArena URL and click Fetch Specs.", isError: false);
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            UIHelper.CopyToClipboard(OutputDataBox, StatusBar, StatusText);
        }

        // ─── Private helpers ───────────────────────────────────────

        /// <summary>
        /// Shows a standard Windows MessageBox explaining that the tool
        /// is GSMArena-only, with the given detail message.
        /// </summary>
        private void ShowGsmArenaOnlyDialog(string detail)
        {
            MessageBox.Show(
                this,
                detail,
                "GSMArena URLs Only",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}