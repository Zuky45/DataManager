namespace DataManager.Data
{
    /// <summary>
    /// Represents a data frame with time-value pairs and additional metadata.
    /// </summary>
    /// <remarks>
    /// This class provides functionality for storing, retrieving, and manipulating time series data.
    /// It includes methods for adding data points, filtering by time range, modifying indices,
    /// and creating copies of the data series.
    /// </remarks>
    public class DataPoints
    {
        #region Properties

        /// <summary>
        /// Gets or sets the name of the data series.
        /// </summary>
        /// <remarks>
        /// This is a required property that must be initialized when creating a DataPoints instance.
        /// </remarks>
        public required string Name { get; init; }

        /// <summary>
        /// Gets or sets the description of the data series.
        /// </summary>
        /// <remarks>
        /// This property is optional and can be null.
        /// </remarks>
        public string? Description { get; set; }

        /// <summary>
        /// Gets the collection of time-value pairs.
        /// </summary>
        /// <remarks>
        /// This collection is initialized as an empty list and can be modified through
        /// the provided methods such as AddDataPoint and ClearData.
        /// </remarks>
        public List<TimeValuePair> Data { get; } = [];

        /// <summary>
        /// Gets the minimum time value in the data series.
        /// </summary>
        /// <remarks>
        /// Returns 0 if the data series is empty.
        /// </remarks>
        public int MinTime => Data.Count != 0 ? Data.Min(p => p.Time) : 0;

        /// <summary>
        /// Gets the maximum time value in the data series.
        /// </summary>
        /// <remarks>
        /// Returns 0 if the data series is empty.
        /// </remarks>
        public int MaxTime => Data.Count != 0 ? Data.Max(p => p.Time) : 0;

        /// <summary>
        /// Gets the minimum value in the data series.
        /// </summary>
        /// <remarks>
        /// Returns 0 if the data series is empty.
        /// </remarks>
        public double MinValue => Data.Count != 0 ? Data.Min(p => p.Value) : 0;

        /// <summary>
        /// Gets the maximum value in the data series.
        /// </summary>
        /// <remarks>
        /// Returns 0 if the data series is empty.
        /// </remarks>
        public double MaxValue => Data.Count != 0 ? Data.Max(p => p.Value) : 0;

        #endregion

        #region Data Manipulation Methods

        /// <summary>
        /// Adds a new time-value pair to the data series.
        /// </summary>
        /// <param name="time">The time as an integer.</param>
        /// <param name="value">The value as a double.</param>
        /// <remarks>
        /// This method appends the new data point to the end of the collection.
        /// The time values do not need to be sequential or sorted.
        /// </remarks>
        public void AddDataPoint(int time, double value)
        {
            Data.Add(new TimeValuePair(time, value));
        }

        /// <summary>
        /// Changes the time indexing of all data points to start from the specified index.
        /// </summary>
        /// <param name="startIndex">The new starting index for the time series.</param>
        /// <returns>
        /// True if the indexing was changed successfully; 
        /// false if the data is empty or already starts at the specified index.
        /// </returns>
        /// <remarks>
        /// This method adjusts all time values to create a sequential series starting from startIndex.
        /// Original time relationships between points are maintained, only the absolute values change.
        /// </remarks>
        public bool ChangeIndexing(int startIndex)
        {
            if (Data.Count == 0 || MinTime == startIndex)
            {
                return false;
            }
            int startTime = startIndex;
            for (int i = 0; i < Data.Count; i++)
            {
                Data[i] = new TimeValuePair(startTime++, Data[i].Value);
            }
            return true;
        }

        /// <summary>
        /// Removes all data points from the collection.
        /// </summary>
        /// <returns>
        /// True if data points were cleared; false if the collection was already empty.
        /// </returns>
        /// <remarks>
        /// This operation cannot be undone. All time-value pairs will be permanently removed.
        /// </remarks>
        public bool ClearData()
        {
            if (Data.Count == 0)
            {
                return false;
            }
            Data.Clear();
            return true;
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets data points within a specified time range.
        /// </summary>
        /// <param name="startTime">The start time (inclusive).</param>
        /// <param name="endTime">The end time (inclusive).</param>
        /// <returns>A collection of time-value pairs within the specified range.</returns>
        /// <remarks>
        /// This method returns data points where the time value is greater than or equal to startTime
        /// and less than or equal to endTime. The returned collection maintains the original ordering.
        /// </remarks>
        public IEnumerable<TimeValuePair> GetTimeRange(int startTime, int endTime)
        {
            return Data.Where(p => p.Time >= startTime && p.Time <= endTime);
        }

        /// <summary>
        /// Gets the value at a specific time point. Returns null if no exact match exists.
        /// </summary>
        /// <param name="time">The time to look up.</param>
        /// <returns>The value at the specified time, or null if not found.</returns>
        /// <remarks>
        /// This method performs an exact match on the time value. If multiple data points
        /// have the same time value (which should be avoided), only the first match is returned.
        /// </remarks>
        public double? GetValueAtTime(int time)
        {
            return Data.FirstOrDefault(p => p.Time == time)?.Value;
        }

        /// <summary>
        /// Gets the number of data points in the collection.
        /// </summary>
        /// <returns>The count of time-value pairs in the data series.</returns>
        public int Size()
        {
            return Data.Count;
        }

        #endregion

        #region Cloning and Conversion Methods

        /// <summary>
        /// Creates a copy of this data series with an optional new name.
        /// </summary>
        /// <param name="newName">Optional new name for the copy.</param>
        /// <returns>A new DataPoints instance with copied values.</returns>
        /// <remarks>
        /// This method performs a deep copy of all data points. The returned object
        /// is completely independent from the original and can be modified separately.
        /// If no name is provided, the new instance will have the original name with " (Copy)" appended.
        /// </remarks>
        public DataPoints Clone(string? newName = null)
        {
            var clone = new DataPoints
            {
                Name = newName ?? $"{Name} (Copy)",
                Description = Description
            };

            foreach (var point in Data)
            {
                clone.Data.Add(new TimeValuePair(point.Time, point.Value));
            }

            return clone;
        }

        /// <summary>
        /// Returns a string representation of the data series.
        /// </summary>
        /// <returns>A string containing the name and number of data points.</returns>
        /// <remarks>
        /// This method is useful for displaying a concise summary of the data series.
        /// </remarks>
        public override string ToString()
        {
            return $"{Name}: {Data.Count} data points";
        }

        #endregion
    }

    /// <summary>
    /// Represents a single time-value pair in a data series.
    /// </summary>
    /// <param name="Time">The time point, represented as an integer.</param>
    /// <param name="Value">The value at the given time point, represented as a double.</param>
    /// <remarks>
    /// This is implemented as a record type for immutability and value-based equality.
    /// Time values should typically be unique within a data series to avoid ambiguity.
    /// </remarks>
    public record TimeValuePair(int Time, double Value);
}


