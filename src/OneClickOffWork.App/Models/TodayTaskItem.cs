using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OneClickOffWork.Models;

public sealed class TodayTaskItem : INotifyPropertyChanged
{
    private string _title = "";
    private bool _isCompleted;
    private int _sortOrder;

    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly WorkDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public string Title { get => _title; set => SetField(ref _title, value); }
    public bool IsCompleted { get => _isCompleted; set => SetField(ref _isCompleted, value); }
    public int SortOrder { get => _sortOrder; set => SetField(ref _sortOrder, value); }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        UpdatedAt = DateTime.Now;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
