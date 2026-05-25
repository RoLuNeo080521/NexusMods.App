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

        // Sur Linux (filesystem sensible à la casse), on cherche le fichier
        // avec une correspondance insensible à la casse dans le dossier parent.
        // Cela corrige les cas où le jeu stocke des noms de fichiers en majuscules
        // (ex: ccBGSSSE037-Curios.esl) mais Linux les a en minuscules.
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

        // Fichier vraiment introuvable, on laisse l'exception naturelle se produire
        return new ValueTask<Stream>(Path.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
    }
}using NexusMods.Paths;

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
        return new ValueTask<Stream>(Path.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
    }
}
