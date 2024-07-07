using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
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
    Task<string> ReadFile(string path, CancellationToken cancellationToken = default);

    [Pure]
    Task<byte[]> ReadFileToBytes(string path, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<string?> TryReadFile(string path, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// The read returning from this will be at the beginning. Closes file after reading into the stream. It's imperative that you close this stream after reading from it.
    /// </summary>
    [Pure]
    ValueTask<System.IO.MemoryStream> ReadFileToMemoryStream(string path, CancellationToken cancellationToken = default);

    Task WriteAllLines(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<List<string>> ReadFileAsLines(string path, CancellationToken cancellationToken = default);

    Task WriteFile(string fullName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Will not close the incoming stream. Will close the file it wrote to. Will seek the stream to the beginning before writing.
    /// </summary>
    ValueTask WriteFile(string path, Stream stream, CancellationToken cancellationToken = default);

    Task WriteFile(string path, byte[] byteArray, CancellationToken cancellationToken = default);
}