using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Shepherd
{
    public static class ConfigurationBuilderExtensions
    {
        public static void AddJsonFilesFromDirectory(this IConfigurationBuilder builder, string directoryPath)
        {
            if (Directory.Exists(directoryPath))
            {
                foreach (var file in RecurseForFiles(directoryPath, "*.json"))
                {
                    builder.AddJsonFile(file, true);
                }
            }
        }

        private static IEnumerable<string> RecurseForFiles(string rootDirectory, string searchPattern)
        {
            foreach (var directory in Directory.EnumerateDirectories(rootDirectory))
            {
                foreach (var file in RecurseForFiles(directory, searchPattern))
                {
                    yield return file;
                }
            }

            foreach (var file in Directory.EnumerateFiles(rootDirectory, searchPattern))
            {
                yield return file;
            }
        }
    }
}