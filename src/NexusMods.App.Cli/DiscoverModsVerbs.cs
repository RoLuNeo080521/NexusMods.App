using System.IO.Compression;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Loadouts;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.CLI;

/// <summary>
/// CLI verbs for the mod-discovery feature.
/// </summary>
public static class DiscoverModsVerbs
{
    public static IServiceCollection AddDiscoverModsVerbs(this IServiceCollection services) =>
        services.AddVerb(() => DiscoverMods);

    [Verb("discover-mods", "List (and optionally import) mods already installed in a game folder")]
    private static async Task<int> DiscoverMods(
        [Injected] IRenderer renderer,
        [Option("g", "game", "Game id, e.g. RedEngine.Cyberpunk2077")] string gameId,
        [Option("p", "path", "Absolute path to the game install folder")] string path,
        [Injected] IFileSystem fileSystem,
        [Injected] IEnumerable<IModDiscoverer> discoverers,
        [Injected] IConnection connection,
        [Injected] ILibraryService library,
        [Injected] ILoadoutManager loadoutManager,
        [Injected] CancellationToken cancellationToken,
        [Option("i", "import", "Import discovered mods into a loadout (default: list only)", isOptional: true)] bool import = false,
        [Option("l", "loadout", "Loadout name to import into. Required with --import when multiple loadouts exist for the game.", isOptional: true)] string? loadoutName = null)
    {
        var discoverer = discoverers.FirstOrDefault(d => d.GameId.ToString().Equals(gameId, StringComparison.OrdinalIgnoreCase));
        if (discoverer is null)
        {
            var known = string.Join(", ", discoverers.Select(d => d.GameId.ToString()));
            await renderer.Error($"No mod discoverer for game id '{gameId}'. Known: {known}");
            return 1;
        }

        AbsolutePath gameFolder;
        try
        {
            gameFolder = fileSystem.FromUnsanitizedFullPath(path);
        }
        catch (Exception e)
        {
            await renderer.Error($"Invalid path '{path}': {e.Message}");
            return 1;
        }

        if (!gameFolder.DirectoryExists())
        {
            await renderer.Error($"Game folder does not exist: {gameFolder}");
            return 1;
        }

        await renderer.TextLine($"Scanning {gameFolder} for {discoverer.GameId} mods...");

        var discovered = new List<DiscoveredMod>();
        await foreach (var mod in discoverer.DiscoverAsync(gameFolder, cancellationToken))
        {
            var version = string.IsNullOrEmpty(mod.Version) ? "" : $" v{mod.Version}";
            await renderer.TextLine($"  [{mod.Type}] {mod.Name}{version}  ({mod.Files.Count} file(s))");
            discovered.Add(mod);
        }
        await renderer.TextLine($"Total: {discovered.Count} mod(s) discovered.");

        if (!import || discovered.Count == 0) return 0;

        // Resolve the target loadout
        var targetLoadout = ResolveLoadout(connection, gameId, loadoutName, out var resolveError);
        if (!targetLoadout.HasValue)
        {
            await renderer.Error(resolveError!);
            return 1;
        }

        await renderer.TextLine($"Importing into loadout '{targetLoadout.Value.Name}'...");

        var tempDir = fileSystem.GetKnownPath(KnownPath.TempDirectory) / "nma-mod-import";
        if (!tempDir.DirectoryExists()) tempDir.CreateDirectory();

        var imported = 0;
        var failed = 0;
        foreach (var mod in discovered)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                AbsolutePath fileToImport;
                if (mod.RootPath.FileExists)
                {
                    // Raw `.archive` files have no installer-recognizable structure
                    // on their own — SimpleOverlayModInstaller looks for files under
                    // `archive/pc/mod/`. Wrap into a zip with that layout.
                    fileToImport = mod.Type == "Archive"
                        ? CreateZipForArchiveFile(mod.RootPath, tempDir)
                        : mod.RootPath;
                }
                else
                {
                    fileToImport = CreateZipFromFolder(mod.RootPath, mod.Name, tempDir);
                }

                var localFile = await library.AddLocalFile(fileToImport);
                var installResult = await loadoutManager.InstallItem(localFile.AsLibraryFile().AsLibraryItem(), targetLoadout.Value.LoadoutId);

                if (installResult.LoadoutItemGroup.HasValue)
                {
                    await renderer.TextLine($"  ✓ {mod.Name}");
                    imported++;
                }
                else
                {
                    await renderer.TextLine($"  ✗ {mod.Name} (no installer matched)");
                    failed++;
                }
            }
            catch (Exception e)
            {
                await renderer.TextLine($"  ✗ {mod.Name} ({e.Message})");
                failed++;
            }
        }

        await renderer.TextLine($"Imported {imported}/{discovered.Count} mod(s). {failed} failed.");
        return failed == 0 ? 0 : 2;
    }

    private static Optional<Loadout.ReadOnly> ResolveLoadout(IConnection connection, string gameId, string? loadoutName, out string? error)
    {
        var matching = Loadout.All(connection.Db)
            .Where(l => l.IsVisible() && l.InstallationInstance.Game.GameId.ToString().Equals(gameId, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matching.Count == 0)
        {
            error = $"No loadout exists for game '{gameId}'. Create one in the UI first.";
            return Optional<Loadout.ReadOnly>.None;
        }

        if (loadoutName is null)
        {
            if (matching.Count == 1)
            {
                error = null;
                return matching[0];
            }

            var available = string.Join(", ", matching.Select(l => $"'{l.Name}'"));
            error = $"Multiple loadouts exist for '{gameId}'. Use --loadout to pick one. Available: {available}.";
            return Optional<Loadout.ReadOnly>.None;
        }

        var match = matching.FirstOrDefault(l => l.Name.Equals(loadoutName, StringComparison.Ordinal));
        if (!match.IsValid())
        {
            var available = string.Join(", ", matching.Select(l => $"'{l.Name}'"));
            error = $"Loadout '{loadoutName}' not found for '{gameId}'. Available: {available}.";
            return Optional<Loadout.ReadOnly>.None;
        }

        error = null;
        return match;
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
