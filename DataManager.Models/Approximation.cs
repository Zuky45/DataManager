using System.Text;
using DataManager.Data;
using MathNet.Numerics;

namespace DataManager.Models
{
    /// <summary>
    /// Implements polynomial approximation modeling for time series data.
    /// </summary>
    /// <remarks>
    /// This class provides functionality to approximate data points using polynomial functions
    /// of varying degrees. It can be used to model trends in time series data, 
    /// identify underlying patterns, and make predictions based on the fitted polynomial.
    /// </remarks>
    public class Approximation : Model
    {
        #region Properties

        /// <summary>
        /// Gets or sets the degree of the polynomial approximation.
        /// </summary>
        /// <remarks>
        /// Values between 1 (linear) and 5 (quintic) are recommended.
        /// Higher degrees can lead to overfitting, especially with limited data points.
        /// </remarks>
        public int Degree { get; set; } = 1;

        /// <summary>
        /// Gets or sets the coefficients of the polynomial approximation.
        /// </summary>
        /// <remarks>
        /// The coefficients are ordered from lowest to highest degree.
        /// For example, in a quadratic equation ax² + bx + c, the coefficients
        /// would be [c, b, a].
        /// </remarks>
        public double[] Coefficients { get; set; } = [];

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Approximation"/> class
        /// with the specified dataset and polynomial degree.
        /// </summary>
        /// <param name="dataSet">The dataset to approximate.</param>
        /// <param name="degree">The degree of the polynomial approximation.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dataSet"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="degree"/> is less than 1.</exception>
        public Approximation(DataPoints dataSet, int degree)
        {
            if (degree < 1)
                throw new ArgumentOutOfRangeException(nameof(degree), "Polynomial degree must be at least 1");

            Name = $"Polynomial Approximation for: {dataSet.Name} degree: {degree}";
            Description = $"Polynomial approximation model (degree {degree})";
            OriginalDataSet = dataSet ?? throw new ArgumentNullException(nameof(dataSet), "Dataset cannot be null");
            Degree = degree;
            CalculateModel();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Calculates the polynomial approximation model based on the original dataset.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the original dataset is null or empty.</exception>
        public override void CalculateModel()
        {
            if (OriginalDataSet == null || OriginalDataSet.Data.Count == 0)
            {
                throw new InvalidOperationException("No original data set provided or data set is empty.");
            }

            // Extract x (time) and y (value) data points
            var dataPoints = OriginalDataSet.Data.OrderBy(p => p.Time).ToList();
            double[] xValues = [.. dataPoints.Select(p => (double)p.Time)];
            double[] yValues = [.. dataPoints.Select(p => p.Value)];

            // Calculate polynomial approximation coefficients using MathNet.Numerics
            Coefficients = Fit.Polynomial(xValues, yValues, Degree);

            // Create a new data set for the model
            ModelDataSet = new DataPoints
            {
                Name = $"Polynomial Approximation (Degree {Degree})",
                Description = $"Polynomial approximation of degree {Degree} for {OriginalDataSet.Name}"
            };

            // Generate model data points over the same time range
            int minTime = OriginalDataSet.MinTime;
            int maxTime = OriginalDataSet.MaxTime;

            for (int time = minTime; time <= maxTime; time++)
            {
                double approximatedValue = EvaluatePolynomial(time);
                ModelDataSet.AddDataPoint(time, approximatedValue);
            }
        }

        /// <summary>
        /// Calculates the Mean Squared Error between original and approximated values.
        /// </summary>
        /// <returns>The Mean Squared Error (MSE) value.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when original or model dataset is null.
        /// </exception>
        public override double GetMSE()
        {
            if (OriginalDataSet == null || ModelDataSet == null)
            {
                throw new InvalidOperationException("Original data set or model data set is null.");
            }

            double[] actualValues = [.. OriginalDataSet.Data.Select(p => p.Value)];
            double[] predictedValues = [.. ModelDataSet.Data.Select(p => p.Value)];

            return CalculateMSE(actualValues, predictedValues);
        }

        /// <summary>
        /// Calculates the R-squared (coefficient of determination) value for the approximation,
        /// which indicates how well the model fits the original data.
        /// </summary>
        /// <returns>R-squared value between 0 and 1, where higher values indicate better fit.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when original or model dataset is null.
        /// </exception>
        public override double GetRSquared()
        {
            if (OriginalDataSet == null || ModelDataSet == null)
            {
                throw new InvalidOperationException("Original data set or model data set is null.");
            }

            double[] actualValues = [.. OriginalDataSet.Data.Select(p => p.Value)];
            double[] predictedValues = [.. ModelDataSet.Data.Select(p => p.Value)];

            return GoodnessOfFit.RSquared(actualValues, predictedValues);
        }

        /// <summary>
        /// Calculates the approximation for a specific time range.
        /// </summary>
        /// <param name="startTime">The start time (inclusive).</param>
        /// <param name="endTime">The end time (inclusive).</param>
        /// <returns>A new DataPoints object containing the approximation for the specified time range.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the model has not been calculated yet.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when end time is less than start time.
        /// </exception>
        public DataPoints CalculateApproximationForTimeRange(int startTime, int endTime)
        {
            if (Coefficients.Length == 0)
            {
                throw new InvalidOperationException("Model has not been calculated yet. Call CalculateModel first.");
            }

            if (endTime < startTime)
            {
                throw new ArgumentException("End time must be greater than or equal to start time.");
            }

            var approximationDataSet = new DataPoints
            {
                Name = $"Approximation [{startTime}-{endTime}]",
                Description = $"Polynomial approximation (degree {Degree}) from time {startTime} to {endTime}"
            };

            // Generate data points for the specified time range
            for (int time = startTime; time <= endTime; time++)
            {
                double approximatedValue = EvaluatePolynomial(time);
                approximationDataSet.AddDataPoint(time, approximatedValue);
            }

            return approximationDataSet;
        }

        /// <summary>
        /// Sets the original data set and calculates the approximation model.
        /// </summary>
        /// <param name="dataSet">The data set to approximate.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when the data set is null or empty.
        /// </exception>
        public void SetDataAndCalculate(DataPoints dataSet)
        {
            if (dataSet == null || dataSet.Data.Count == 0)
            {
                throw new ArgumentException("Data set is null or empty.");
            }

            OriginalDataSet = dataSet;
            CalculateModel();
        }

        /// <summary>
        /// Gets a string representation of the polynomial function.
        /// </summary>
        /// <returns>A string representation of the polynomial function.</returns>
        /// <example>
        /// For a quadratic model with coefficients [3, -2, 0.5], the formula would be:
        /// "3 - 2x + 0.5x^2"
        /// </example>
        public string GetPolynomialFormula()
        {
            if (Coefficients.Length == 0)
            {
                return "Model not calculated";
            }

            StringBuilder formula = new();

            for (int i = 0; i < Coefficients.Length; i++)
            {
                double coefficient = Coefficients[i];

                // Skip terms with zero coefficients
                if (Math.Abs(coefficient) < 1e-10)
                {
                    continue;
                }

                // Add plus sign if not the first term and coefficient is positive
                if (i > 0 && coefficient > 0)
                {
                    formula.Append(" + ");
                }
                // Add minus sign if not the first term and coefficient is negative
                else if (i > 0 && coefficient < 0)
                {
                    formula.Append(" - ");
                    coefficient = Math.Abs(coefficient);
                }
                else if (coefficient < 0)
                {
                    formula.Append('-');
                    coefficient = Math.Abs(coefficient);
                }

                // Format the coefficient (with reasonable precision)
                string coefficientStr = Math.Round(coefficient, 4).ToString();

                // Degree 0 term (constant)
                if (i == 0)
                {
                    formula.Append(coefficientStr);
                }
                // Degree 1 term (linear)
                else if (i == 1)
                {
                    formula.Append($"{coefficientStr}x");
                }
                // Higher degree terms
                else
                {
                    formula.Append($"{coefficientStr}x^{i}");
                }
            }

            return formula.Length > 0 ? formula.ToString() : "0";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculates the polynomial approximation for a specific time value.
        /// </summary>
        /// <param name="time">The time value to evaluate.</param>
        /// <returns>The approximated value at the given time.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the model has not been calculated yet.
        /// </exception>
        /// <remarks>
        /// Uses Horner's method for efficient polynomial evaluation.
        /// </remarks>
        private double EvaluatePolynomial(double time)
        {
            if (Coefficients.Length == 0)
            {
                throw new InvalidOperationException("Model has not been calculated yet. Call CalculateModel first.");
            }

            // Horner's method for polynomial evaluation
            double result = 0;
            for (int i = Coefficients.Length - 1; i >= 0; i--)
            {
                result = result * time + Coefficients[i];
            }
            return result;
        }

        #endregion
    }
}


