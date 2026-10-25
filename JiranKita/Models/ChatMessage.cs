using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JiranKita.Models;

public class ChatMessage : INotifyPropertyChanged
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;

    public string SenderName { get; set; } = string.Empty;
    public string SenderUnit { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public bool IsSelf { get; set; }
    public bool IsNotSelf => !IsSelf;

    // ==========================================
    // CIRI WHATSAPP: LENCANA PEMBAHAGI TARIKH
    // ==========================================
    private bool _showDateHeader = false;
    public bool ShowDateHeader
    {
        get => _showDateHeader;
        set { _showDateHeader = value; OnPropertyChanged(); }
    }

    private string _dateHeaderDisplay = string.Empty;
    public string DateHeaderDisplay
    {
        get => _dateHeaderDisplay;
        set { _dateHeaderDisplay = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}