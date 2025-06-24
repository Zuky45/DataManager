using DataManager.API;
using DataManager.Data;
using DataManager.DB;
using DataManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace DataManager.CentralManager
{
    /// <summary>
    /// Central manager that coordinates between the UI (WPF) and business logic (models, API).
    /// Implements the Singleton pattern for global access.
    /// </summary>
    public class Manager : INotifyPropertyChanged
    {
        #region Singleton Implementation & Private Fields

        private static readonly Lazy<Manager> _instance = new(() => new Manager());
        private readonly DBOperationsManager _dbManager = new();
        private readonly DataManager.StockManager.Manager _stockManager = new() ;

        /// <summary>
        /// Gets the singleton instance of the Manager.
        /// </summary>
        public static Manager Instance => _instance.Value;


        /// <summary>
        /// Initializes a new instance of the Manager class.
        /// </summary>
        private Manager()
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the currently selected data points.
        /// </summary>
        public DataPoints? SelectedData
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the collection of all available data sets.
        /// </summary>
        public ObservableCollection<DataPoints> DataList { get; } = [];

        /// <summary>
        /// Gets or sets the currently selected model.
        /// </summary>
        public Model? SelectedModel { get; set; }

        /// <summary>
        /// Gets the collection of all available models.
        /// </summary>
        public ObservableCollection<Model> ModelList { get; } = [];

        /// <summary>
        /// Gets the current state of the manager.
        /// </summary>
        public State ActualState { get; private set; } = State.Idle;

        /// <summary>
        /// Gets or sets a value indicating whether an error has occurred.
        /// </summary>
        public bool ErrorFlag { get; set; } = false;

        /// <summary>
        /// Gets or sets the error message when an error occurs.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Gets a value indicating whether the manager is currently busy with an operation.
        /// </summary>
        public bool IsBusy => ActualState == State.Loading || ActualState == State.Calculating;

        #endregion

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Event that is raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region State Management

        /// <summary>
        /// Changes the current state of the manager and notifies listeners.
        /// </summary>
        /// <param name="newState">The new state to set.</param>
        public void ChangeState(State newState)
        {
            ActualState = newState;
            OnPropertyChanged(nameof(ActualState));
            OnPropertyChanged(nameof(IsBusy));
        }

        /// <summary>
        /// Sets the error flag and message, and notifies listeners.
        /// </summary>
        /// <param name="message">The error message.</param>
        private void OnLoadError(string message)
        {
            ErrorFlag = true;
            ErrorMessage = message;
            OnPropertyChanged(nameof(ErrorFlag));
            OnPropertyChanged(nameof(ErrorMessage));
        }

        #endregion

        #region File Operations

        /// <summary>
        /// Loads data from a CSV file asynchronously.
        /// </summary>
        /// <param name="filePath">The path to the CSV file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the file path is null or empty.</exception>
        public async Task LoadFromCsvAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            ChangeState(State.Loading);

            try
            {
                // Execute file reading on a background task
                var result = await Task.Run(() => DataFileHandler.ReadDataFile(filePath));
                DataPoints data = result.data;
                bool isSuccess = result.isSuccess;

                if (isSuccess)
                {
                    // Update collections on the calling thread
                    DataList.Add(data);
                    SelectedData = data;
                    OnPropertyChanged(nameof(SelectedData));
                }
                else
                {
                    // Notify the UI layer of the error
                    OnLoadError("Failed to load data from file.");
                }
            }
            catch (Exception ex)
            {
                // Notify the UI layer of the error
                OnLoadError($"Error loading file: {ex.Message}");
            }
            finally
            {
                ChangeState(State.Idle);
            }
        }

        /// <summary>
        /// Saves the currently selected data to a CSV file asynchronously.
        /// </summary>
        /// <param name="filePath">The path to save the CSV file.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the file path is null or empty.</exception>
        public async Task SaveToCsvAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (SelectedData == null)
                throw new InvalidOperationException("No data selected for saving.");

            ChangeState(State.Saving);

            try
            {
                // Execute file writing on a background task
                await Task.Run(() => DataFileHandler.WriteDataFile(SelectedData, filePath));
                ChangeState(State.Saved);
            }
            catch (Exception ex)
            {
                // Notify the UI layer of the error
                OnLoadError($"Error saving file: {ex.Message}");
            }
            finally
            {
                ChangeState(State.Idle);
            }
        }

        #endregion

        #region API Operations

        /// <summary>
        /// Loads data from an API asynchronously.
        /// </summary>
        /// <param name="symbol">The symbol to retrieve data for.</param>
        /// <param name="function">The API function to call.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the symbol is null or empty.</exception>
        public async Task LoadFromApiAsync(string symbol, Function function)
        {
            if (string.IsNullOrEmpty(symbol))
                throw new ArgumentException("Symbol cannot be null or empty.", nameof(symbol));

            ChangeState(State.Loading);

            try
            {
                // Execute API call on a background task
                var data = await APIHandler.GetDataSet(symbol, function);

                if (data != null && data.Size() != 0)
                {
                    // Update collections on the calling thread
                    DataList.Add(data);
                    SelectedData = data;
                    OnPropertyChanged(nameof(SelectedData));
                }
                else
                {
                    // Notify the UI layer of the error
                    OnLoadError("Failed to load data from API.");
                }
            }
            catch (Exception ex)
            {
                // Notify the UI layer of the error
                OnLoadError($"Error loading data from API: {ex.Message}");
            }
            finally
            {
                ChangeState(State.Idle);
            }
        }

        /// <summary>
        /// Gets a list of available stock symbols.
        /// </summary>
        /// <returns>A list of available stock symbols.</returns>
        public static List<string> AvailableStocks()
        {
            return APIHandler.AvailabelSymbols();
        }

        #endregion

        #region Stock Operations

        /// <summary>
        /// Gets a list of all stock symbols managed by the stock manager.
        /// </summary>
        /// <returns>A list of stock symbols.</returns>
        public List<string> GetStockSymbols()
        {
            return _stockManager.GetNames();
        }


        public List<DataManager.Data.DataPoints> GetDataPoints()
        {
            return _stockManager.DataPoints;
        }

        /// <summary>
        /// Loads all tracked stock data asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ReLoadStocksAsync(API.Function function)
        {
            ChangeState(State.Loading);
            try
            {
                // Execute stock data loading on a background task
                await Task.Run(() => _stockManager.ReLoadData(function));
                OnPropertyChanged(nameof(DataList));
            }
            catch (Exception ex)
            {
                // Notify the UI layer of the error
                OnLoadError($"Error loading stock data: {ex.Message}");
            }
            finally
            {
                ChangeState(State.Idle);
            }
        }

        public async Task LoadAllStocksAsync()
        {
            ChangeState(State.Loading);
            try
            {
                // Execute stock data loading on a background task
                await Task.Run(() => _stockManager.LoadData());
                OnPropertyChanged(nameof(DataList));
            }
            catch (Exception ex)
            {
                // Notify the UI layer of the error
                OnLoadError($"Error loading stock data: {ex.Message}");
            }
            finally
            {
                ChangeState(State.Idle);
            }
        }
        /// <summary>
        /// Gets the latest values for all tracked stocks.
        /// </summary>
        /// <returns>A list of stock symbols and their latest values.</returns>
        public List<(string Name, double Value)> GetStockLatestValues()
        {
            return _stockManager.GetLastValues();
        }

        /// <summary>
        /// Gets the minimum values for all tracked stocks.
        /// </summary>
        /// <returns>A list of stock symbols and their minimum values.</returns>
        public List<(string Name, double Value)> GetStockMinValues()
        {
            return _stockManager.GetMinValues();
        }

        /// <summary>
        /// Gets the maximum values for all tracked stocks.
        /// </summary>
        /// <returns>A list of stock symbols and their maximum values.</returns>
        public List<(string Name, double Value)> GetStockMaxValues()
        {
            return _stockManager.GetMaxValues();
        }

        /// <summary>
        /// Clears all tracked stock data.
        /// </summary>
        public void ClearStockData()
        {
            _stockManager.ClearData();
        }

        #endregion

        #region Database Operations

        /// <summary>
        /// Loads data from the database by name.
        /// </summary>
        /// <param name="name">The name of the dataset to load.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null or empty.</exception>
        public async Task LoadFromDbAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));

            ChangeState(State.Loading);

            try
            {
                // Execute database reading on a background task
                var data = await Task.Run(() => DBOperationsManager.GetDataPointsByNameAsync(name));

                if (data != null)
                {
                    // Update collections on the calling thread
                    DataList.Add(data);
                    SelectedData = data;
                    OnPropertyChanged(nameof(SelectedData));
                }
                else
                {
                    // Notify the UI layer of the error
                    OnLoadError("Failed to load data from database.");
                }
            }
            catch (Exception ex)
            {
                // Notify the UI layer of the error
                OnLoadError($"Error loading data from database: {ex.Message}");
            }
            finally
            {
                ChangeState(State.Idle);
            }
        }

        /// <summary>
        /// Exports data to the database.
        /// </summary>
        /// <param name="data">The data to export.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
        public async Task ExportToDbAsync(DataPoints data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Data cannot be null.");

            ChangeState(State.Loading);

            try
            {
                // Execute database writing on a background task
                var result = await Task.Run(() => DBOperationsManager.ExportToDbAsync(data));

                if (result)
                {
                    // Notify the UI layer of success
                    OnPropertyChanged(nameof(DataList));
                }
                else
                {
                    // Notify the UI layer of the error
                    OnLoadError("Failed to export data to database.");
                }
            }
            catch (Exception ex)
            {
                // Notify the UI layer of the error
                OnLoadError($"Error exporting data to database: {ex.Message}");
            }
            finally
            {
                ChangeState(State.Idle);
            }
        }

        /// <summary>
        /// Gets the creation and modification dates of a dataset by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<(DateTime Created, DateTime Modified)> GetDateTimes(string name)
        {
            return await DBOperationsManager.GetDateTimes(name);
        }

        /// <summary>
        /// Gets a list of all dataset names from the database.
        /// </summary>
        /// <returns>A list of dataset names.</returns>
        public static async Task<List<string>> ListDataPointsFromDb()
        {
            return await DBOperationsManager.GetAllDatasetNamesAsync();
        }

        public static async Task<DataPoints?> GetDataPointsByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            return await DBOperationsManager.GetDataPointsByNameAsync(name);
        }

        #endregion

        #region Model Operations

        /// <summary>
        /// Calculates a model asynchronously.
        /// </summary>
        /// <param name="model">The model to calculate.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the model is null.</exception>
        public async Task CalculateModelAsync(Model model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model), "Model cannot be null.");

            ChangeState(State.Calculating);

            try
            {
                // Execute model calculation on a background task
                await Task.Run(() => model.CalculateModel());
                ModelList.Add(model);
                SelectedModel = model;
                OnPropertyChanged(nameof(SelectedModel));
            }
            catch (Exception ex)
            {
                // Notify the UI layer of the error
                OnLoadError($"Error calculating model: {ex.Message}");
            }
            finally
            {
                ChangeState(State.Idle);
            }
        }

        #endregion

        #region Data Manipulation Operations

        /// <summary>
        /// Changes the indexing of the currently selected data.
        /// </summary>
        /// <param name="index">The new starting index.</param>
        /// <returns>True if the indexing was changed successfully; otherwise, false.</returns>
        /// <exception cref="InvalidOperationException">Thrown when no data is selected.</exception>
        public bool ChangeIndexing(int index)
        {
            if (SelectedData == null)
                throw new InvalidOperationException("No data selected.");

            bool result = SelectedData.ChangeIndexing(index);

            if (result)
            {
                OnPropertyChanged(nameof(SelectedData));
            }
            else
            {
                OnLoadError("Failed to change indexing.");
            }

            return result;
        }

        #endregion

        #region Selection Methods

        /// <summary>
        /// Sets a data set as the selected data based on its name.
        /// </summary>
        /// <param name="name">The name of the data set to select.</param>
        /// <returns>True if a data set with the specified name was found and selected; otherwise, false.</returns>
        public bool SetAsSelected(string name)
        {
            var data = DataList.FirstOrDefault(d => d.Name == name);
            if (data != null)
            {
                SelectedData = data;
                OnPropertyChanged(nameof(SelectedData));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets a model as the selected model based on its name.
        /// </summary>
        /// <param name="name">The name of the model to select.</param>
        /// <returns>True if a model with the specified name was found and selected; otherwise, false.</returns>
        public bool SetAsSelectedModel(string name)
        {
            var model = ModelList.FirstOrDefault(m => m.Name == name);
            if (model != null)
            {
                SelectedModel = model;
                OnPropertyChanged(nameof(SelectedModel));
                return true;
            }
            return false;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets the names of all available data sets.
        /// </summary>
        /// <returns>A list of data set names.</returns>
        public List<string> GetAvailableDataSetsNames()
        {
            return [.. DataList.Select(data => data.Name)];
        }

        /// <summary>
        /// Gets the names of all available models.
        /// </summary>
        /// <returns>A list of model names.</returns>
        public List<string> GetAvailableModelsNames()
        {
            return [.. ModelList.Select(model => model.Name)];
        }

        /// <summary>
        /// Gets a dataset by its name.
        /// </summary>
        /// <param name="name">The name of the dataset to retrieve.</param>
        /// <returns>The dataset if found; otherwise, null.</returns>
        public DataPoints? GetDataSetByName(string name)
        {
            return DataList.FirstOrDefault(d => d.Name == name);
        }

        #endregion

        #region Collection Management

        /// <summary>
        /// Removes a dataset from the collection.
        /// </summary>
        /// <param name="name">The name of the dataset to remove.</param>
        /// <returns>True if the dataset was found and removed; otherwise, false.</returns>
        public bool RemoveData(string name)
        {
            var data = DataList.FirstOrDefault(d => d.Name == name);
            if (data != null)
            {
                DataList.Remove(data);
                OnPropertyChanged(nameof(DataList));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes a model from the collection.
        /// </summary>
        /// <param name="name">The name of the model to remove.</param>
        /// <returns>True if the model was found and removed; otherwise, false.</returns>
        public bool RemoveModel(string name)
        {
            var model = ModelList.FirstOrDefault(m => m.Name == name);
            if (model != null)
            {
                ModelList.Remove(model);
                OnPropertyChanged(nameof(ModelList));
                return true;
            }
            return false;
        }

        #endregion
    }
}



