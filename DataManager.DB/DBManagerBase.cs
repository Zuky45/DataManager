using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DataManager.DB
{
    /// <summary>
    /// SQLite database context for the DataManager application
    /// </summary>
    public class DBManagerBase : DbContext
    {
        public DbSet<DatasetInfo> Datasets { get; set; } = null!;

        public string DbPath { get; }

        public DBManagerBase()
        {
            // Store the database in the user's AppData folder
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "datamanager.db");
        }

        // Constructor for specifying a custom database path
        public DBManagerBase(string dbPath)
        {
            DbPath = dbPath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite($"Data Source={DbPath}");
        }
    }

    /// <summary>
    /// Stores metadata about datasets in the application
    /// </summary>
    public class DatasetInfo
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public required string Name { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        // Keep track of when the dataset was created
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        // Keep track of when the dataset was last modified
        public DateTime LastModified { get; internal set; }

        // Store the actual dataset content as serialized JSON
        [Required]
        public required string DataContent { get; set; }
    }
}






