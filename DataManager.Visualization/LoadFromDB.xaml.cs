using DataManager.CentralManager;
using DataManager.Data;
using System.Windows;
using System.Windows.Controls;

namespace DataManager.Visualization
{
    /// <summary>
    /// Interaction logic for LoadFromDB.xaml
    /// </summary>
    public partial class LoadFromDB : Window
    {
        private readonly Manager _manager = Manager.Instance;
        private DataPoints? _previewData;

        public LoadFromDB()
        {
            InitializeComponent();
            LoadDatasetsAsync();
        }

        /// <summary>
        /// Loads all available datasets from the database
        /// </summary>
        private async void LoadDatasetsAsync()
        {
            try
            {
                StatusTextBlock.Text = "Loading available datasets...";

                // Get all dataset names from the database
                var datasets = await Manager.ListDataPointsFromDb();

                // Display them in the combo box
                DatasetComboBox.ItemsSource = datasets;

                if (datasets.Count > 0)
                {
                    StatusTextBlock.Text = $"Found {datasets.Count} datasets in the database";
                    DatasetComboBox.SelectedIndex = 0;
                }
                else
                {
                    StatusTextBlock.Text = "No datasets found in the database";
                    LoadButton.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Error loading datasets";
                MessageBox.Show($"Error loading datasets from database: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the dataset selection change event
        /// </summary>
        private void DatasetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string? selectedDatasetName = DatasetComboBox.SelectedItem as string;

            if (!string.IsNullOrEmpty(selectedDatasetName))
            {
                LoadPreviewAsync(selectedDatasetName);
                LoadButton.IsEnabled = true;
            }
            else
            {
                ClearPreview();
                LoadButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Loads a preview of the selected dataset
        /// </summary>
        private async void LoadPreviewAsync(string datasetName)
        {
            try
            {
                StatusTextBlock.Text = $"Loading preview for '{datasetName}'...";

                // Get the dataset from the database
                _previewData = await Task.Run(() => Manager.GetDataPointsByNameAsync(datasetName));

                if (_previewData != null)
                {
                    (DateTime Created, DateTime Modified) = await Manager.GetDateTimes(datasetName);
                    // Update the selected dataset details
                    SelectedDatasetNameTextBlock.Text = _previewData.Name;
                    SelectedDatasetDescriptionTextBlock.Text = _previewData.Description ?? "No description available";
                    SelectedDatasetRecordsTextBlock.Text = $"Records: {_previewData.Data.Count}";
                    SelectedDatasetCreatedTextBlock.Text = $"Created: {Created}";
                    SelectedDatasetModifiedTextBlock.Text = $"Modified: {Modified}";



                    // Show a preview of the data (first 10 records)
                    var previewRecords = _previewData.Data.Take(10).Select(p => new { p.Time, p.Value }).ToList();
                    DataPreviewGrid.ItemsSource = previewRecords;

                    StatusTextBlock.Text = $"Previewing '{datasetName}' ({_previewData.Data.Count} records)";
                }
                else
                {
                    ClearPreview();
                    StatusTextBlock.Text = $"Could not load preview for '{datasetName}'";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Error loading preview";
                MessageBox.Show($"Error loading preview: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clears the preview area
        /// </summary>
        private void ClearPreview()
        {
            SelectedDatasetNameTextBlock.Text = "No dataset selected";
            SelectedDatasetDescriptionTextBlock.Text = "Select a dataset from the dropdown to view details";
            SelectedDatasetRecordsTextBlock.Text = "Records: 0";
            SelectedDatasetCreatedTextBlock.Text = "Created: N/A";
            DataPreviewGrid.ItemsSource = null;
            _previewData = null;
        }

        /// <summary>
        /// Handles the refresh button click
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Reload all datasets from the database
            LoadDatasetsAsync();
        }

        /// <summary>
        /// Handles the load button click
        /// </summary>
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSelectedDataset();
        }

        /// <summary>
        /// Loads the selected dataset into the application
        /// </summary>
        private async void LoadSelectedDataset()
        {
            string? selectedDatasetName = DatasetComboBox.SelectedItem as string;

            if (string.IsNullOrEmpty(selectedDatasetName))
            {
                MessageBox.Show("Please select a dataset to load.",
                    "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                StatusTextBlock.Text = $"Loading dataset '{selectedDatasetName}'...";
                LoadButton.IsEnabled = false;

                // Use the Manager to load the dataset
                await _manager.LoadFromDbAsync(selectedDatasetName);

                // Close the dialog with success
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = "Error loading dataset";
                LoadButton.IsEnabled = true;
                MessageBox.Show($"Error loading dataset: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the cancel button click
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}


