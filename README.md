[![](https://img.shields.io/nuget/v/Soenneker.Utils.File.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.File/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.file/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.file/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.File.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.File/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.file/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.file/actions/workflows/codeql.yml)

# Soenneker.Utils.File

DI-friendly asynchronous file reading, writing, copying, moving, enumeration, metadata, and cleanup operations.

## Installation

```bash
dotnet add package Soenneker.Utils.File
```

## Registration

```csharp
builder.Services.AddFileUtilAsSingleton();
```

`AddFileUtilAsScoped()` is also available and registers the memory-stream dependency with the matching lifetime.

## Read and write

```csharp
string text = await files.Read(path, cancellationToken: cancellationToken);
List<string> lines = await files.ReadAsLines(path, cancellationToken: cancellationToken);
byte[] bytes = await files.ReadToBytes(path, cancellationToken: cancellationToken);

await files.Write(path, text, cancellationToken: cancellationToken);
await files.Append(path, "next entry\n", cancellationToken: cancellationToken);
```

Text writes use UTF-8 without a byte-order mark. Reads detect a byte-order mark when present. Whole-file methods materialize the complete contents; use `OpenRead()` for large or streaming workloads.

`TryRead()` and `TryReadToHashSet()` return `null` and optionally log when reading fails. Requested cancellation still throws `OperationCanceledException` rather than being converted to `null`.

## Stream ownership

```csharp
await using FileStream input = files.OpenRead(path);
await using FileStream output = files.OpenWrite(destinationPath);

using MemoryStream buffered = await files.ReadToMemoryStream(path, cancellationToken: cancellationToken);
```

The caller owns every stream returned by this package. `OpenWrite()` creates missing parent directories and truncates an existing file. `Write(path, sourceStream)` copies from the source's current position, leaves the source open, and replaces the destination contents.

## Copy and move

```csharp
await files.Copy(sourcePath, destinationPath, cancellationToken: cancellationToken);
await files.Move(sourcePath, archivePath, cancellationToken: cancellationToken);
await files.CopyRecursively(sourceDirectory, destinationDirectory, cancellationToken: cancellationToken);
```

`Copy()` creates the destination parent and overwrites the destination. It is not transactional: a failed or cancelled copy can leave a partial destination.

`Move()` uses the filesystem's native overwrite move when available. Its cross-volume fallback copies to a temporary file beside the destination, publishes that completed copy, then deletes the source. If cancellation occurs after publication but before source deletion, both complete files can remain.

Recursive copy skips inaccessible entries and does not follow symbolic links, junctions, or other reparse points. It copies discovered files and their required parent directories; empty source directories are not reproduced.

## Deletion and bulk mutation

```csharp
bool removed = await files.DeleteIfExists(path, cancellationToken: cancellationToken);
await files.DeleteAll(directory, cancellationToken: cancellationToken);
```

`DeleteAll()` removes only files immediately inside the directory, not descendants. `TryDelete()`, `TryDeleteIfExists()`, and `TryDeleteAll()` convert I/O failures to `false`, but propagate requested cancellation.

Bulk rename, attribute removal, recursive copy, and multi-file deletion are incremental operations. Cancellation or a later conflict does not undo earlier filesystem changes. Resolve and validate any user-controlled root path before calling destructive methods.
