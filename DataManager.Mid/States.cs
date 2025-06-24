
namespace DataManager.CentralManager
{
    /// <summary>
    /// Represents the different operational states of the application's data manager.
    /// </summary>
    public enum State
    {
        /// <summary>
        /// The manager is not performing any operations and is ready to accept new commands.
        /// </summary>
        Idle,

        /// <summary>
        /// The manager is currently loading data from a source (file, API, or database).
        /// </summary>
        Loading,

        /// <summary>
        /// Data loading operation has completed successfully.
        /// </summary>
        Loaded,

        /// <summary>
        /// An error occurred during an operation.
        /// </summary>
        Error,

        /// <summary>
        /// The manager is currently saving data to a destination (file or database).
        /// </summary>
        Saving,

        /// <summary>
        /// Data saving operation has completed successfully.
        /// </summary>
        Saved,

        /// <summary>
        /// The manager is currently performing calculations or generating a model.
        /// </summary>
        Calculating
    }
}

