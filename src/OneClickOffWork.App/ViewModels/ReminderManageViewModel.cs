using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using System.Windows.Input;
using Microsoft.Win32;
using OneClickOffWork.Commands;
using OneClickOffWork.Models;

namespace OneClickOffWork.ViewModels;

public sealed class ReminderManageViewModel : ViewModelBase, IDisposable
{
    private readonly AppServices _services;
    private readonly MainViewModel _main;
    private readonly DispatcherTimer _autoSaveTimer;
    private ReminderItem? _selectedItem;

    public ReminderManageViewModel(AppServices services, MainViewModel main)
    {
        _services = services;
        _main = main;
        Items = new ObservableCollection<ReminderItem>(_services.Reminders.Items.OrderBy(x => x.SortOrder));
        foreach (var item in Items) item.PropertyChanged += OnItemPropertyChanged;

        _autoSaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        _autoSaveTimer.Tick += AutoSaveTimerTick;

        AddCommand = new AsyncRelayCommand(AddAsync);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedItem is not null);
        MoveUpCommand = new AsyncRelayCommand(MoveUpAsync, () => SelectedItem is not null);
        MoveDownCommand = new AsyncRelayCommand(MoveDownAsync, () => SelectedItem is not null);
        RestoreDefaultsCommand = new AsyncRelayCommand(RestoreDefaultsAsync);
        ExportCommand = new AsyncRelayCommand(ExportAsync);
        ImportCommand = new AsyncRelayCommand(ImportAsync);
    }

    public ObservableCollection<ReminderItem> Items { get; }
    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }
    public ICommand RestoreDefaultsCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ImportCommand { get; }

    public ReminderItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            RaiseCommandStates();
        }
    }

    private async Task AddAsync()
    {
        var item = new ReminderItem
        {
            Title = "新的注意事项",
            Content = "写下下班前需要确认的内容。",
            SortOrder = Items.Count + 1
        };
        item.PropertyChanged += OnItemPropertyChanged;
        Items.Add(item);
        SelectedItem = item;
        await SaveAsync();
    }

    private async Task DeleteAsync()
    {
        if (SelectedItem is null) return;
        SelectedItem.PropertyChanged -= OnItemPropertyChanged;
        Items.Remove(SelectedItem);
        SelectedItem = Items.FirstOrDefault();
        await SaveAsync();
        _services.Notifications.Toast("注意事项已删除");
    }

    private async Task SaveAsync()
    {
        _autoSaveTimer.Stop();
        var now = DateTime.Now;
        for (var i = 0; i < Items.Count; i++)
        {
            Items[i].SortOrder = i + 1;
            Items[i].UpdatedAt = now;
        }
        _services.Reminders.Items = Items.ToList();
        await _services.Reminders.SaveAsync();
        _main.RefreshComputed();
    }

    private async Task MoveUpAsync()
    {
        if (SelectedItem is null) return;
        var index = Items.IndexOf(SelectedItem);
        if (index <= 0) return;
        Items.Move(index, index - 1);
        await SaveAsync();
    }

    private async Task MoveDownAsync()
    {
        if (SelectedItem is null) return;
        var index = Items.IndexOf(SelectedItem);
        if (index < 0 || index >= Items.Count - 1) return;
        Items.Move(index, index + 1);
        await SaveAsync();
    }

    private async Task RestoreDefaultsAsync()
    {
        UnsubscribeItems();
        await _services.Reminders.RestoreDefaultsAsync();
        Items.Clear();
        foreach (var item in _services.Reminders.Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Add(item);
        }
        _services.Notifications.Toast("已恢复默认注意事项");
        _main.RefreshComputed();
    }

    private async Task ExportAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "JSON 文件|*.json", FileName = "reminders.json" };
        if (dialog.ShowDialog() != true) return;
        await _services.Reminders.ExportAsync(dialog.FileName);
        _services.Notifications.Toast("数据备份成功");
    }

    private async Task ImportAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "JSON 文件|*.json" };
        if (dialog.ShowDialog() != true) return;
        UnsubscribeItems();
        await _services.Reminders.ImportAsync(dialog.FileName);
        Items.Clear();
        foreach (var item in _services.Reminders.Items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Add(item);
        }
        _services.Notifications.Toast("数据恢复成功");
        _main.RefreshComputed();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(ReminderItem.Title) or nameof(ReminderItem.Content) or nameof(ReminderItem.IsEnabled)))
        {
            return;
        }

        _autoSaveTimer.Stop();
        _autoSaveTimer.Start();
    }

    private async void AutoSaveTimerTick(object? sender, EventArgs e)
    {
        _autoSaveTimer.Stop();
        await SaveAsync();
    }

    private void RaiseCommandStates()
    {
        foreach (var command in new[] { DeleteCommand, MoveUpCommand, MoveDownCommand })
        {
            if (command is RelayCommand relay) relay.RaiseCanExecuteChanged();
            if (command is AsyncRelayCommand asyncRelay) asyncRelay.RaiseCanExecuteChanged();
        }
    }

    private void UnsubscribeItems()
    {
        foreach (var item in Items)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }
    }

    public void Dispose()
    {
        _autoSaveTimer.Stop();
        _autoSaveTimer.Tick -= AutoSaveTimerTick;
        UnsubscribeItems();
    }
}
