using DataManager.CentralManager;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.Windows;
using System.Windows.Controls;

namespace DataManager.Visualization
{
    /// <summary>
    /// Provides an interface for comparing two models side-by-side with visualizations and metrics.
    /// </summary>
    /// <remarks>
    /// This window allows users to select two different models and view a detailed comparison
    /// of their performance metrics (MSE and R-squared), visualize them on the same chart,
    /// and export the comparison results.
    /// </remarks>
    public partial class ModelComparisonWindow : Window
    {
        #region Fields

        private readonly Manager _manager = Manager.Instance;
        private DataManager.Models.Model? _firstModel;
        private DataManager.Models.Model? _secondModel;
        
        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the ModelComparisonWindow class.
        /// </summary>
        public ModelComparisonWindow()
        {
            InitializeComponent();
            LoadModels();
        }

        /// <summary>
        /// Loads available models into the selection dropdowns.
        /// </summary>
        /// <remarks>
        /// This method populates the model selection dropdown lists and 
        /// automatically selects the first two available models if possible.
        /// If fewer than two models are available, it shows a warning message and closes the window.
        /// </remarks>
        private void LoadModels()
        {
            try
            {
                // Get available models
                var modelNames = _manager.GetAvailableModelsNames();

                if (modelNames.Count < 2)
                {
                    MessageBox.Show("You need at least two models to compare. Please create more models.",
                        "Insufficient Models", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                // Populate ComboBoxes
                FirstModelComboBox.ItemsSource = modelNames;
                SecondModelComboBox.ItemsSource = modelNames;

                // Select first two models by default (if available)
                if (modelNames.Count >= 1)
                    FirstModelComboBox.SelectedIndex = 0;
                if (modelNames.Count >= 2)
                    SecondModelComboBox.SelectedIndex = 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading models: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the selection change event for the first model dropdown.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data containing information about the selection change.</param>
        private void FirstModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FirstModelComboBox.SelectedItem is string modelName)
            {
                _firstModel = _manager.ModelList.FirstOrDefault(m => m.Name == modelName);
                UpdateFirstModelInfo();
                UpdateComparisonPlot();
            }
        }

        /// <summary>
        /// Handles the selection change event for the second model dropdown.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data containing information about the selection change.</param>
        private void SecondModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SecondModelComboBox.SelectedItem is string modelName)
            {
                _secondModel = _manager.ModelList.FirstOrDefault(m => m.Name == modelName);
                UpdateSecondModelInfo();
                UpdateComparisonPlot();
            }
        }

        /// <summary>
        /// Handles the click event for the export button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data.</param>
        /// <remarks>
        /// Exports the model comparison summary to a text file.
        /// </remarks>
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a simple comparison summary
                var comparison = GenerateComparisonSummary();

                // Show export dialog
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    Title = "Save Model Comparison"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveFileDialog.FileName, comparison);
                    MessageBox.Show("Comparison exported successfully.",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting comparison: {ex.Message}",
                    "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles the click event for the close button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data.</param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region UI Update Methods

        /// <summary>
        /// Updates the information display for the first model.
        /// </summary>
        /// <remarks>
        /// This method updates the UI with the first model's type, MSE, and R² values.
        /// If no model is selected, it displays "N/A" for all fields.
        /// </remarks>
        private void UpdateFirstModelInfo()
        {
            if (_firstModel != null)
            {
                FirstModelType.Text = _firstModel.GetType().Name;
                FirstModelMSE.Text = _firstModel.GetMSE().ToString("F6");
                FirstModelR2.Text = _firstModel.GetRSquared().ToString("F6");
            }
            else
            {
                FirstModelType.Text = "N/A";
                FirstModelMSE.Text = "N/A";
                FirstModelR2.Text = "N/A";
            }
        }

        /// <summary>
        /// Updates the information display for the second model.
        /// </summary>
        /// <remarks>
        /// This method updates the UI with the second model's type, MSE, and R² values.
        /// If no model is selected, it displays "N/A" for all fields.
        /// </remarks>
        private void UpdateSecondModelInfo()
        {
            if (_secondModel != null)
            {
                SecondModelType.Text = _secondModel.GetType().Name;
                SecondModelMSE.Text = _secondModel.GetMSE().ToString("F6");
                SecondModelR2.Text = _secondModel.GetRSquared().ToString("F6");
            }
            else
            {
                SecondModelType.Text = "N/A";
                SecondModelMSE.Text = "N/A";
                SecondModelR2.Text = "N/A";
            }
        }

        /// <summary>
        /// Updates the comparison plot with data from both models.
        /// </summary>
        /// <remarks>
        /// Creates a visualization showing the original data alongside both models' approximations.
        /// Each series uses different colors and markers for clear visual distinction.
        /// </remarks>
        private void UpdateComparisonPlot()
        {
            if (_firstModel == null || _secondModel == null)
                return;

            try
            {
                var plotModel = new PlotModel { Title = "Model Comparison" };

                // Configure axes
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Time"
                });

                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "Value"
                });

                // Add original data series if available (from either model)
                var originalData = _firstModel.OriginalDataSet ?? _secondModel.OriginalDataSet;
                if (originalData != null)
                {
                    var originalSeries = new LineSeries
                    {
                        Title = "Original Data",
                        MarkerType = MarkerType.Circle,
                        MarkerSize = 3,
                        Color = OxyColors.Black
                    };

                    foreach (var point in originalData.Data)
                    {
                        originalSeries.Points.Add(new DataPoint(point.Time, point.Value));
                    }

                    plotModel.Series.Add(originalSeries);
                }

                // Add first model series
                if (_firstModel.ModelDataSet != null)
                {
                    var firstModelSeries = new LineSeries
                    {
                        Title = _firstModel.Name,
                        MarkerType = MarkerType.Diamond,
                        MarkerSize = 4,
                        Color = OxyColors.Blue
                    };

                    foreach (var point in _firstModel.ModelDataSet.Data)
                    {
                        firstModelSeries.Points.Add(new DataPoint(point.Time, point.Value));
                    }

                    plotModel.Series.Add(firstModelSeries);
                }

                // Add second model series
                if (_secondModel.ModelDataSet != null)
                {
                    var secondModelSeries = new LineSeries
                    {
                        Title = _secondModel.Name,
                        MarkerType = MarkerType.Square,
                        MarkerSize = 4,
                        Color = OxyColors.Red
                    };

                    foreach (var point in _secondModel.ModelDataSet.Data)
                    {
                        secondModelSeries.Points.Add(new DataPoint(point.Time, point.Value));
                    }

                    plotModel.Series.Add(secondModelSeries);
                }

                // Set the plot model
                ComparisonPlotView.Model = plotModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating comparison plot: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Comparison Analysis

        /// <summary>
        /// Generates a comprehensive textual summary comparing the two selected models.
        /// </summary>
        /// <returns>
        /// A formatted string containing model details, performance metrics, and recommendations.
        /// </returns>
        /// <remarks>
        /// The summary includes:
        /// - Basic information about both models
        /// - Performance metrics (MSE and R²)
        /// - A comparison of their relative performance
        /// - A recommendation based on which model performs better
        /// - A timestamp of when the comparison was generated
        /// </remarks>
        private string GenerateComparisonSummary()
        {
            if (_firstModel == null || _secondModel == null)
                return "Unable to generate comparison - model data is incomplete";

            var summary = new System.Text.StringBuilder();

            summary.AppendLine("MODEL COMPARISON SUMMARY");
            summary.AppendLine("=======================");
            summary.AppendLine();

            // First Model
            summary.AppendLine($"Model 1: {_firstModel.Name}");
            summary.AppendLine($"Type: {_firstModel.GetType().Name}");
            summary.AppendLine($"MSE: {_firstModel.GetMSE():F6}");
            summary.AppendLine($"R²: {_firstModel.GetRSquared():F6}");
            summary.AppendLine();

            // Second Model
            summary.AppendLine($"Model 2: {_secondModel.Name}");
            summary.AppendLine($"Type: {_secondModel.GetType().Name}");
            summary.AppendLine($"MSE: {_secondModel.GetMSE():F6}");
            summary.AppendLine($"R²: {_secondModel.GetRSquared():F6}");
            summary.AppendLine();

            // Comparison
            summary.AppendLine("COMPARISON");
            summary.AppendLine("----------");

            double mseDiff = _firstModel.GetMSE() - _secondModel.GetMSE();
            double r2Diff = _firstModel.GetRSquared() - _secondModel.GetRSquared();

            summary.AppendLine($"MSE Difference: {mseDiff:F6} ({(mseDiff < 0 ? "Model 1 better" : "Model 2 better")})");
            summary.AppendLine($"R² Difference: {r2Diff:F6} ({(r2Diff > 0 ? "Model 1 better" : "Model 2 better")})");
            summary.AppendLine();

            // Recommendation
            bool model1BetterMSE = mseDiff < 0;
            bool model1BetterR2 = r2Diff > 0;

            if (model1BetterMSE && model1BetterR2)
            {
                summary.AppendLine($"Recommendation: {_firstModel.Name} performs better on both metrics.");
            }
            else if (!model1BetterMSE && !model1BetterR2)
            {
                summary.AppendLine($"Recommendation: {_secondModel.Name} performs better on both metrics.");
            }
            else
            {
                summary.AppendLine("Recommendation: Models have mixed performance. " +
                                   $"{(model1BetterMSE ? _firstModel.Name : _secondModel.Name)} has better MSE, " +
                                   $"while {(model1BetterR2 ? _firstModel.Name : _secondModel.Name)} has better R².");
            }

            summary.AppendLine();
            summary.AppendLine($"Generated on: {DateTime.Now}");

            return summary.ToString();
        }

        #endregion
    }
}


