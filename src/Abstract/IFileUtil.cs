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
    Task<byte[]> ReadToBytes(string path, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<string?> TryRead(string path, bool log = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// The read returning from this will be at the beginning. Closes file after reading into the stream. It's imperative that you close this stream after reading from it.
    /// </summary>
    [Pure]
    ValueTask<System.IO.MemoryStream> ReadToMemoryStream(string path, CancellationToken cancellationToken = default);

    Task WriteAllLines(string path, IEnumerable<string> lines, CancellationToken cancellationToken = default);

    [Pure]
    ValueTask<List<string>> ReadAsLines(string path, CancellationToken cancellationToken = default);

    Task Write(string fullName, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Will not close the incoming stream. Will close the file it wrote to. Will seek the stream to the beginning before writing.
    /// </summary>
    ValueTask Write(string path, Stream stream, CancellationToken cancellationToken = default);

    Task Write(string path, byte[] byteArray, CancellationToken cancellationToken = default);

    ValueTask Move(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);

    ValueTask Copy(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);
}