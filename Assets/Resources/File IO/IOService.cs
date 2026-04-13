using System;
using System.IO;
using System.Threading.Tasks;

namespace Services.IO
{
    public struct FileData
    {
        public string _fileContent;
        public string _fileName;
        public string _filePath;

        public FileData(string fileContent, string fileName, string filePath)
        {
            _fileContent = fileContent;
            _fileName = fileName;
            _filePath = filePath;
        }

        /// <summary>Full path including file name (e.g. /tmp/logs/app.txt).</summary>
        public string FullPath => Path.Combine(_filePath, _fileName);
    }

    public interface IFileService
    {
        Task WriteAsync(FileData fileData);
        Task<FileData> ReadAsync(string fileName, string filePath);
        Task<bool> ExistsAsync(string fileName, string filePath);
        Task DeleteAsync(string fileName, string filePath);
    }

    public sealed class FileService : IFileService
    {
        // -------------------------------------------------------------------------
        // Write
        // -------------------------------------------------------------------------

        /// <summary>
        /// Writes <see cref="FileData._fileContent"/> to disk, creating any missing
        /// directories along the way.
        /// </summary>
        public async Task WriteAsync(FileData fileData)
        {
            if (string.IsNullOrWhiteSpace(fileData._fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileData));

            if (string.IsNullOrWhiteSpace(fileData._filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(fileData));

            Directory.CreateDirectory(fileData._filePath);

            await File.WriteAllTextAsync(fileData.FullPath, fileData._fileContent ?? string.Empty);
        }

        // -------------------------------------------------------------------------
        // Read
        // -------------------------------------------------------------------------

        /// <summary>
        /// Reads the file at <paramref name="filePath"/>/<paramref name="fileName"/>
        /// and returns a populated <see cref="FileData"/>.
        /// </summary>
        public async Task<FileData> ReadAsync(string fileName, string filePath)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));

            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            string fullPath = Path.Combine(filePath, fileName);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"File not found: {fullPath}");

            string content = await File.ReadAllTextAsync(fullPath);

            return new FileData(content, fileName, filePath);
        }

        // -------------------------------------------------------------------------
        // Exists
        // -------------------------------------------------------------------------

        /// <summary>Returns <c>true</c> if the file exists on disk.</summary>
        public Task<bool> ExistsAsync(string fileName, string filePath)
        {
            string fullPath = Path.Combine(filePath, fileName);
            return Task.FromResult(File.Exists(fullPath));
        }

        // -------------------------------------------------------------------------
        // Delete
        // -------------------------------------------------------------------------

        /// <summary>Deletes the file if it exists; does nothing otherwise.</summary>
        public Task DeleteAsync(string fileName, string filePath)
        {
            string fullPath = Path.Combine(filePath, fileName);

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }
    }
}
