using System.Linq;
using NexusMods.Paths;

namespace NexusMods.Sdk.IO;

/// <summary>
/// Represents a Stream Factory backed by a native path on the FileSystem.
/// </summary>
public class NativeFileStreamFactory : IStreamFactory
{
    /// <summary/>
    /// <param name="file">Absolute path of the file.</param>
    public NativeFileStreamFactory(AbsolutePath file) => Path = file;

    /// <inheritdoc/>
    public RelativePath FileName => Path.Name;

    /// <summary>
    /// Absolute path to the file.
    /// </summary>
    public AbsolutePath Path { get; }

    /// <inheritdoc />
    public ValueTask<Stream> GetStreamAsync()
    {
        if (Path.FileExists)
            return new ValueTask<Stream>(Path.Open(FileMode.Open, FileAccess.Read, FileShare.Read));

        var parent = Path.Parent;
        if (parent.DirectoryExists())
        {
            var fileName = Path.Name;
            var matches = parent.EnumerateFiles()
                .Where(f => f.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count > 0)
                return new ValueTask<Stream>(matches[0].Open(FileMode.Open, FileAccess.Read, FileShare.Read));
        }

        return new ValueTask<Stream>(Path.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
    }
}
