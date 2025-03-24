using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Utils.File.Abstract;

/// <summary>
/// Provides utility methods for file operations, including reading, writing, copying, and moving files.
/// </summary>
public interface IFileUtil
{
    /// <summary>
    /// Reads the entire content of a file as a string.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the file content as a string.</returns>
    ValueTask<string> Read(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tries to read the content of a file as a string. Logs a warning on failure.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="log">Indicates whether to log the operation.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the file content as a string or null on failure.</returns>
    ValueTask<string?> TryRead(string path, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes all lines of text to a file.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="lines">The lines of text to write.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    ValueTask WriteAllLines(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the entire content of a file as a byte array.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the file content as a byte array.</returns>
    ValueTask<byte[]> ReadToBytes(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the entire content of a file into a memory stream.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing a memory stream with the file content.</returns>
    ValueTask<System.IO.MemoryStream> ReadToMemoryStream(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the entire content of a file as a list of strings, where each line is an item in the list.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing a list of strings representing the file's lines.</returns>
    ValueTask<List<string>> ReadAsLines(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a string to a file.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="content">The content to write to the file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    ValueTask Write(string path, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the content of a stream to a file.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="stream">The stream containing the content to write.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    ValueTask Write(string path, Stream stream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a byte array to a file.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="byteArray">The byte array to write.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    ValueTask Write(string path, byte[] byteArray, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file from one path to another. Deletes the source file after copying.
    /// </summary>
    /// <param name="sourcePath">The path of the source file.</param>
    /// <param name="destinationPath">The path of the destination file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    ValueTask Move(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file from one path to another.
    /// </summary>
    /// <param name="sourcePath">The path of the source file.</param>
    /// <param name="destinationPath">The path of the destination file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    ValueTask Copy(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recursively copies all directories and files from the specified source directory to the destination directory.
    /// </summary>
    /// <param name="sourceDir">The path to the source directory to copy from.</param>
    /// <param name="destinationDir">The path to the destination directory to copy to.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous copy operation.</returns>
    /// <remarks>
    /// This method first creates the entire directory structure of the source directory in the destination,
    /// and then copies all files into the corresponding locations.
    /// </remarks>
    /// <example>
    /// <code>
    /// await CopyRecursively("C:\\SourceFolder", "D:\\BackupFolder");
    /// </code>
    /// </example>
    ValueTask CopyRecursively(string sourceDir, string destinationDir, CancellationToken cancellationToken = default);
}