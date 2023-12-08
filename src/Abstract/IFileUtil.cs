using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading.Tasks;

namespace Soenneker.Utils.File.Abstract;

/// <summary>
/// A utility library encapsulating both async/sync file IO operations <para/>
/// </summary>
public interface IFileUtil
{
    /// <summary>
    /// Closes the file before returning
    /// </summary>
    [Pure]
    Task<string> ReadFile(string path);

    [Pure]
    Task<byte[]> ReadFileToBytes(string path);

    [Pure]
    ValueTask<string?> TryReadFile(string path, bool log = true);

    /// <summary>
    /// The read returning from this will be at the beginning. Closes file after reading into the stream. It's imperative that you close this stream after reading from it.
    /// </summary>
    [Pure]
    ValueTask<System.IO.MemoryStream> ReadFileToMemoryStream(string path);

    Task WriteAllLines(string path, IEnumerable<string> lines);

    [Pure]
    ValueTask<List<string>> ReadFileAsLines(string path);

    Task WriteFile(string fullName, string content);

    /// <summary>
    /// Will not close the incoming stream. Will close the file it wrote to. Will seek the stream to the beginning before writing.
    /// </summary>
    Task WriteFile(string path, Stream stream);

    Task WriteFile(string path, byte[] byteArray);
}