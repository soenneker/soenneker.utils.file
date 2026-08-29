[![](https://img.shields.io/nuget/v/Soenneker.Utils.File.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.File/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.file/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.utils.file/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Utils.File.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Utils.File/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.utils.file/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.utils.file/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Utils.File
A utility library encapsulating asynchronous file IO operations.

## Installation

```bash
dotnet add package Soenneker.Utils.File
```

## Quick start

```csharp
using Soenneker.Utils.File.Registrars;

services.AddFileUtilAsSingleton();
```

Then inject `IFileUtil` wherever you need it.

## Common operations

- `Read()` - Reads the entire content of a file as a string. Returns a task containing the file content as a string.
- `TryRead()` - Tries to read the content of a file as a string. Logs a warning on failure. Returns a task containing the file content as a string or null on failure.
- `ReadAsLines()` - Reads the entire content of a file as a list of strings, where each line is an item in the list. Returns a task containing a list of strings representing the file's lines.
- `ReadToBytes()` - Reads the entire content of a file as a byte array. Returns a task containing the file content as a byte array.
- `ReadToMemoryStream()` - Reads the entire content of a file into a memory stream. Returns a task containing a memory stream with the file content.
- `Write()` - Writes a string to a file. Returns a task representing the operation.
- `WriteAllLines()` - Writes all lines of text to a file. Returns a task representing the operation.
- `Append()` - Appends text to the end of an existing file, creating the file if it does not exist. Returns a `ValueTask` that completes when the write finishes.
- `Copy()` - Copies a file from one path to another. Returns a task representing the operation.
- `Move()` - Moves a file from one path to another. Deletes the source file after copying. Returns a task representing the operation.
- `Delete()` - Deletes the specified file if it exists. Returns a `ValueTask` that completes when the deletion (if any) finishes.
- `Exists()` - Checks whether a file exists at the given path.

The package also includes 18 additional operations for more specialized cases.
