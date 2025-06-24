using Newtonsoft.Json.Linq;
using DataManager.Data;

namespace DataManager.API
{
    /// <summary>
    /// Represents the available time series functions supported by the Alpha Vantage API.
    /// </summary>
    public enum Function
    {
        /// <summary>
        /// Daily time series with trading day close values.
        /// </summary>
        TIME_SERIES_DAILY,

        /// <summary>
        /// Intraday time series with 5-minute interval data.
        /// </summary>
        TIME_SERIES_INTRADAY,

        /// <summary>
        /// Weekly time series with trading week close values.
        /// </summary>
        TIME_SERIES_WEEKLY,

        /// <summary>
        /// Monthly time series with trading month close values.
        /// </summary>
        TIME_SERIES_MONTHLY
    }

    /// <summary>
    /// Provides methods for retrieving financial data from the Alpha Vantage API.
    /// </summary>
    public static class APIHandler
    {
        // Replace with your Alpha Vantage API key
        /// <summary>
        /// Alpha Vantage API key used for authentication.
        /// </summary>
        /// <remarks>
        /// Replace this with your own API key if needed. 
        /// Free API keys can be obtained from https://www.alphavantage.co/support/#api-key
        /// </remarks>
        private const string API_KEY = "UQJPQ6PTAGFOV7UM"; // Alternative key: HS1TR2HVQ5ITLJCB  LUJM21HKAG713O4V  UQJPQ6PTAGFOV7UM

        /// <summary>
        /// Retrieves stock data from Alpha Vantage API and returns it as a DataPoints object.
        /// </summary>
        /// <param name="symbol">The stock symbol (e.g., "AAPL" for Apple).</param>
        /// <param name="function">The time series function to use (daily, weekly, monthly, or intraday).</param>
        /// <returns>
        /// A DataPoints object containing the requested stock data with time values starting from 1.
        /// If an error occurs, returns a DataPoints object with error information.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the stock symbol is null or empty.</exception>
        /// <remarks>
        /// The data is normalized with sequential time indices starting from 1, 
        /// with the most recent data points appearing first.
        /// </remarks>
        public static async Task<DataPoints> GetDataSet(string symbol, Function function)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Stock symbol cannot be null or empty.", nameof(symbol));

            // Alpha Vantage API URL
            string url = $"https://www.alphavantage.co/query?function={function}&symbol={symbol}&apikey={API_KEY}";

            using HttpClient client = new();
            try
            {
                // Fetch data from API
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode(); // Throw if HTTP error

                string json = await response.Content.ReadAsStringAsync();
                JObject data = JObject.Parse(json);

                // Check for API errors
                if (data["Error Message"] != null)
                    throw new Exception($"Alpha Vantage error: {data["Error Message"]}");

                // Extract time series data
                var timeSeries = data[ResolveSeriesType(function)] ?? throw new Exception("No time series data found in the API response.");

                // Create a new DataPoints object
                var dataSet = new DataPoints
                {
                    Name = $"{symbol} Stock Data",
                    Description = $"Daily stock prices for {symbol} from Alpha Vantage"
                };

                // Process the time series data
                int timeIndex = 1;
                foreach (var day in timeSeries)
                {
                    var date = day.Path.Split('.').Last();
                    var values = day.First;
                    // Extract the closing price
                    if (values is JObject dayData &&
                        dayData.TryGetValue("4. close", out JToken? closeToken) &&
                        closeToken != null)
                    {
                        string token = closeToken.ToString();
                        token = token.Replace(".", ",");
                        if (!double.TryParse(token, out double closePrice))
                        {
                            continue;
                        }
                        // Add data point with sequential numbering
                        dataSet.AddDataPoint(timeIndex++, closePrice);
                    }
                }

                return dataSet;
            }
            catch (Exception ex)
            {
                // Return an error DataPoints object
                return new DataPoints
                {
                    Name = $"{symbol} - Error",
                    Description = $"Error retrieving data: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Resolves the function enum to the appropriate JSON property name in the Alpha Vantage API response.
        /// </summary>
        /// <param name="function">The time series function.</param>
        /// <returns>The corresponding JSON property name in the API response.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when an unsupported function value is provided.
        /// </exception>
        private static string ResolveSeriesType(Function function)
        {
            return function switch
            {
                Function.TIME_SERIES_DAILY => "Time Series (Daily)",
                Function.TIME_SERIES_INTRADAY => "Time Series (5min)",
                Function.TIME_SERIES_WEEKLY => "Weekly Time Series",
                Function.TIME_SERIES_MONTHLY => "Monthly Time Series",
                _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
                
            };
        }

        /// <summary>
        /// Provides a list of commonly traded stock symbols available through the Alpha Vantage API.
        /// </summary>
        /// <returns>A list of popular stock symbols that can be used with the API.</returns>
        /// <remarks>
        /// This is a static list of popular symbols. The Alpha Vantage API supports thousands
        /// of additional symbols not included in this list.
        /// </remarks>
        public static List<string> AvailabelSymbols()
        {
            return
            [
                "AAPL",  // Apple Inc.
                "MSFT",  // Microsoft Corporation
                "GOOGL", // Alphabet Inc. (Google)
                "AMZN",  // Amazon.com Inc.
                "TSLA",  // Tesla Inc.
                "FB",    // Meta Platforms Inc. (Facebook)
                "NFLX",  // Netflix Inc.
                "NVDA",  // NVIDIA Corporation
                "INTC",  // Intel Corporation
                "AMD"    // Advanced Micro Devices Inc.
            ];
        }
    }
}


