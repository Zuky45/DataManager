using DataManager.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DataManager.DB
{
    
    /// <summary>
    /// Manages database operations for datasets and models
    /// </summary>
    /// <remarks>
    /// This class provides a layer of abstraction over the Entity Framework Core
    /// database context, offering methods to store, retrieve, and manage DataPoints objects.
    /// It includes built-in error handling and database initialization.
    /// </remarks>
    public class DBOperationsManager
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the DBOperationsManager class and ensures the database exists
        /// </summary>
        /// <remarks>
        /// The constructor automatically calls EnsureDatabaseCreated to initialize the database
        /// on first use, creating the necessary tables based on the DbContext model.
        /// </remarks>
        public DBOperationsManager()
        {
            // Ensure database is created when the manager is instantiated
            EnsureDatabaseCreated();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures that the database and schema exist
        /// </summary>
        /// <remarks>
        /// This method creates the SQLite database file if it doesn't exist
        /// and creates all required tables based on the DbContext model.
        /// </remarks>
        private static void EnsureDatabaseCreated()
        {
            try
            {
                using var context = CreateContext();
                context.Database.EnsureCreated();
                Console.WriteLine("Database initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a new database context instance
        /// </summary>
        /// <returns>A configured DbContext instance</returns>
        /// <remarks>
        /// This factory method ensures that all database operations use a consistent
        /// configuration for connecting to the database.
        /// </remarks>
        private static DBManagerBase  CreateContext()
        {
            return new DBManagerBase();
        }

        #endregion

        #region Data Retrieval Methods

        /// <summary>
        /// Retrieves a DataPoints object from the database by name
        /// </summary>
        /// <param name="name">The name of the dataset to retrieve</param>
        /// <returns>A fully populated DataPoints object, or null if not found</returns>
        /// <exception cref="ArgumentException">Thrown when the name parameter is null or empty</exception>
        /// <remarks>
        /// This method retrieves a dataset by name, deserializes the JSON data content,
        /// and returns a fully populated DataPoints object with all time-value pairs.
        /// </remarks>
        public static async Task<DataPoints?> GetDataPointsByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Dataset name cannot be null or empty", nameof(name));

            try
            {
                using var context = CreateContext();

                // Ensure database exists before proceeding
                await context.Database.EnsureCreatedAsync();

                var dataset = await context.Datasets.FirstOrDefaultAsync(d => d.Name == name);

                if (dataset == null)
                    return null;

                // Deserialize the data from JSON
                var timeValuePairs = JsonSerializer.Deserialize<List<TimeValuePair>>(dataset.DataContent)
                                   ?? [];

                // Create a new DataPoints object
                var dataPoints = new DataPoints
                {
                    Name = dataset.Name,
                    Description = dataset.Description
                };

                // Add all time-value pairs to the DataPoints object
                foreach (var pair in timeValuePairs)
                {
                    dataPoints.AddDataPoint(pair.Time, pair.Value);
                }

                return dataPoints;
            }
            catch (Exception ex)
            {
                // Log the error or handle it as needed
                Console.WriteLine($"Error retrieving dataset: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Retrieves the creation and modification timestamps for a dataset
        /// </summary>
        /// <param name="name">The name of the dataset</param>
        /// <returns>A tuple containing the creation and last modified dates</returns>
        /// <exception cref="ArgumentException">Thrown when the name parameter is null or empty</exception>
        /// <remarks>
        /// Returns DateTime.MinValue for both values if the dataset is not found or an error occurs.
        /// </remarks>
        public static async Task<(DateTime Created, DateTime Modified)> GetDateTimes(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Dataset name cannot be null or empty", nameof(name));

            try
            {
                using var context = CreateContext();

                // Ensure database exists before proceeding
                await context.Database.EnsureCreatedAsync();

                var dataset = await context.Datasets.FirstOrDefaultAsync(d => d.Name == name);

                if (dataset == null)
                    return (DateTime.MinValue, DateTime.MinValue); // Return minimum dates if dataset not found

                return (dataset.CreatedDate, dataset.LastModified);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving dataset dates: {ex.Message}");
                return (DateTime.MinValue, DateTime.MinValue); // Return minimum dates on error
            }
        }

        /// <summary>
        /// Gets the names of all datasets in the database
        /// </summary>
        /// <returns>A list of dataset names</returns>
        /// <remarks>
        /// Returns an empty list if no datasets are found or if an error occurs.
        /// </remarks>
        public static async Task<List<string>> GetAllDatasetNamesAsync()
        {
            try
            {
                using var context = CreateContext();

                // Ensure database exists before proceeding
                await context.Database.EnsureCreatedAsync();

                return await context.Datasets
                    .Select(d => d.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving dataset names: {ex.Message}");
                return [];
            }
        }

        #endregion

        #region Data Modification Methods

        /// <summary>
        /// Exports a DataPoints object to the database
        /// </summary>
        /// <param name="dataPoints">The DataPoints object to export</param>
        /// <returns>True if the export was successful; otherwise, false</returns>
        /// <exception cref="ArgumentNullException">Thrown when the dataPoints parameter is null</exception>
        /// <remarks>
        /// If a dataset with the same name already exists, it will be updated with the new data.
        /// Otherwise, a new dataset will be created.
        /// </remarks>
        public static async Task<bool> ExportToDbAsync(DataPoints dataPoints)
        {
            if (dataPoints == null)
                throw new ArgumentNullException(nameof(dataPoints), "DataPoints cannot be null");

            try
            {
                using var context = CreateContext();

                // Ensure database exists before proceeding
                await context.Database.EnsureCreatedAsync();

                // Check if dataset with this name already exists
                var existingDataset = await context.Datasets.FirstOrDefaultAsync(d => d.Name == dataPoints.Name);

                // Serialize the data points to JSON
                string dataContent = JsonSerializer.Serialize(dataPoints.Data);

                if (existingDataset != null)
                {
                    // Update existing dataset
                    existingDataset.Description = dataPoints.Description;
                    existingDataset.DataContent = dataContent;
                    existingDataset.LastModified = DateTime.UtcNow;
                }
                else
                {
                    // Create new dataset
                    var newDataset = new DatasetInfo
                    {
                        Name = dataPoints.Name,
                        Description = dataPoints.Description,
                        DataContent = dataContent,
                        CreatedDate = DateTime.UtcNow,
                        LastModified = DateTime.UtcNow
                    };
                    context.Datasets.Add(newDataset);
                }

                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                // Log the error or handle it as needed
                Console.WriteLine($"Error exporting dataset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a dataset from the database by name
        /// </summary>
        /// <param name="name">The name of the dataset to delete</param>
        /// <returns>True if the dataset was deleted; otherwise, false</returns>
        /// <exception cref="ArgumentException">Thrown when the name parameter is null or empty</exception>
        /// <remarks>
        /// Returns false if the dataset wasn't found or if an error occurs during deletion.
        /// </remarks>
        public static async Task<bool> DeleteDatasetAsync(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Dataset name cannot be null or empty", nameof(name));

            try
            {
                using var context = CreateContext();

                // Ensure database exists before proceeding
                await context.Database.EnsureCreatedAsync();

                var dataset = await context.Datasets.FirstOrDefaultAsync(d => d.Name == name);

                if (dataset == null)
                    return false;

                context.Datasets.Remove(dataset);
                await context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting dataset: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Diagnostics Methods

        /// <summary>
        /// Checks if the database exists and is accessible
        /// </summary>
        /// <returns>True if the database can be accessed; otherwise, false</returns>
        /// <remarks>
        /// This method can be used to verify that the database connection is functioning properly
        /// before attempting more complex operations.
        /// </remarks>
        public static bool IsDatabaseAccessible()
        {
            try
            {
                using var context = CreateContext();
                return context.Database.CanConnect();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database connection error: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}




