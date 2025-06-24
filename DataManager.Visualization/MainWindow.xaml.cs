using DataManager.CentralManager;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DataManager.Visualization
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields
        private readonly Manager _manager = Manager.Instance;
        private const string STATUS_READY = "Ready";
        private const string STATUS_LOADING = "Loading...";
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            // Subscribe to Manager's PropertyChanged event to update UI when data or models change
            _manager.PropertyChanged += Manager_PropertyChanged;

            // Initialize the UI with current data and models
            InitializeUI();
        }
        #endregion

        #region Initialization
        private void InitializeUI()
        {
            try
            {
                UpdateDataGrid();
                UpdatePlot();
                UpdateAvailableData();
                UpdateAvailableModels();
                UpdateStatusText();
                UpdateRecordCount();
                UpdateLastUpdated("Application initialized");
            }
            catch (Exception ex)
            {
                HandleError($"Error initializing UI: {ex.Message}");
            }
        }
        #endregion

        #region Event Handlers
        private async void BtnLoadData_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Select a CSV file to load"
            };

            if (openFileDialog.ShowDialog() != true) return;

            try
            {
                string filePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);

                UpdateStatus($"Loading data from {fileName}...");
                await _manager.LoadFromCsvAsync(filePath);
                UpdateStatus($"Data '{_manager.SelectedData?.Name}' loaded successfully");

                RefreshUIAfterDataOperation();
            }
            catch (Exception ex)
            {
                HandleError($"Error loading data: {ex.Message}");
            }
        }

        private void BtnLoadFromApi_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectApiDataWindow = new SelectApiData { Owner = this };
                bool? result = selectApiDataWindow.ShowDialog();

                if (result == true)
                {
                    UpdateStatus($"API data '{_manager.SelectedData?.Name}' loaded successfully");
                    RefreshUIAfterDataOperation();
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error loading data from API: {ex.Message}");
            }
        }
        private void BtnLoadFromDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectDbDataWindow = new LoadFromDB { Owner = this };
                bool? result = selectDbDataWindow.ShowDialog();
                if (result == true)
                {
                    UpdateStatus($"Database data '{_manager.SelectedData?.Name}' loaded successfully");
                    RefreshUIAfterDataOperation();
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error loading data from DB: {ex.Message}");
            }

        }
        private void BtnExportToDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exportToDbWindow = new ExportToDB { Owner = this };
                bool? result = exportToDbWindow.ShowDialog();
                if (result == true)
                {
                    UpdateStatus($"Data '{_manager.SelectedData?.Name}' exported to DB successfully");
                    UpdateLastUpdated("Data export to DB");
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error exporting data to DB: {ex.Message}");
            }

        }


        private void BtnCalculateModel_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateDataForModel()) return;

            try
            {
                var selectModelWindow = new SelectModel { Owner = this };
                bool? result = selectModelWindow.ShowDialog();

                if (result == true && selectModelWindow.CreatedModel != null)
                {
                    UpdateStatus($"Model '{selectModelWindow.CreatedModel.Name}' created successfully");
                    //_manager.SelectedModel = selectModelWindow.CreatedModel;
                    RefreshUIAfterModelOperation();
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error creating model: {ex.Message}");
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            // Check if there's data to export
            if (_manager.DataList.Count == 0)
            {
                MessageBox.Show("No data available to export.", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create and show the export dialog
                var exportWindow = new ExportToFile
                {
                    Owner = this
                };

                // Show the dialog and check the result
                bool? result = exportWindow.ShowDialog();

                if (result == true)
                {
                    // Export was successful
                    UpdateStatus("Data exported successfully");
                    UpdateLastUpdated("Data export");
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error during export: {ex.Message}");
            }
        }

        private void BtnCompare_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_manager.ModelList.Count < 2)
                {
                    MessageBox.Show("You need at least two models to compare. Please create more models.",
                        "Insufficient Models", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Implement model comparison functionality here
                var comparisonWindow = new ModelComparisonWindow
                {
                    Owner = this
                };
                comparisonWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                HandleError($"Error during model comparison: {ex.Message}");
            }
        }


        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clearOptionWindow = new ClearOption { Owner = this };
                bool? result = clearOptionWindow.ShowDialog();

                if (result != true) return;

                ProcessClearOptions(clearOptionWindow);
            }
            catch (Exception ex)
            {
                HandleError($"Error during clear operation: {ex.Message}");
            }
        }
        private void BtnCompareStocks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var compareStock = new StockWindow
                {
                    Owner = this
                };
                _ = compareStock.ShowDialog();
            }
            catch (Exception ex)
            {
                HandleError($"Error during stock comparison: {ex.Message}");
            }
        }

        private void AvailableData_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AvailableData.SelectedItem == null) return;

            try
            {
                var selectedData = AvailableData.SelectedItem.ToString();
                if (selectedData != null)
                {
                    _manager.SetAsSelected(selectedData);
                    UpdateDataGrid();
                    UpdatePlot();
                    UpdateRecordCount();
                    UpdateStatus($"Selected data: {selectedData}");
                    UpdateLastUpdated("Data selection changed");
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error changing data selection: {ex.Message}");
            }
        }

        private void AvailableModels_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (AvailableModels.SelectedItem != null)
                {
                    var selectedModel = AvailableModels.SelectedItem.ToString();
                    if (selectedModel != null)
                    {
                        _manager.SetAsSelectedModel(selectedModel);
                        UpdatePlot();
                        UpdateDataGrid();
                        UpdateModelDetails();
                        UpdateRecordCount();
                        UpdateStatus($"Selected model: {selectedModel}");
                        UpdateLastUpdated("Model selection changed");
                    }
                }
                else
                {
                    UpdateModelDetails();
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error changing model selection: {ex.Message}");
            }
        }

        private void Manager_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e == null) return;

            Dispatcher.Invoke(() =>
            {
                try
                {
                    switch (e.PropertyName)
                    {
                        case nameof(Manager.SelectedData):
                            UpdateDataGrid();
                            UpdatePlot();
                            UpdateRecordCount();
                            break;

                        case nameof(Manager.SelectedModel):
                            UpdateDataGrid();
                            UpdatePlot();
                            UpdateModelDetails();
                            UpdateRecordCount();
                            break;

                        case nameof(Manager.IsBusy):
                            UpdateStatusText();
                            break;

                        case nameof(Manager.ErrorFlag) when _manager.ErrorFlag:
                            HandleError(_manager.ErrorMessage);
                            break;

                        case nameof(Manager.ActualState):
                            UpdateStateBasedStatus();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    HandleError($"Error handling property change: {ex.Message}");
                }
            });
        }
        #endregion

        #region UI Update Methods
        private void UpdateDataGrid()
        {
            try
            {
                // Clear the DataGrid if no model or data is selected
                if (_manager.SelectedModel == null && _manager.SelectedData == null)
                {
                    dataGrid.ItemsSource = null;
                    return;
                }

                if (_manager.SelectedModel != null && _manager.SelectedData != null &&
                    _manager.SelectedModel.ModelDataSet != null && _manager.SelectedData.Data != null)
                {
                    // Case 1: Both model and data are selected
                    var combinedData = _manager.SelectedData.Data.Select(d => new
                    {
                        d.Time,
                        DataValue = d.Value,
                        ModelValue = _manager.SelectedModel.ModelDataSet.Data
                            .FirstOrDefault(m => m.Time == d.Time)?.Value
                    }).ToList();

                    dataGrid.ItemsSource = combinedData;
                }
                else if (_manager.SelectedModel != null && _manager.SelectedModel.ModelDataSet != null)
                {
                    // Case 2: Only a model is selected
                    var modelOnly = _manager.SelectedModel.ModelDataSet.Data.Select(m => new
                    {
                        m.Time,
                        ModelValue = m.Value
                    }).ToList();

                    dataGrid.ItemsSource = modelOnly;
                }
                else if (_manager.SelectedData != null && _manager.SelectedData.Data != null)
                {
                    // Case 3: Only data is selected
                    var dataOnly = _manager.SelectedData.Data.Select(d => new
                    {
                        d.Time,
                        DataValue = d.Value
                    }).ToList();

                    dataGrid.ItemsSource = dataOnly;
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error updating data grid: {ex.Message}");
                dataGrid.ItemsSource = null;
            }
        }

        private void UpdatePlot()
        {
            try
            {
                // Clear plot if no data or model is selected
                if (_manager.SelectedData == null && _manager.SelectedModel == null)
                {
                    plotView.Model = null;
                    return;
                }

                var plotModel = CreatePlotModel();
                ConfigurePlotAxes(plotModel);
                AddDataSeries(plotModel);
                AddModelSeries(plotModel);

                plotView.Model = plotModel;
            }
            catch (Exception ex)
            {
                HandleError($"Error updating plot: {ex.Message}");
                plotView.Model = null;
            }
        }

        private PlotModel CreatePlotModel()
        {
            var plotModel = new PlotModel();

            // Set title based on what's selected
            if (_manager.SelectedData != null)
            {
                plotModel.Title = _manager.SelectedData.Name;
            }
            else if (_manager.SelectedModel != null)
            {
                plotModel.Title = _manager.SelectedModel.Name;
            }

            return plotModel;
        }

        private static void ConfigurePlotAxes(PlotModel plotModel)
        {
            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Time" });
            plotModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Value" });
        }

        private void AddDataSeries(PlotModel plotModel)
        {
            if (_manager.SelectedData?.Data == null) return;

            var dataSeries = new LineSeries
            {
                Title = _manager.SelectedData.Name,
                MarkerType = MarkerType.Circle
            };

            foreach (var point in _manager.SelectedData.Data)
            {
                dataSeries.Points.Add(new DataPoint(point.Time, point.Value));
            }

            plotModel.Series.Add(dataSeries);
        }

        private void AddModelSeries(PlotModel plotModel)
        {
            if (_manager.SelectedModel?.ModelDataSet?.Data == null) return;

            var modelSeries = new LineSeries
            {
                Title = _manager.SelectedModel.Name,
                MarkerType = MarkerType.Diamond,
                Color = OxyColors.Red
            };

            foreach (var point in _manager.SelectedModel.ModelDataSet.Data)
            {
                modelSeries.Points.Add(new DataPoint(point.Time, point.Value));
            }

            plotModel.Series.Add(modelSeries);
        }

        private void UpdateAvailableData()
        {
            try
            {
                var dataList = _manager.GetAvailableDataSetsNames();
                AvailableData.ItemsSource = dataList;

                // Select the currently selected data if it exists
                if (_manager.SelectedData != null)
                {
                    AvailableData.SelectedItem = _manager.SelectedData.Name;
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error updating available data: {ex.Message}");
                AvailableData.ItemsSource = new List<string>();
            }
        }

        private void UpdateAvailableModels()
        {
            try
            {
                var modelList = _manager.GetAvailableModelsNames();
                AvailableModels.ItemsSource = modelList;

                // Select the currently selected model if it exists
                if (_manager.SelectedModel != null)
                {
                    AvailableModels.SelectedItem = _manager.SelectedModel.Name;
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error updating available models: {ex.Message}");
                AvailableModels.ItemsSource = new List<string>();
            }
        }

        private void UpdateModelDetails()
        {
            try
            {
                if (_manager.SelectedModel != null)
                {
                    double mse = _manager.SelectedModel.GetMSE();
                    double r2 = _manager.SelectedModel.GetRSquared();

                    mseValue.Text = mse.ToString("F6");  // Format to 6 decimal places
                    r2BoxValue.Text = r2.ToString("F6");  // Format to 6 decimal places
                }
                else
                {
                    mseValue.Text = "N/A";
                    r2BoxValue.Text = "N/A";
                }
            }
            catch (Exception ex)
            {
                HandleError($"Error updating model details: {ex.Message}");
                mseValue.Text = "Error";
                r2BoxValue.Text = "Error";
            }
        }

        private void UpdateStatusText()
        {
            statusText.Text = _manager.IsBusy ? STATUS_LOADING : STATUS_READY;
        }

        private void UpdateDropDown()
        {
            AvailableData.SelectedItem = _manager.SelectedData?.Name;
            AvailableModels.SelectedItem = _manager.SelectedModel?.Name;
        }

        private void UpdateRecordCount()
        {
            try
            {
                if (_manager.SelectedData != null)
                {
                    int count = _manager.SelectedData.Data.Count;
                    recordCount.Text = $"Records: {count}";
                }
                else if (_manager.SelectedModel?.ModelDataSet != null)
                {
                    int count = _manager.SelectedModel.ModelDataSet.Data.Count;
                    recordCount.Text = $"Records: {count}";
                }
                else
                {
                    recordCount.Text = "Records: 0";
                }
            }
            catch (Exception)
            {
                recordCount.Text = "Records: ?";
            }
        }

        private void UpdateStatus(string message)
        {
            statusText.Text = message;
        }

        private void UpdateLastUpdated(string operation)
        {
            if (lastUpdated != null)
            {
                lastUpdated.Text = $"Last Updated: {DateTime.Now:g} ({operation})";
            }
        }

        private void UpdateStateBasedStatus()
        {
            switch (_manager.ActualState)
            {
                case State.Loading:
                    UpdateStatus("Loading data...");
                    break;
                case State.Loaded:
                    UpdateStatus($"Data '{_manager.SelectedData?.Name}' loaded successfully");
                    break;
                case State.Calculating:
                    UpdateStatus("Calculating model...");
                    break;
                case State.Saving:
                    UpdateStatus("Saving data...");
                    break;
                case State.Saved:
                    UpdateStatus("Data saved successfully");
                    break;
                case State.Error:
                    UpdateStatus($"Error: {_manager.ErrorMessage}");
                    break;
                case State.Idle:
                default:
                    UpdateStatus(STATUS_READY);
                    break;
            }
        }
        #endregion

        #region Helper Methods
        private void RefreshUIAfterDataOperation()
        {
            UpdateDataGrid();
            UpdatePlot();
            UpdateAvailableData();
            UpdateDropDown();
            UpdateRecordCount();
            UpdateLastUpdated("Data operation completed");
        }

        private void RefreshUIAfterModelOperation()
        {
            UpdateAvailableModels();
            UpdateModelDetails();
            UpdateDataGrid();
            UpdatePlot();
            UpdateDropDown();
            UpdateRecordCount();
            UpdateLastUpdated("Model operation completed");
        }

        private void ProcessClearOptions(ClearOption clearOptionWindow)
        {
            if (clearOptionWindow.IsModel)
            {
                plotView.Model = new PlotModel();
                ClearModelOnly();

            }
            else if (clearOptionWindow.IsData)
            {
                plotView.Model = new PlotModel();
                ClearDataOnly();
            }
            else if (clearOptionWindow.IsBoth)
            {
                plotView.Model = new PlotModel();
                ClearBothDataAndModel();
            }

            UpdateLastUpdated("Clear operation completed");
        }

        private void ClearModelOnly()
        {
            string modelName = _manager.SelectedModel?.Name ?? "Unknown";
            _manager.SelectedModel = null;
            ClearModelDisplay();
            UpdateStatus($"Model '{modelName}' cleared");
        }

        private void ClearDataOnly()
        {
            string dataName = _manager.SelectedData?.Name ?? "Unknown";
            _manager.SelectedData = null;
            ClearDataDisplay();
            UpdateStatus($"Data '{dataName}' cleared");
        }

        private void ClearBothDataAndModel()
        {
            string modelName = _manager.SelectedModel?.Name ?? "Unknown";
            string dataName = _manager.SelectedData?.Name ?? "Unknown";
            _manager.SelectedModel = null;
            _manager.SelectedData = null;
            ClearAllDisplay();
            UpdateStatus($"Data '{dataName}' and model '{modelName}' cleared");
        }

        private void ClearModelDisplay()
        {
            try
            {
                // Clear model from UI but keep data
                if (_manager.SelectedData != null)
                {
                    // Only update data-related parts
                    dataGrid.ItemsSource = _manager.SelectedData.Data.Select(d => new { d.Time, DataValue = d.Value }).ToList();
                }
                else
                {
                    dataGrid.ItemsSource = null;
                }

                // Reset model metrics
                mseValue.Text = "N/A";
                r2BoxValue.Text = "N/A";

                // Update UI
                plotView.InvalidatePlot(true);
                UpdateAvailableModels();
                UpdatePlot();
                UpdateDropDown();
                UpdateRecordCount();
            }
            catch (Exception ex)
            {
                HandleError($"Error clearing model display: {ex.Message}");
            }
        }

        private void ClearDataDisplay()
        {
            try
            {
                // Clear data from UI but keep model if applicable
                if (_manager.SelectedModel?.ModelDataSet != null)
                {
                    // Only show model data
                    dataGrid.ItemsSource = _manager.SelectedModel.ModelDataSet.Data
                        .Select(m => new { m.Time, ModelValue = m.Value }).ToList();
                }
                else
                {
                    dataGrid.ItemsSource = null;
                }

                // Update UI
                plotView.InvalidatePlot(true);
                UpdateAvailableData();
                UpdatePlot();
                UpdateDropDown();
                UpdateRecordCount();
            }
            catch (Exception ex)
            {
                HandleError($"Error clearing data display: {ex.Message}");
            }
        }

        private void ClearAllDisplay()
        {
            try
            {
                // Clear both model and data from UI
                dataGrid.ItemsSource = null;
                plotView.Model = new PlotModel();
                plotView.InvalidatePlot(true);

                // Reset metrics
                mseValue.Text = "N/A";
                r2BoxValue.Text = "N/A";
                recordCount.Text = "Records: 0";

                // Update dropdowns
                UpdateAvailableData();
                UpdateAvailableModels();
                UpdateDropDown();
            }
            catch (Exception ex)
            {
                HandleError($"Error clearing all display: {ex.Message}");
            }
        }


        private void HandleError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus($"Error: {errorMessage}");
            UpdateLastUpdated("Error occurred");
        }

        private bool ValidateDataForModel()
        {
            if (_manager.DataList.Count == 0)
            {
                MessageBox.Show("No data available. Please load data before creating a model.",
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
        #endregion
    }
}







