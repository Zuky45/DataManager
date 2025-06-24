using DataManager.CentralManager;
using System.Windows;
using System.Windows.Controls;

namespace DataManager.Visualization
{
    /// <summary>
    /// Interaction logic for SelectApiData.xaml
    /// </summary>
    public partial class SelectApiData : Window
    {
        private readonly Manager _manager = Manager.Instance;

        public SelectApiData()
        {
            InitializeComponent();
            LoadAvailableStocks();
        }

        private void LoadAvailableStocks()
        {
            // Populate the ComboBox with available stock symbols
            var availableStocks = Manager.AvailableStocks();
            cmbStocks.ItemsSource = availableStocks;

            if (availableStocks.Count > 0)
            {
                cmbStocks.SelectedIndex = 0;
                cmbApiModel.SelectedIndex = 0;
            }
        }

        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (cmbStocks.SelectedItem == null)
            {
                MessageBox.Show("Please select a stock symbol.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbApiModel.SelectedItem == null)
            {
                MessageBox.Show("Please select an API model.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get the selected stock symbol and API model
            string? selectedStock = cmbStocks.SelectedItem.ToString();
            string? selectedModel = (cmbApiModel.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (Enum.TryParse(selectedModel, out API.Function apiFunction) && !string.IsNullOrEmpty(selectedStock))
            {
                try
                {
                    btnLoad.IsEnabled = false;
                    await _manager.LoadFromApiAsync(selectedStock, apiFunction);
                    MessageBox.Show("Data loaded successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true; // Indicate success
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    btnLoad.IsEnabled = true;
                }
            }
            else
            {
                MessageBox.Show("Invalid API model selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}


