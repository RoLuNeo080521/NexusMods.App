using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Cli;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.ProxyConsole;

namespace NexusMods.CLI;

/// <summary>
/// CLI verbs for the mod-discovery feature.
/// </summary>
public static class DiscoverModsVerbs
{
    public static IServiceCollection AddDiscoverModsVerbs(this IServiceCollection services) =>
        services.AddVerb(() => DiscoverMods);

    [Verb("discover-mods", "List mods already installed in a game folder, using the per-game discoverer")]
    private static async Task<int> DiscoverMods(
        [Injected] IRenderer renderer,
        [Option("g", "game", "Game id, e.g. RedEngine.Cyberpunk2077")] string gameId,
        [Option("p", "path", "Absolute path to the game install folder")] string path,
        [Injected] IFileSystem fileSystem,
        [Injected] IEnumerable<IModDiscoverer> discoverers,
        [Injected] CancellationToken cancellationToken)
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
        var count = 0;
        await foreach (var mod in discoverer.DiscoverAsync(gameFolder, cancellationToken))
        {
            var version = string.IsNullOrEmpty(mod.Version) ? "" : $" v{mod.Version}";
            await renderer.TextLine($"  [{mod.Type}] {mod.Name}{version}  ({mod.Files.Count} file(s) at {mod.RootPath})");
            count++;
        }
        await renderer.TextLine($"Total: {count} mod(s) discovered.");
        return 0;
    }
}
