using System.Collections.ObjectModel;
using System.Windows.Input;
using OneClickOffWork.Commands;
using OneClickOffWork.Models;

namespace OneClickOffWork.ViewModels;

public sealed class TodayTaskManageViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private TodayTaskItem? _selectedItem;

    public TodayTaskManageViewModel(AppServices services)
    {
        _services = services;
        Items = new ObservableCollection<TodayTaskItem>(_services.TodayTasks.TodayItems());
        AddCommand = new RelayCommand(Add);
        DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => SelectedItem is not null);
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        MoveUpCommand = new RelayCommand(MoveUp, () => SelectedItem is not null);
        MoveDownCommand = new RelayCommand(MoveDown, () => SelectedItem is not null);
    }

    public ObservableCollection<TodayTaskItem> Items { get; }
    public ICommand AddCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand MoveUpCommand { get; }
    public ICommand MoveDownCommand { get; }

    public TodayTaskItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            SetProperty(ref _selectedItem, value);
            RaiseCommandStates();
        }
    }

    private void Add()
    {
        var item = new TodayTaskItem
        {
            Title = "新的今日任务",
            WorkDate = DateOnly.FromDateTime(DateTime.Now),
            SortOrder = Items.Count + 1
        };
        Items.Add(item);
        SelectedItem = item;
    }

    private async Task DeleteAsync()
    {
        if (SelectedItem is null) return;
        Items.Remove(SelectedItem);
        SelectedItem = Items.FirstOrDefault();
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        for (var i = 0; i < Items.Count; i++)
        {
            Items[i].WorkDate = today;
            Items[i].SortOrder = i + 1;
            Items[i].UpdatedAt = DateTime.Now;
        }

        _services.TodayTasks.Items.RemoveAll(x => x.WorkDate == today);
        _services.TodayTasks.Items.AddRange(Items);
        await _services.TodayTasks.SaveAsync();
        _services.Notifications.Toast("今日任务保存成功");
    }

    private void MoveUp()
    {
        if (SelectedItem is null) return;
        var index = Items.IndexOf(SelectedItem);
        if (index <= 0) return;
        Items.Move(index, index - 1);
    }

    private void MoveDown()
    {
        if (SelectedItem is null) return;
        var index = Items.IndexOf(SelectedItem);
        if (index < 0 || index >= Items.Count - 1) return;
        Items.Move(index, index + 1);
    }

    private void RaiseCommandStates()
    {
        foreach (var command in new[] { DeleteCommand, MoveUpCommand, MoveDownCommand })
        {
            if (command is RelayCommand relay) relay.RaiseCanExecuteChanged();
            if (command is AsyncRelayCommand asyncRelay) asyncRelay.RaiseCanExecuteChanged();
        }
    }
}
