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
        // Cas normal : le fichier existe avec la bonne casse
        if (Path.FileExists)
            return new ValueTask<Stream>(Path.Open(FileMode.Open, FileAccess.Read, FileShare.Read));

        // Sur Linux (filesystem sensible à la casse), recherche insensible à la casse
        var parent = Path.Parent;
        if (parent.DirectoryExists())
        {
            var fileName = Path.Name.ToString();
            foreach (var f in parent.EnumerateFiles())
            {
                if (string.Equals(f.Name.ToString(), fileName, StringComparison.OrdinalIgnoreCase))
                    return new ValueTask<Stream>(f.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
            }
        }

        // Fichier vraiment introuvable, on laisse l'exception naturelle se produire
        return new ValueTask<Stream>(Path.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
    }
}
