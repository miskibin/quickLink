using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using quickLink.Constants;

namespace quickLink.Services.Helpers
{
    /// <summary>
    /// Helper class for initializing services with common configuration
    /// </summary>
    public static class ServiceInitializer
    {
        private static readonly Lazy<string> AppDataPath = new(GetAppDataFolder);
        private static readonly Lazy<JsonSerializerOptions> JsonOptions = new(CreateJsonOptions);

        /// <summary>
        /// Gets the application data folder path, creating it if necessary
        /// </summary>
        public static string GetAppDataFolder()
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataFolder, AppConstants.Folders.DataFolder);

            if (!Directory.Exists(appFolder))
            {
                Directory.CreateDirectory(appFolder);
            }

            return appFolder;
        }

        /// <summary>
        /// Gets the cached app data folder path
        /// </summary>
        public static string AppDataFolderPath => AppDataPath.Value;

        /// <summary>
        /// Gets the standard JSON serializer options for the application
        /// </summary>
        public static JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions(JsonOptions.Value);
        }

        private static JsonSerializerOptions CreateJsonOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        /// <summary>
        /// Gets the full path for a data file
        /// </summary>
        public static string GetDataFilePath(string filename)
        {
            return Path.Combine(AppDataFolderPath, filename);
        }
    }
}
