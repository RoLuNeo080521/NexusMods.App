using R3;

namespace NexusMods.App.UI.Overlays;

public interface IWelcomeOverlayViewModel : IOverlayViewModel
{
    ReactiveCommand CommandOpenGitHub { get; }

    ReactiveCommand<Unit> CommandLogIn { get; }
    ReactiveCommand<Unit> CommandLogOut { get; }
    IReadOnlyBindableReactiveProperty<bool> IsLoggedIn { get; }

    ReactiveCommand CommandClose { get; }
}
