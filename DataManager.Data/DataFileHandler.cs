namespace DataManager.Data
{
    /// <summary>
    /// Provides functionality for reading and writing DataPoints objects to and from text files.
    /// </summary>
    /// <remarks>
    /// The file format used is a simple text-based format with the following structure:
    /// - Line 1: Data series name
    /// - Line 2: Data series description (optional)
    /// - Remaining lines: Time-value pairs in the format "time;value"
    /// </remarks>
    public static class DataFileHandler
    {
        /// <summary>
        /// Reads a data file and converts it to a DataPoints object.
        /// </summary>
        /// <param name="filePath">The full path to the data file to read.</param>
        /// <returns>
        /// A tuple containing:
        /// - data: The DataPoints object populated with the file contents
        /// - isSuccess: A boolean indicating whether the file was read successfully
        /// </returns>
        /// <remarks>
        /// The method performs validation on the file before reading its contents.
        /// If validation fails, a DataPoints object with the name "INVALID" is returned
        /// along with isSuccess = false.
        /// </remarks>
        /// <exception cref="System.IO.FileNotFoundException">
        /// Thrown when the specified file does not exist (through internal validation).
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// May be thrown when there are issues accessing the file.
        /// </exception>
        public static (DataPoints data, bool isSuccess) ReadDataFile(string filePath)
        {
            FileInfo fileInfo = new(filePath);
            if (!ValidateDataFile(filePath))
            {
                return (new DataPoints { Name = "INVALID" }, false);
            }

            var lines = File.ReadAllLines(fileInfo.FullName);
            var name = lines[0].TrimEnd().TrimStart();
            String? description = null;
            if (!lines[1].Contains(';'))
            {
                description = lines[1].TrimEnd().TrimStart();
            }
            var data = new DataPoints
            {
                Name = name,
                Description = description
            };
            for (int i = 2; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd().TrimStart();
                var componets = line.Split(';');
                if (componets.Length == 2)
                {
                    if (int.TryParse(componets[0], out int time) &&
                        double.TryParse(componets[1], out double value))
                    {
                        data.AddDataPoint(time, value);
                    }
                }
            }
            return (data, true);
        }

        /// <summary>
        /// Writes a DataPoints object to a text file.
        /// </summary>
        /// <param name="data">The DataPoints object to write to the file.</param>
        /// <param name="filePath">The full path to the output file.</param>
        /// <remarks>
        /// The file will be created if it doesn't exist, or overwritten if it does.
        /// The file format will have the following structure:
        /// - Line 1: Data series name
        /// - Line 2: Data series description (if available)
        /// - Remaining lines: Time-value pairs in the format "time;value"
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown when the data parameter is null.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// Thrown when there are issues writing to the file.
        /// </exception>
        /// <exception cref="System.UnauthorizedAccessException">
        /// Thrown when the application doesn't have permission to write to the specified location.
        /// </exception>
        public static void WriteDataFile(DataPoints data, string filePath)
        {
            var writer = new StreamWriter(filePath);
            writer.WriteLine(data.Name);
            if (data.Description != null)
            {
                writer.WriteLine(data.Description);
            }
            foreach (var dataPoint in data.Data)
            {
                writer.WriteLine($"{dataPoint.Time};{dataPoint.Value}");
            }
            writer.Close();
        }

        /// <summary>
        /// Validates that a file exists and has the correct format to be a data file.
        /// </summary>
        /// <param name="filePath">The full path to the file to validate.</param>
        /// <returns>True if the file exists and has valid format; otherwise, false.</returns>
        /// <remarks>
        /// A valid data file must:
        /// - Exist on disk
        /// - Contain at least 2 lines
        /// - Have non-empty first and second lines
        /// </remarks>
        private static bool ValidateDataFile(string filePath)
        {
            FileInfo fileInfo = new(filePath);
            if (!fileInfo.Exists)
            {
                return false;
            }
            var lines = File.ReadAllLines(fileInfo.FullName);
            if (lines.Length < 2)
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(lines[0]))
            {
                return false;
            }
            if (string.IsNullOrWhiteSpace(lines[1]))
            {
                return false;
            }
            return true;
        }
    }
}

