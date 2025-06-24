using DataManager.CentralManager;
using DataManager.Data;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DataManager.Visualization
{
    /// <summary>
    /// Interaction logic for ExportToFile.xaml
    /// </summary>
    public partial class ExportToFile : Window
    {
        private readonly Manager _manager = Manager.Instance;
        private DataPoints? _selectedData;

        /// <summary>
        /// Initializes a new instance of the ExportToFile class.
        /// </summary>
        public ExportToFile()
        {
            InitializeComponent();
            InitializeControls();
        }

        /// <summary>
        /// Initializes the form controls with data from the manager.
        /// </summary>
        private void InitializeControls()
        {
            // Check if there's any data available to export
            var availableData = _manager.GetAvailableDataSetsNames();
            if (availableData.Count == 0)
            {
                MessageBox.Show("No data available to export.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
                return;
            }

            // Populate data source dropdown
            DataSourceComboBox.ItemsSource = availableData;

            // Select current data if available, otherwise select the first item
            if (_manager.SelectedData != null)
            {
                DataSourceComboBox.SelectedItem = _manager.SelectedData.Name;
            }
            else
            {
                DataSourceComboBox.SelectedIndex = 0;
            }

            // Set initial status
            StatusTextBlock.Text = "Ready to export";
        }

        /// <summary>
        /// Updates the form fields based on the selected data.
        /// </summary>
        private void Update()
        {
            // Get the selected data by name
            if (DataSourceComboBox.SelectedItem is string selectedDataName)
            {
                // Find the data in the manager's list
                _selectedData = _manager.DataList.FirstOrDefault(d => d.Name == selectedDataName);

                if (_selectedData != null)
                {
                    // Update UI fields
                    DataNameTextBox.Text = _selectedData.Name;
                    DescriptionTextBox.Text = _selectedData.Description ?? string.Empty;
                    StartIndexTextBox.Text = _selectedData.MinTime.ToString();

                    // Set default file name
                    string extension = GetSelectedFileExtension();
                    FileNameTextBox.Text = $"{_selectedData.Name}_export{extension}";
                }
            }
        }

        /// <summary>
        /// Gets the file extension based on the selected format.
        /// </summary>
        private string GetSelectedFileExtension()
        {
            // Default to CSV if no format is selected
            if (FormatComboBox.SelectedItem is not ComboBoxItem selectedFormat)
                return ".csv";

            string format = selectedFormat.Content.ToString() ?? "";

            if (format.Contains("Text"))
                return ".txt";

            // Default to CSV for all other cases
            return ".csv";
        }

        /// <summary>
        /// Gets the file filter based on the selected format.
        /// </summary>
        private string GetSelectedFileFilter()
        {
            // Default to CSV if no format is selected
            if (FormatComboBox.SelectedItem is not ComboBoxItem selectedFormat)
                return "CSV files (*.csv)|*.csv|All files (*.*)|*.*";

            string format = selectedFormat.Content.ToString() ?? "";

            if (format.Contains("Text"))
                return "Text files (*.txt)|*.txt|All files (*.*)|*.*";

            // Default to CSV for all other cases
            return "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
        }

        /// <summary>
        /// Handles the browse button click to select a file location.
        /// </summary>
        private void BrowseButton_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = GetSelectedFileFilter(),
                Title = "Save Data File",
                FileName = Path.GetFileName(FileNameTextBox.Text)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                LocationTextBox.Text = Path.GetDirectoryName(saveFileDialog.FileName) ?? "";
                FileNameTextBox.Text = Path.GetFileName(saveFileDialog.FileName);

                // Update status
                StatusTextBlock.Text = "File location selected";
            }
        }

        /// <summary>
        /// Handles the data source selection changed event.
        /// </summary>
        private void DataSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }

        /// <summary>
        /// Handles the export button click to export the selected data.
        /// </summary>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate inputs
                if (_selectedData == null)
                {
                    MessageBox.Show("No data selected for export.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string fileName = FileNameTextBox.Text;
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    MessageBox.Show("Please specify a file name.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string location = LocationTextBox.Text;
                if (string.IsNullOrWhiteSpace(location))
                {
                    MessageBox.Show("Please specify a location.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Construct full path
                string path = Path.Combine(location, fileName);

                // Update status and disable export button
                StatusTextBlock.Text = "Exporting data...";
                ExportButton.IsEnabled = false;

                // Clone the data to avoid modifying the original
                var copy = _selectedData.Clone(DataNameTextBox.Text);

                // Handle description option
                copy.Description = IncludeDescriptionCheckBox.IsChecked == true
                    ? DescriptionTextBox.Text
                    : null;

                // Change indexing if needed
                if (int.TryParse(StartIndexTextBox.Text, out int startIndex))
                {
                    copy.ChangeIndexing(startIndex);
                }
                else
                {
                    MessageBox.Show("Invalid start index. Using original indexing.", "Warning",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Write the data to file
                DataFileHandler.WriteDataFile(copy, path);

                // Show success message
                MessageBox.Show($"Data successfully exported to:\n{path}", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Update status
                StatusTextBlock.Text = "Export completed successfully";
                ExportButton.IsEnabled = true;

                // Close the dialog
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                // Enable the export button again
                ExportButton.IsEnabled = true;

                // Show error message
                StatusTextBlock.Text = "Export failed";
                MessageBox.Show($"Error exporting data: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the cancel button click to close the dialog.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }


    }
}


