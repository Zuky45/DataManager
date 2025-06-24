using DataManager.CentralManager;
using DataManager.Data;
using System.Windows;
using System.Windows.Controls;

namespace DataManager.Visualization
{
    /// <summary>
    /// Interaction logic for ExportToDB.xaml
    /// </summary>
    public partial class ExportToDB : Window
    {
        private readonly Manager _manager = Manager.Instance;
        private DataPoints? _selectedData;

        /// <summary>
        /// Initializes a new instance of the ExportToDB class.
        /// </summary>
        public ExportToDB()
        {
            InitializeComponent();
            InitializeControls();
        }

        /// <summary>
        /// Initializes the form controls with data from the manager.
        /// </summary>
        private void InitializeControls()
        {
            try
            {
                // Get available data sources
                var availableData = _manager.GetAvailableDataSetsNames();

                // Check if any data is available
                if (availableData.Count == 0)
                {
                    MessageBox.Show("No data available to export to the database.",
                        "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DialogResult = false;
                    Close();
                    return;
                }

                // Populate the data source combo box
                DataSourceComboBox.ItemsSource = availableData;

                // Select current data if available, otherwise select first item
                if (_manager.SelectedData != null)
                {
                    DataSourceComboBox.SelectedItem = _manager.SelectedData.Name;
                }
                else
                {
                    DataSourceComboBox.SelectedIndex = 0;
                }

                // Set initial status
                StatusTextBlock.Text = "Ready to save to database";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing form: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
                Close();
            }
        }

        /// <summary>
        /// Updates the form fields based on the selected data.
        /// </summary>
        private void UpdateFormFields()
        {
            if (_selectedData == null)
                return;

            // Set name and description
            DataNameTextBox.Text = _selectedData.Name;
            DescriptionTextBox.Text = _selectedData.Description ?? string.Empty;
            StartIndexTextBox.Text = _selectedData.MinTime.ToString();

            // Update data preview
            UpdatePreview();
        }

        /// <summary>
        /// Updates the data preview grid.
        /// </summary>
        private void UpdatePreview()
        {
            if (_selectedData == null || _selectedData.Data.Count == 0)
            {
                PreviewDataGrid.ItemsSource = null;
                return;
            }

            // Get preview data (first 10 records)
            var previewData = _selectedData.Data
                .Take(10)
                .Select(p => new { p.Time, p.Value })
                .ToList();

            PreviewDataGrid.ItemsSource = previewData;
        }

        /// <summary>
        /// Handles the data source selection changed event.
        /// </summary>
        private void DataSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataSourceComboBox.SelectedItem is string selectedDataName)
            {
                _selectedData = _manager.GetDataSetByName(selectedDataName);

                if (_selectedData != null)
                {
                    UpdateFormFields();
                    SaveButton.IsEnabled = true;
                }
                else
                {
                    ClearForm();
                    SaveButton.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// Clears the form fields.
        /// </summary>
        private void ClearForm()
        {
            DataNameTextBox.Text = string.Empty;
            DescriptionTextBox.Text = string.Empty;
            StartIndexTextBox.Text = "1";
            PreviewDataGrid.ItemsSource = null;
        }

        /// <summary>
        /// Handles the save button click event.
        /// </summary>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate selected data
                if (_selectedData == null)
                {
                    MessageBox.Show("No data selected to export.",
                        "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate name
                string name = DataNameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Please enter a name for the dataset.",
                        "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                    DataNameTextBox.Focus();
                    return;
                }

                // Validate start index
                if (!int.TryParse(StartIndexTextBox.Text, out int startIndex) || startIndex < 0)
                {
                    MessageBox.Show("Please enter a valid start index (must be a non-negative number).",
                        "Invalid Index", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StartIndexTextBox.Focus();
                    return;
                }

                // Disable the save button to prevent multiple clicks
                SaveButton.IsEnabled = false;
                StatusTextBlock.Text = "Saving to database...";

                // Create a clone with the new name and description
                var dataToSave = _selectedData.Clone(name);
                if (IncludeDescriptionCheckBox.IsChecked == true)
                {
                    if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
                    {
                        dataToSave.Description = null;
                    }
                    dataToSave.Description = DescriptionTextBox.Text;
                }
                else
                {
                    dataToSave.Description = null;
                }

                    // Change indexing if needed
                    if (startIndex != _selectedData.MinTime)
                {
                    dataToSave.ChangeIndexing(startIndex);
                }

                // Save to database
                await _manager.ExportToDbAsync(dataToSave);

                // Show success message
                MessageBox.Show($"Dataset '{name}' has been successfully saved to the database.",
                    "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                // Close the dialog with success
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                SaveButton.IsEnabled = true;
                StatusTextBlock.Text = "Error saving to database";
                MessageBox.Show($"Error exporting to database: {ex.Message}",
                    "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                DialogResult = false;
            }
        }

        /// <summary>
        /// Handles the cancel button click event.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}



