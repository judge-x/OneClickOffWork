using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OneClickOffWork.Models;

public sealed class ReminderItem : INotifyPropertyChanged
{
    private string _title = "";
    private string _content = "";
    private bool _isEnabled = true;
    private int _sortOrder;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get => _title; set => SetField(ref _title, value); }
    public string Content { get => _content; set => SetField(ref _content, value); }
    public bool IsEnabled { get => _isEnabled; set => SetField(ref _isEnabled, value); }
    public int SortOrder { get => _sortOrder; set => SetField(ref _sortOrder, value); }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
