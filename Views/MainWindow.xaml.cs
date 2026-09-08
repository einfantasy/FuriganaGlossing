using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using FuriganaGlossing.Services;
using FuriganaGlossing.ViewModels;
using System.ComponentModel;
using System.Threading.Tasks;

namespace FuriganaGlossing.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            var configService = new ConfigService();
            _viewModel = new MainViewModel(configService);
            DataContext = _viewModel;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.KeyDown += OnKeyDown;
            
            InitializeWebView();
        }

        private async void InitializeWebView()
        {
            try
            {
                await ResultWebView.EnsureCoreWebView2Async();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"WebView2 Initialization Error: {ex.Message}");
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                CaptureClipboardImage();
            }
        }

        private void CaptureClipboardImage()
        {
            if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                using (var ms = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(ms);
                    _viewModel.ProcessImageAsync(ms.ToArray());
                }
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ResultHtml))
            {
                UpdateWebView();
            }
        }

        private void UpdateWebView()
        {
            if (ResultWebView != null && ResultWebView.CoreWebView2 != null)
            {
                ResultWebView.CoreWebView2.NavigateToString(_viewModel.ResultHtml ?? "");
            }
        }

        private void RenderOcrResults()
        {
            // No longer used since coordinates are unavailable.
        }
    }
}
