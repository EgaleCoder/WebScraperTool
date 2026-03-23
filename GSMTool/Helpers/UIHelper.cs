using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GSMTool.Helpers
{
    /// <summary>
    /// Shared UI helper methods — status bar updates, loading indicator,
    /// and clipboard copy — extracted so MainWindow stays lean.
    /// </summary>
    public static class UIHelper
    {
        // ── Cached brushes (avoids repeated allocations) ────────────

        // Info (blue) set
        private static readonly SolidColorBrush InfoBg     = new(Color.FromRgb(0xEF, 0xF6, 0xFF));
        private static readonly SolidColorBrush InfoBorder = new(Color.FromRgb(0xBF, 0xDB, 0xFE));
        private static readonly SolidColorBrush InfoFg     = new(Color.FromRgb(0x1D, 0x4E, 0xD8));

        // Error (red) set
        private static readonly SolidColorBrush ErrBg     = new(Color.FromRgb(0xFE, 0xF2, 0xF2));
        private static readonly SolidColorBrush ErrBorder = new(Color.FromRgb(0xFC, 0xA5, 0xA5));
        private static readonly SolidColorBrush ErrFg     = new(Color.FromRgb(0x99, 0x1B, 0x1B));

        /// <summary>
        /// Updates the status bar text and changes its colour to reflect
        /// success/info (blue) or error (red).
        /// </summary>
        public static void SetStatus(
            Border statusBar,
            TextBlock statusText,
            string message,
            bool isError)
        {
            statusText.Text       = message;
            statusBar.Background  = isError ? ErrBg     : InfoBg;
            statusBar.BorderBrush = isError ? ErrBorder : InfoBorder;
            statusText.Foreground = isError ? ErrFg     : InfoFg;
        }

        /// <summary>
        /// Shows or hides the indeterminate progress bar.
        /// When showing, also updates the status text to a "loading" message.
        /// </summary>
        public static void SetLoading(
            ProgressBar loadingBar,
            TextBlock statusText,
            bool isLoading)
        {
            loadingBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            if (isLoading)
                statusText.Text = "Fetching specs, please wait...";
        }

        /// <summary>
        /// Copies the contents of a TextBox to the clipboard and updates
        /// the status bar to confirm the action.
        /// </summary>
        public static void CopyToClipboard(
            TextBox outputBox,
            Border statusBar,
            TextBlock statusText)
        {
            if (!string.IsNullOrEmpty(outputBox.Text))
            {
                Clipboard.SetText(outputBox.Text);
                SetStatus(statusBar, statusText, "Specs text copied to clipboard.", isError: false);
            }
        }
    }
}
