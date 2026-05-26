using NexusMods.Paths;

namespace NexusMods.Sdk.Games;

/// <summary>
/// Scans a game's install folder for mods that were installed outside of the app
/// (manually copied files, mods left behind from another mod manager, etc.).
/// Implementations are per-game and encode that game's mod-folder conventions.
/// </summary>
public interface IModDiscoverer
{
    /// <summary>
    /// The game this discoverer applies to.
    /// </summary>
    GameId GameId { get; }

    /// <summary>
    /// Enumerates every mod the discoverer can identify under <paramref name="gameFolder"/>.
    /// </summary>
    /// <param name="gameFolder">Absolute path to the game's install root (the directory the user picked when adding the game).</param>
    /// <param name="cancellationToken">Cancellation token for early termination.</param>
    IAsyncEnumerable<DiscoveredMod> DiscoverAsync(AbsolutePath gameFolder, CancellationToken cancellationToken);
}
