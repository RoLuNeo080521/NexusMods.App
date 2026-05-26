using System.IO.Compression;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.CLI;

public enum ImportOutcome
{
    Installed,
    NoInstallerMatched,
    Failed,
}

public record ImportResult(DiscoveredMod Mod, ImportOutcome Outcome, string? ErrorMessage = null);

/// <summary>
/// Shared service that imports a <see cref="DiscoveredMod"/> into a loadout.
/// Used by the CLI verb and the UI page.
/// </summary>
public class ModImporter
{
    private readonly IFileSystem _fileSystem;
    private readonly ILibraryService _library;
    private readonly ILoadoutManager _loadoutManager;

    public ModImporter(IFileSystem fileSystem, ILibraryService library, ILoadoutManager loadoutManager)
    {
        _fileSystem = fileSystem;
        _library = library;
        _loadoutManager = loadoutManager;
    }

    public async Task<ImportResult> ImportAsync(DiscoveredMod mod, LoadoutId loadoutId, CancellationToken cancellationToken)
    {
        var tempDir = _fileSystem.GetKnownPath(KnownPath.TempDirectory) / "nma-mod-import";
        if (!tempDir.DirectoryExists()) tempDir.CreateDirectory();

        try
        {
            AbsolutePath fileToImport;
            if (mod.RootPath.FileExists)
            {
                fileToImport = mod.Type == "Archive"
                    ? CreateZipForArchiveFile(mod.RootPath, tempDir)
                    : mod.RootPath;
            }
            else
            {
                fileToImport = CreateZipFromFolder(mod.RootPath, mod.Name, tempDir);
            }

            var localFile = await _library.AddLocalFile(fileToImport);
            var installResult = await _loadoutManager.InstallItem(localFile.AsLibraryFile().AsLibraryItem(), loadoutId);

            return installResult.LoadoutItemGroup.HasValue
                ? new ImportResult(mod, ImportOutcome.Installed)
                : new ImportResult(mod, ImportOutcome.NoInstallerMatched);
        }
        catch (Exception e)
        {
            return new ImportResult(mod, ImportOutcome.Failed, e.Message);
        }
    }

    /// <summary>
    /// Wraps a raw `.archive` file into a zip placing it under
    /// `archive/pc/mod/&lt;filename&gt;.archive` so SimpleOverlayModInstaller
    /// recognizes it.
    /// </summary>
    private static AbsolutePath CreateZipForArchiveFile(AbsolutePath archiveFile, AbsolutePath tempDir)
    {
        var fileName = archiveFile.FileName.ToString();
        var safeName = string.Concat(fileName.Split(Path.GetInvalidFileNameChars()));
        var zipPath = tempDir / $"{safeName}-{Guid.NewGuid():N}.zip";

        using var fileStream = File.Create(zipPath.ToString());
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        archive.CreateEntryFromFile(archiveFile.ToString(), $"archive/pc/mod/{fileName}");

        return zipPath;
    }

    /// <summary>
    /// Zips a mod folder into a temp file, preserving the folder name as the
    /// top-level entry so installers like RedModInstaller can find their
    /// expected `mods/&lt;name&gt;/info.json` structure.
    /// </summary>
    private static AbsolutePath CreateZipFromFolder(AbsolutePath folder, string modName, AbsolutePath tempDir)
    {
        var safeName = string.Concat(modName.Split(Path.GetInvalidFileNameChars()));
        var zipPath = tempDir / $"{safeName}-{Guid.NewGuid():N}.zip";

        using var fileStream = File.Create(zipPath.ToString());
        using var archive = new ZipArchive(fileStream, ZipArchiveMode.Create);

        var folderName = folder.FileName.ToString();
        foreach (var file in folder.EnumerateFiles(recursive: true))
        {
            var relative = file.RelativeTo(folder).ToString();
            var entryName = $"{folderName}/{relative.Replace('\\', '/')}";
            archive.CreateEntryFromFile(file.ToString(), entryName);
        }

        return zipPath;
    }
}
