using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;
using Soenneker.Utils.FileSync.Abstract;

namespace Soenneker.Utils.File.Abstract;

/// <summary>
/// A utility library encapsulating both async/sync file IO operations <para/>
/// (Adds asynchronous support to <see cref="IFileUtilSync"/>)
/// </summary>
public interface IFileUtil : IFileUtilSync
{
    /// <summary>
    /// Closes the file before returning
    /// </summary>
    [Pure]
    new Task<string> ReadFile(string path);

    [Pure]
    new Task<byte[]> ReadFileToBytes(string path);

    [Pure]
    ValueTask<string?> TryReadFile(string path, bool log = true);

    /// <summary>
    /// The read returning from this will be at the beginning. Closes file after reading into the stream. It's imperative that you close this stream after reading from it.
    /// </summary>
    [Pure]
    ValueTask<System.IO.MemoryStream> ReadFileToMemoryStream(string path);

    new Task WriteAllLines(string path, IEnumerable<string> lines);

    [Pure]
    new ValueTask<List<string>> ReadFileAsLines(string path);

    new Task WriteFile(string fullName, string content);

    /// <summary>
    /// Will not close the incoming stream. Will close the file it wrote to. Will seek the stream to the beginning before writing.
    /// </summary>
    new Task WriteFile(string path, Stream stream);

    new Task WriteFile(string path, byte[] byteArray);
}