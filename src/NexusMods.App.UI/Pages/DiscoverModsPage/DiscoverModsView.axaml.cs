using System.Reactive.Disposables;
using Avalonia.ReactiveUI;
using JetBrains.Annotations;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.DiscoverModsPage;

[UsedImplicitly]
public partial class DiscoverModsView : ReactiveUserControl<IDiscoverModsViewModel>
{
    public DiscoverModsView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Items, view => view.ItemsList.ItemsSource)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.StatusText, view => view.StatusTextBlock.Text)
                .DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.CanImport, view => view.ImportButton.IsEnabled)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandScan, view => view.ScanButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandImport, view => view.ImportButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandSelectAll, view => view.SelectAllButton)
                .DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.CommandDeselectAll, view => view.DeselectAllButton)
                .DisposeWith(disposables);
        });
    }
}
