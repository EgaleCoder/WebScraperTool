using GSMTool.Scraper;
using System.Windows;

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
        }

        private async void LoadHtml_Click(object sender, RoutedEventArgs e)
        {

            /* Load HTML */

            //string url = UrlTextBox.Text;
            //var html = await _scraper.LoadHtml(url);
            //OutputTextBox.Text = html;

            /* Load HTML */
            //string url = UrlTextBox.Text;
            //var title = await _scraper.GetTitle(url);
            //OutputTextBox.Text = title;


            /* Load Div */
            string url = UrlTextBox.Text;

            var text = await _scraper.GetDivText(url, "specs-list");

            OutputTextBox.Text = text;
        }
    }
}