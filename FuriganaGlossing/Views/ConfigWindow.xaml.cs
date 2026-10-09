using CommunityToolkit.Mvvm.Messaging;
using FuriganaGlossing.Messages;
using FuriganaGlossing.ViewModels;
using System.Windows;

namespace FuriganaGlossing.Views
{
    public partial class ConfigWindow : Window
    {
        public ConfigWindow(ConfigViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            WeakReferenceMessenger.Default.Register<ConfigWindow, SaveCompletedMessage>(this, (r, m) =>
            {
                if (m.IsSuccess)
                {
                    MessageBox.Show("Configuration saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Failed to save configuration. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            WeakReferenceMessenger.Default.Register<ConfigWindow, CloseConfigWindowMessage>(this, (r, m) =>
            {
                this.Close();
            });
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
