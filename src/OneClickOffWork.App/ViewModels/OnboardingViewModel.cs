using System.Windows.Input;
using OneClickOffWork.Commands;

namespace OneClickOffWork.ViewModels;

public sealed class OnboardingViewModel
{
    private readonly AppServices _services;
    private readonly MainViewModel _main;

    public OnboardingViewModel(AppServices services, MainViewModel main)
    {
        _services = services;
        _main = main;
        FinishCommand = new AsyncRelayCommand(FinishAsync);
    }

    public ICommand FinishCommand { get; }

    private async Task FinishAsync()
    {
        _services.Settings.Current.ShowOnboarding = false;
        await _services.Settings.SaveAsync();
        _main.NavigateHome();
    }
}
