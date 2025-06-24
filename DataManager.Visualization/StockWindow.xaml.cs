using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace DataManager.Visualization
{
    /// <summary>
    /// Interaction logic for StockWindow.xaml
    /// Provides visualization of stock time series data using OxyPlot charts and DataGrid.
    /// </summary>
    public partial class StockWindow : Window
    {
        #region Fields

        /// <summary>
        /// Reference to the central manager singleton instance.
        /// </summary>
        private readonly CentralManager.Manager _manager = CentralManager.Manager.Instance;

        /// <summary>
        /// The plot model used for chart visualization.
        /// </summary>
        private PlotModel? _plotModel;

        /// <summary>
        /// The current API function used for loading stock data.
        /// </summary>
        private readonly DataManager.API.Function _currentFunction = DataManager.API.Function.TIME_SERIES_DAILY;

        #endregion
        

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="StockWindow"/> class.
        /// </summary>
        public StockWindow()
        {
            InitializeComponent();
            InitializePlotModel();

            // Set initial function in status bar
            currentFunction.Text = $"Function: {_currentFunction}";
        }

        /// <summary>
        /// Initializes the OxyPlot model with default settings and axes.
        /// </summary>
        private void InitializePlotModel()
        {
            _plotModel = new PlotModel
            {
                Title = "Stock Time Series Data",
                PlotAreaBorderColor = OxyColors.Gray,
                PlotAreaBorderThickness = new OxyThickness(1),
                TextColor = OxyColor.Parse("#2C3E50"),
                TitleColor = OxyColor.Parse("#2C3E50"),
                Background = OxyColors.White
            };

            _plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time",
                AxislineStyle = LineStyle.Solid,
                AxislineThickness = 1,
                AxislineColor = OxyColor.Parse("#2C3E50"),
                MajorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.Parse("#DDDDDD"),
                TextColor = OxyColor.Parse("#2C3E50")
            });

            _plotModel.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Value",
                AxislineStyle = LineStyle.Solid,
                AxislineThickness = 1,
                AxislineColor = OxyColor.Parse("#2C3E50"),
                MajorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.Parse("#DDDDDD"),
                TextColor = OxyColor.Parse("#2C3E50")
            });

            StockPlot.Model = _plotModel;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Load button click event. Loads all stock data and displays it.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadButton.IsEnabled = false;
                statusText.Text = "Loading data...";

                await _manager.LoadAllStocksAsync();
                DisplayStockData();

                statusText.Text = "Data loaded successfully";
                var totalPoints = _manager.GetDataPoints()?.Sum(dp => dp.Data.Count) ?? 0;
                recordCount.Text = $"Records: {totalPoints}";
                lastUpdated.Text = $"Last Updated: {DateTime.Now:g}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                statusText.Text = "Failed to load data";
            }
            finally
            {
                LoadButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the Refresh button click event. Reloads stock data using the current function.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshButton.IsEnabled = false;
                statusText.Text = "Refreshing data...";

                // Reload with current function
                await _manager.ReLoadStocksAsync(_currentFunction);
                DisplayStockData();

                statusText.Text = "Data refreshed successfully";
                var totalPoints = _manager.GetDataPoints()?.Sum(dp => dp.Data.Count) ?? 0;
                recordCount.Text = $"Records: {totalPoints}";
                lastUpdated.Text = $"Last Updated: {DateTime.Now:g}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                statusText.Text = "Failed to refresh data";
            }
            finally
            {
                RefreshButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Handles the Clear button click event. Clears the plot and data table.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the plot and table
            if (_plotModel != null)
            {
                _plotModel.Series.Clear();
                _plotModel.InvalidatePlot(true);
            }

            DataTable.ItemsSource = null;
            statusText.Text = "View cleared";
            recordCount.Text = "Records: 0";
        }

        #endregion

        #region Data Visualization

        /// <summary>
        /// Displays stock data in both the plot and data table.
        /// </summary>
        private void DisplayStockData()
        {
            var dataPoints = _manager.GetDataPoints();
            if (dataPoints == null || dataPoints.Count == 0)
            {
                MessageBox.Show("No data available to display.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (_plotModel != null)
            {
                // Clear existing series
                _plotModel.Series.Clear();

                // Configure DataGrid
                ConfigureDataGrid();

                // Create table data collection
                var tableData = new List<dynamic>();

                // Colors for stock lines
                var stockColors = new[]
                {
                    OxyColor.Parse("#3498DB"), // Blue
                    OxyColor.Parse("#1ABC9C"), // Turquoise
                    OxyColor.Parse("#E74C3C"), // Red
                    OxyColor.Parse("#F39C12"), // Orange
                    OxyColor.Parse("#27AE60"), // Green
                    OxyColor.Parse("#8E44AD"), // Purple
                    OxyColor.Parse("#34495E"), // Navy Blue
                    OxyColor.Parse("#000000")  // Black


                };

                // Add each stock as a line series
                int colorIndex = 0;
                foreach (var stock in dataPoints)
                {
                    // Create line series for this stock
                    var lineSeries = new LineSeries
                    {
                        Title = stock.Name,
                        Color = stockColors[colorIndex % stockColors.Length],
                        MarkerType = MarkerType.Circle,
                        MarkerSize = 3,
                        StrokeThickness = 2
                    };
                    colorIndex++;

                    // Add data points to plot and table
                    foreach (var point in stock.Data)
                    {
                        lineSeries.Points.Add(new DataPoint(point.Time, point.Value));

                        // Add to table data
                        dynamic row = new System.Dynamic.ExpandoObject();
                        var rowDict = (IDictionary<string, object>)row;
                        rowDict["Symbol"] = stock.Name;
                        rowDict["Time"] = point.Time;
                        rowDict["Value"] = point.Value;
                        tableData.Add(row);
                    }

                    _plotModel.Series.Add(lineSeries);
                }

                // Set table data source
                DataTable.ItemsSource = tableData;

                // Update the plot title to include the current function
                _plotModel.Title = $"Stock Time Series Data ({_currentFunction})";

                // Refresh the plot
                _plotModel.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Configures the DataGrid columns for displaying stock data.
        /// </summary>
        private void ConfigureDataGrid()
        {
            DataTable.AutoGenerateColumns = false;
            DataTable.Columns.Clear();
            DataTable.IsReadOnly = true;
            DataTable.AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(242, 242, 242));

            // Define columns
            DataTable.Columns.Add(new DataGridTextColumn
            {
                Header = "Symbol",
                Binding = new System.Windows.Data.Binding("Symbol"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            DataTable.Columns.Add(new DataGridTextColumn
            {
                Header = "Time",
                Binding = new System.Windows.Data.Binding("Time"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            DataTable.Columns.Add(new DataGridTextColumn
            {
                Header = "Value",
                Binding = new System.Windows.Data.Binding("Value"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }

        #endregion
    }
}




















