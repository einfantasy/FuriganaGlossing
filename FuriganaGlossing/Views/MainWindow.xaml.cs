using CommunityToolkit.Mvvm.Messaging;
using FuriganaGlossing.Messages;
using FuriganaGlossing.Services;
using FuriganaGlossing.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FuriganaGlossing.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly LogPresenter _logPresenter;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();
            DataContext = _viewModel;

            _logPresenter = new LogPresenter(App.ServiceProvider.GetRequiredService<ILogService>(), Application.Current.Dispatcher);
            LogListBox.ItemsSource = _logPresenter.Logs;
            _logPresenter.Logs.CollectionChanged += OnLogsCollectionChanged;
            UpdateLogCount();

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            this.KeyDown += OnKeyDown;

            InitializeWebView();
            WeakReferenceMessenger.Default.Register<MainWindow, OpenSettingsRequestedMessage>(this, (r, m) => OpenSettingsWindow());
        }

        private void OpenSettingsWindow()
        {
            var configViewModel = App.ServiceProvider.GetRequiredService<ConfigViewModel>();
            var configWindow = new ConfigWindow(configViewModel);

            configWindow.Owner = this;
            configWindow.Activated += async (s, e) => await configViewModel.InitializeAsync();
            configWindow.ShowDialog();
        }

        private void UpdateLogCount()
        {
            LogCountText.Text = $"({_logPresenter.Logs.Count})";
        }

        private async void PasteImageButton_Click(object sender, RoutedEventArgs e)
        {
            await CaptureClipboardImageAsync();
        }

        private void OnLogsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateLogCount();
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

        private async void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                await CaptureClipboardImageAsync();
            }
        }

        private async Task CaptureClipboardImageAsync()
        {
            try
            {
                if (!Clipboard.ContainsImage()) return;

                var image = Clipboard.GetImage();
                using (var ms = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(ms);
                    await _viewModel.ProcessImageAsync(ms.ToArray());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Image processing failed: {ex.Message}");
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
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