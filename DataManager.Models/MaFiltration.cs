using DataManager.Data;
using MathNet.Numerics;

namespace DataManager.Models
{
    /// <summary>
    /// Implements a Moving Average (MA) filtration model that smooths time series data 
    /// by averaging values within a sliding window.
    /// </summary>
    /// <remarks>
    /// The Moving Average filter is a simple but effective method for removing high-frequency noise
    /// from time series data while preserving lower-frequency trends. Each output value
    /// is the average of a certain number (specified by the window size) of consecutive input values.
    /// </remarks>
    public class MaFiltration : Model
    {
        #region Properties

        /// <summary>
        /// Gets or sets the window size for the moving average calculation.
        /// </summary>
        /// <remarks>
        /// The window size determines how many consecutive data points are averaged.
        /// Larger window sizes produce smoother results but may lag more behind significant changes.
        /// </remarks>
        public int WindowSize { get; set; } = 5;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MaFiltration"/> class
        /// with the specified data and window size.
        /// </summary>
        /// <param name="data">The original data to filter.</param>
        /// <param name="windowSize">The window size for the moving average calculation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="windowSize"/> is less than 1.</exception>
        public MaFiltration(DataPoints data, int windowSize)
        {
            if (windowSize < 1)
                throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be at least 1");

            Name = $"Moving Average Filtration for: {data.Name} window: {windowSize}";
            Description = "This model applies a moving average filter to smooth time series data.";
            OriginalDataSet = data ?? throw new ArgumentNullException(nameof(data), "Data points cannot be null");
            WindowSize = windowSize;
            CalculateModel();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the moving average model from the original dataset.
        /// </summary>
        /// <exception cref="Exception">Thrown when original data is not set or empty.</exception>
        public override void CalculateModel()
        {
            // Validate data
            if (OriginalDataSet == null || OriginalDataSet.Size() == 0)
                throw new Exception("Original data not set or empty");

            // Extract values from original data
            double[] data = [.. OriginalDataSet.Data.Select(p => p.Value)];

            // Apply moving average algorithm
            double[] movingAverages = CalculateMovingAverage(data, WindowSize);

            // Create a new dataset for the model results
            ModelDataSet = new DataPoints
            {
                Name = "Moving Average",
                Description = $"Moving average with window size {WindowSize}"
            };

            int startIndex = OriginalDataSet.Size() - (OriginalDataSet.Size() - WindowSize) + (OriginalDataSet.MinTime - 1) ;

            // Add the results to the model dataset
            for (int i = 0; i < movingAverages.Length; i++)
            {
                ModelDataSet.AddDataPoint(startIndex + i, movingAverages[i]);
            }
        }

        /// <summary>
        /// Calculates the Mean Squared Error (MSE) between the original and filtered data.
        /// </summary>
        /// <returns>The mean squared error value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when original data set or model data set is not set.
        /// </exception>
        public override double GetMSE()
        {
            if (OriginalDataSet == null || ModelDataSet == null)
                throw new InvalidOperationException("Original data set or model data set is not set");

            // Extract values from within the same time range
            double[] originalValues = [.. OriginalDataSet.GetTimeRange(
                ModelDataSet.MinTime, ModelDataSet.MaxTime).Select(p => p.Value)];

            double[] modelValues = [.. ModelDataSet.Data.Select(p => p.Value)];

            // Calculate MSE using base class method
            return CalculateMSE(originalValues, modelValues);
        }

        /// <summary>
        /// Calculates the R-squared value (coefficient of determination) between 
        /// the original and the filtered data.
        /// </summary>
        /// <returns>
        /// The R-squared value, representing the proportion of the variance in the 
        /// dependent variable that is predictable from the independent variable.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when original data set or model data set is not set.
        /// </exception>
        public override double GetRSquared()
        {
            if (OriginalDataSet == null || ModelDataSet == null)
                throw new InvalidOperationException("Original data set or model data set is not set");

            // Extract values from within the same time range
            double[] originalValues = [.. OriginalDataSet.GetTimeRange(
                ModelDataSet.MinTime, ModelDataSet.MaxTime).Select(p => p.Value)];

            double[] modelValues = [.. ModelDataSet.Data.Select(p => p.Value)];

            // Use MathNet.Numerics to calculate R-squared
            return GoodnessOfFit.RSquared(originalValues, modelValues);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates a simple moving average over the provided data.
        /// </summary>
        /// <param name="data">The input data array.</param>
        /// <param name="windowSize">The size of the moving average window.</param>
        /// <returns>An array of moving average values.</returns>
        private static double[] CalculateMovingAverage(double[] data, int windowSize)
        {
            // Calculate the output size: input size - window size + 1
            double[] smoothedData = new double[data.Length - windowSize + 1];

            // Apply moving average for each position
            for (int i = 0; i < smoothedData.Length; i++)
            {
                double sum = 0;

                // Sum values in the current window
                for (int j = 0; j < windowSize; j++)
                {
                    sum += data[i + j];
                }

                // Calculate average for this window
                smoothedData[i] = sum / windowSize;
            }

            return smoothedData;
        }

        #endregion
    }
}

