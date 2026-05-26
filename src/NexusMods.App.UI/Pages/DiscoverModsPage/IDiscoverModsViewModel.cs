using System.Collections.ObjectModel;
using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.DiscoverModsPage;

public interface IDiscoverModsViewModel : IPageViewModelInterface
{
    string StatusText { get; }
    bool IsBusy { get; }
    bool CanImport { get; }

    ReadOnlyObservableCollection<DiscoveredModItemViewModel> Items { get; }

    ReactiveCommand<Unit> CommandScan { get; }
    ReactiveCommand<Unit> CommandImport { get; }
    ReactiveCommand<Unit> CommandSelectAll { get; }
    ReactiveCommand<Unit> CommandDeselectAll { get; }
}
