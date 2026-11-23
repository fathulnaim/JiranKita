using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JiranKita.Models;

// Data model representing a single chat message in Firebase; implements INotifyPropertyChanged so dynamic UI badges update live
public class ChatMessage : INotifyPropertyChanged
{
    // Unique local GUID and the database node key assigned by Firebase Realtime Database
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FirebaseKey { get; set; } = string.Empty;

    // Core message payload fields serialized to and from Firebase JSON
    public string SenderName { get; set; } = string.Empty;
    public string SenderUnit { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    // Ownership flags evaluated in ChatPage.xaml.cs to toggle right-aligned green vs. left-aligned white bubbles
    public bool IsSelf { get; set; }
    public bool IsNotSelf => !IsSelf;

    // ====================================================
    // WHATSAPP-STYLE FEATURE: DYNAMIC DATE DIVIDER BADGES
    // ====================================================

    // Backing field & observable property controlling whether the date badge ("Today", "Yesterday", date) renders above this bubble
    private bool _showDateHeader = false;
    public bool ShowDateHeader
    {
        get => _showDateHeader;
        set { _showDateHeader = value; OnPropertyChanged(); }
    }

    // Backing field & observable property holding the formatted date label displayed inside the divider badge
    private string _dateHeaderDisplay = string.Empty;
    public string DateHeaderDisplay
    {
        get => _dateHeaderDisplay;
        set { _dateHeaderDisplay = value; OnPropertyChanged(); }
    }

    // Standard INotifyPropertyChanged event and helper that notify bound XAML controls whenever a property value changes
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}