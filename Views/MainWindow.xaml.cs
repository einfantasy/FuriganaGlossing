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
using System.Collections.Specialized;
using Microsoft.Extensions.DependencyInjection;

namespace FuriganaGlossing.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();
            DataContext = _viewModel;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.KeyDown += OnKeyDown;
            
            ((INotifyCollectionChanged)_viewModel.Logs).CollectionChanged += OnLogsCollectionChanged;
            
            InitializeWebView();
        }

        private void OnLogsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (LogListBox != null && e.NewItems != null && e.NewItems.Count > 0)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LogListBox.ScrollIntoView(e.NewItems[0]);
                    }));
                }
            }
        }

        private async void InitializeWebView()
        {
            try
            {
                await ResultWebView.EnsureCoreWebView2Async();
                ResultWebView.CoreWebView2.Profile.PreferredColorScheme = Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Light;
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

        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
