using System;
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
    /// <param name="log"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the file content as a string.</returns>
    Task<string> Read(string path, bool log = true, CancellationToken cancellationToken = default);

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
    /// <param name="log"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task WriteAllLines(string path, IEnumerable<string> lines, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the entire content of a file as a byte array.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="log"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing the file content as a byte array.</returns>
    Task<byte[]> ReadToBytes(string path, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the entire content of a file into a memory stream.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="log"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing a memory stream with the file content.</returns>
    ValueTask<System.IO.MemoryStream> ReadToMemoryStream(string path, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the entire content of a file as a list of strings, where each line is an item in the list.
    /// </summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="log"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task containing a list of strings representing the file's lines.</returns>
    ValueTask<List<string>> ReadAsLines(string path, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a string to a file.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="content">The content to write to the file.</param>
    /// <param name="log"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task Write(string path, string content, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the content of a stream to a file.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="stream">The stream containing the content to write.</param>
    /// <param name="log"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    ValueTask Write(string path, Stream stream, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a byte array to a file.
    /// </summary>
    /// <param name="path">The path of the file to write to.</param>
    /// <param name="byteArray">The byte array to write.</param>
    /// <param name="log"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    Task Write(string path, byte[] byteArray, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a file from one path to another. Deletes the source file after copying.
    /// </summary>
    /// <param name="sourcePath">The path of the source file.</param>
    /// <param name="destinationPath">The path of the destination file.</param>
    /// <param name="log"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    ValueTask Move(string sourcePath, string destinationPath, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Copies a file from one path to another.
    /// </summary>
    /// <param name="sourcePath">The path of the source file.</param>
    /// <param name="destinationPath">The path of the destination file.</param>
    /// <param name="log"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the operation.</returns>
    ValueTask Copy(string sourcePath, string destinationPath, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recursively copies all directories and files from the specified source directory to the destination directory.
    /// </summary>
    /// <param name="sourceDir">The path to the source directory to copy from.</param>
    /// <param name="destinationDir">The path to the destination directory to copy to.</param>
    /// <param name="log"></param>
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
    ValueTask CopyRecursively(string sourceDir, string destinationDir, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends text to the end of an existing file, creating the file if it does not exist.
    /// </summary>
    /// <param name="path">The full path of the file to append to.</param>
    /// <param name="content">The text to append.</param>
    /// <param name="log">Emit a debug-level log message when <see langword="true"/> (default).</param>
    /// <param name="cancellationToken">Token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the write finishes.</returns>
    Task Append(string path, string content, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Appends a sequence of lines to the end of an existing file, creating the file if it does not exist.
    /// </summary>
    /// <param name="path">The full path of the file to append to.</param>
    /// <param name="lines">Lines that will be written, each followed by the platform newline.</param>
    /// <param name="log">Emit a debug-level log message when <see langword="true"/> (default).</param>
    /// <param name="cancellationToken">Token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when all lines are written.</returns>
    Task Append(string path, IEnumerable<string> lines, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified file if it exists.
    /// </summary>
    /// <param name="path">Full path of the file to delete.</param>
    /// <param name="ignoreMissing">
    /// When <see langword="true"/> (default), no exception is thrown if the file is not found.
    /// </param>
    /// <param name="log">Emit a debug-level log message when <see langword="true"/> (default).</param>
    /// <param name="cancellationToken">Token that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the deletion (if any) finishes.</returns>
    ValueTask Delete(string path, bool ignoreMissing = true, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a file exists at the given path.
    /// </summary>
    /// <param name="path">Full path to test.</param>
    /// <param name="cancellationToken">Token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise <see langword="false"/>.</returns>
    ValueTask<bool> Exists(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the size of a file in bytes.
    /// </summary>
    /// <param name="path">Full path of the file.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// The file size in bytes, or <see langword="null"/> if the file does not exist.
    /// </returns>
    ValueTask<long?> GetSize(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the UTC timestamp of the most recent modification to a file.
    /// </summary>
    /// <param name="path">Full path of the file.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>
    /// A <see cref="DateTimeOffset"/> representing last modification time, or
    /// <see langword="null"/> if the file does not exist.
    /// </returns>
    ValueTask<DateTimeOffset?> GetLastModified(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to delete <paramref name="path"/> (regardless of existence).  
    /// Returns <c>true</c> on success; <c>false</c> when an exception occurs.
    /// </summary>
    ValueTask<bool> TryDelete(string path, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to delete <paramref name="path"/> when it exists.
    /// Swallows any exception and returns whether the delete succeeded.
    /// </summary>
    ValueTask<bool> TryDeleteIfExists(string path, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all files in <paramref name="directory"/>.
    /// </summary>
    ValueTask DeleteAll(string directory, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to delete all files in <paramref name="directory"/>.
    /// Returns <c>false</c> if an exception occurs.
    /// </summary>
    ValueTask<bool> TryDeleteAll(string directory, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to remove the <see cref="FileAttributes.ReadOnly"/> and
    /// <see cref="FileAttributes.Archive"/> flags from all files under <paramref name="directory"/>.
    /// Returns <c>false</c> if an exception occurs.
    /// </summary>
    ValueTask<bool> TryRemoveReadonlyAndArchiveAttributesFromAll(string directory, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes <paramref name="path"/> if it exists.
    /// Returns <c>true</c> when a file was found and removed; otherwise <c>false</c>.
    /// </summary>
    ValueTask<bool> DeleteIfExists(string path, bool log = true, CancellationToken cancellationToken = default);

    ValueTask<HashSet<string>> ReadToHashSet(string path, IEqualityComparer<string>? comparer = null, bool trim = true, bool ignoreEmpty = true,
        bool log = true, CancellationToken cancellationToken = default);

    ValueTask<HashSet<string>?> TryReadToHashSet(string path, IEqualityComparer<string>? comparer = null, bool trim = true,
        bool ignoreEmpty = true, bool log = true, CancellationToken cancellationToken = default);

    ValueTask<DirectoryInfo> CreateDirectory(string path, CancellationToken cancellationToken = default);

    string[] GetAllFileNamesInDirectoryRecursively(string directory, bool log = true);

    List<FileInfo> GetAllFileInfoInDirectoryRecursivelySafe(string directory, bool log = true);
}