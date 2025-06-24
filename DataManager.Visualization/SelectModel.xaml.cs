using DataManager.CentralManager;
using DataManager.Data;
using DataManager.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DataManager.Visualization
{
    /// <summary>
    /// Interaction logic for SelectModel.xaml
    /// </summary>
    public partial class SelectModel : Window
    {
        private readonly Manager _manager = Manager.Instance;
        private Model? _createdModel;

        /// <summary>
        /// Gets the model that was created using this dialog.
        /// </summary>
        public Model? CreatedModel => _createdModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectModel"/> class.
        /// </summary>
        public SelectModel()
        {
            InitializeComponent();
            LoadDataSources();

            // Set default selection
            if (ModelTypeComboBox.Items.Count > 0)
            {
                ModelTypeComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Loads data sources for combo boxes.
        /// </summary>
        private void LoadDataSources()
        {
            // Populate base data combo box from manager
            var dataNames = _manager.GetAvailableDataSetsNames();
            BaseDataComboBox.ItemsSource = dataNames;

            // Select the current data if available
            if (_manager.SelectedData != null)
            {
                BaseDataComboBox.SelectedItem = _manager.SelectedData.Name;
            }
            else if (dataNames.Count > 0)
            {
                BaseDataComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Handles the model type selection change event.
        /// </summary>
        private void ModelTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Hide all parameter panels first
            ApproximationParameters.Visibility = Visibility.Collapsed;
            MaFiltrationParameters.Visibility = Visibility.Collapsed;

            // Show the appropriate parameter panel based on selection
            if (ModelTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string modelType = selectedItem.Content.ToString() ?? string.Empty;

                switch (modelType)
                {
                    case "Approximation":
                        ApproximationParameters.Visibility = Visibility.Visible;
                        break;
                    case "MA Filtration":
                        MaFiltrationParameters.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        /// <summary>
        /// Updates the text displaying the degree value for the approximation.
        /// </summary>
        private void DegreeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DegreeValue != null)
            {
                DegreeValue.Text = ((int)e.NewValue).ToString();
            }
        }

        /// <summary>
        /// Updates the text displaying the window size value for MA filtration.
        /// </summary>
        private void WindowSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (WindowSizeValue != null)
            {
                WindowSizeValue.Text = ((int)e.NewValue).ToString();
            }
        }

        /// <summary>
        /// Creates a model based on the user's selections.
        /// </summary>
        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate selections
            if (BaseDataComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a base data set.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedDataName = BaseDataComboBox.SelectedItem.ToString();
            if (string.IsNullOrEmpty(selectedDataName))
            {
                MessageBox.Show("Selected data name is invalid.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get the selected data
            DataPoints? selectedData = null;
            foreach (var data in _manager.DataList)
            {
                if (data.Name == selectedDataName)
                {
                    selectedData = data;
                    break;
                }
            }

            if (selectedData == null)
            {
                MessageBox.Show("Could not find the selected data set.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                // Get the model type and create appropriate model
                if (ModelTypeComboBox.SelectedItem is not ComboBoxItem selectedItem)
                {
                    MessageBox.Show("Please select a model type.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string modelType = selectedItem.Content.ToString() ?? string.Empty;
                Model model;

                switch (modelType)
                {
                    case "Approximation":
                        if (_manager.SelectedData == null)
                        {
                            MessageBox.Show("No data set is selected.", "Validation Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (DegreeSlider.Value >= _manager.SelectedData.Size())
                        {
                            MessageBox.Show("Degree must be less than the number of data points.", "Validation Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        int degree = (int)DegreeSlider.Value;
                        model = new Approximation(selectedData, degree);
                        break;

                    case "MA Filtration":
                        if (WindowSizeSlider.Value >= selectedData.Size())
                        {
                            MessageBox.Show("Window size must be less than the number of data points.", "Validation Error",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        int windowSize = (int)WindowSizeSlider.Value;
                        model = new MaFiltration(selectedData, windowSize);
                        break;

                    default:
                        MessageBox.Show($"Unsupported model type: {modelType}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                }

                // Calculate the model
                Mouse.OverrideCursor = Cursors.Wait;
                await _manager.CalculateModelAsync(model);
                Mouse.OverrideCursor = null;

                // Store reference to created model
                _createdModel = model;

                // Set dialog result to true (success)
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"Error creating model: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Closes the dialog without creating a model.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

