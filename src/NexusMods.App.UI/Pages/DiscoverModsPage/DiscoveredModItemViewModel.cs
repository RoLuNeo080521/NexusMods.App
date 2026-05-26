using NexusMods.Sdk.Games;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.DiscoverModsPage;

public class DiscoveredModItemViewModel : ReactiveObject
{
    public DiscoveredMod Mod { get; }

    public string TypeText => Mod.Type;
    public string NameText => Mod.Name;
    public string VersionText => string.IsNullOrEmpty(Mod.Version) ? "" : $"v{Mod.Version}";
    public string FilesText => $"{Mod.Files.Count} file(s)";

    [Reactive] public bool IsSelected { get; set; } = true;
    [Reactive] public string StatusText { get; set; } = "";

    public DiscoveredModItemViewModel(DiscoveredMod mod)
    {
        Mod = mod;
    }
}
