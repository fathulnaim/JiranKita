using System.Collections.ObjectModel;
using System.Text;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class JiranBotPage : ContentPage
{
    private readonly FirebaseService _firebaseService = new FirebaseService();
    private readonly GeminiService _geminiService = new GeminiService();
    private bool _isProcessing = false; // Prevents duplicate concurrent requests

    public ObservableCollection<BotMessageModel> Messages { get; set; } = new();

    public JiranBotPage()
    {
        InitializeComponent();
        CvBotMessages.ItemsSource = Messages;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        string userName = string.IsNullOrWhiteSpace(AppSession.CurrentUserName)
            ? "Resident"
            : AppSession.CurrentUserName;

        string communityName = string.IsNullOrWhiteSpace(AppSession.CurrentCommunityName)
            ? "your neighbourhood"
            : AppSession.CurrentCommunityName;

        // Dalam JiranBotPage.xaml.cs (OnAppearing)
        LblBotGreeting.Text = $"Hello {userName} ({AppSession.CurrentCommunityCode})! How can I assist you?";

        // Initial welcome card on first load
        if (Messages.Count == 0)
        {
            Messages.Add(new BotMessageModel
            {
                IsUser = false,
                Title = "JiranBot AI Knowledge Assistant",
                MessageText = $"Welcome {userName}! Ask me anything about house rules, facilities, FAQs, or ask me to summarize the {communityName} group chat.",
                SourceCitation = $"Official Handbook & Chat AI · {communityName}",
                ActionButtonText = "Summarize Chat",
                RulePoints = new List<string>
                {
                    "Tap popular topics above for instant handbook guidelines",
                    "Ask \"Summarize group chat\" to catch up on neighbour discussions"
                }
            });
        }
    }

    // Quick suggestion prompt chip buttons
    private async void OnQuickPromptClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && !string.IsNullOrWhiteSpace(btn.Text))
        {
            string raw = btn.Text.Trim();
            string topic = raw.Contains(' ')
                ? raw.Substring(raw.IndexOf(' ') + 1).Trim()
                : raw;

            if (topic.Contains("Summarize", StringComparison.OrdinalIgnoreCase) ||
                topic.Contains("Chat", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessUserQueryAsync("Summarize the latest community group chat");
            }
            else
            {
                await ProcessUserQueryAsync($"What are the official rules, fees, and operating hours for {topic}?");
            }
        }
    }

    // Send query button (➤)
    private async void OnSendQueryClicked(object sender, EventArgs e)
    {
        string query = TxtBotQuery.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query)) return;

        TxtBotQuery.Text = string.Empty;
        await ProcessUserQueryAsync(query);
    }

    // Routes resident requests either to Community Chat Summarization or Rules/FAQ PDF Q&A
    private async Task ProcessUserQueryAsync(string query)
    {
        if (_isProcessing) return;
        _isProcessing = true;

        try
        {
            // 1. Append user message bubble
            var userMsg = new BotMessageModel
            {
                IsUser = true,
                MessageText = query,
                Timestamp = DateTime.Now
            };
            Messages.Add(userMsg);
            CvBotMessages.ScrollTo(userMsg, position: ScrollToPosition.End, animate: true);

            // Capture conversation history before adding the temporary thinking card
            var conversationHistory = Messages.ToList();

            // Detect group chat summary intent
            string qLower = query.ToLower();
            bool isChatSummaryRequest =
                (qLower.Contains("summarize") || qLower.Contains("summary") || qLower.Contains("catch up") || qLower.Contains("recap")) &&
                (qLower.Contains("chat") || qLower.Contains("group") || qLower.Contains("discussion") || qLower.Contains("messages"));

            // 2. Show live thinking indicator card
            var thinkingCard = new BotMessageModel
            {
                IsUser = false,
                Title = "JiranBot AI",
                MessageText = isChatSummaryRequest
                    ? "✨ Reading recent community group chat messages..."
                    : "✨ Searching community rules & FAQ handbook...",
                SourceCitation = AppSession.CurrentCommunityName,
                ActionButtonText = "Please Wait",
                RulePoints = new List<string>()
            };
            Messages.Add(thinkingCard);
            CvBotMessages.ScrollTo(thinkingCard, position: ScrollToPosition.End, animate: true);

            BotMessageModel botReply;
            try
            {
                if (isChatSummaryRequest)
                {
                    string transcript = await BuildRecentGroupChatTranscriptAsync();
                    botReply = await _geminiService.SummarizeCommunityChatAsync(
                        transcript,
                        AppSession.CurrentCommunityName,
                        AppSession.CurrentUserName
                    );
                }
                else
                {
                    botReply = await _geminiService.AskJiranBotAsync(
                        query,
                        AppSession.CurrentCommunityCode,
                        AppSession.CurrentCommunityName,
                        AppSession.CurrentUserName,
                        conversationHistory
                    );
                }
            }
            catch (Exception error)
            {
                botReply = new BotMessageModel
                {
                    IsUser = false,
                    Title = "Connection Issue",
                    MessageText = $"Unable to reach JiranBot AI right now: {error.Message}",
                    SourceCitation = "JiranKita System",
                    ActionButtonText = "Retry Later",
                    RulePoints = new List<string>
                    {
                        "Please verify your internet connection and Gemini API key"
                    }
                };
            }
            finally
            {
                Messages.Remove(thinkingCard);
            }

            // 3. Append AI response card
            Messages.Add(botReply);
            CvBotMessages.ScrollTo(botReply, position: ScrollToPosition.End, animate: true);
        }
        finally
        {
            _isProcessing = false;
        }
    }

    // Fetches the last 30 group chat messages from Firebase and formats them into a transcript
    private async Task<string> BuildRecentGroupChatTranscriptAsync()
    {
        if (string.IsNullOrWhiteSpace(AppSession.CurrentCommunityCode))
            return string.Empty;

        var recentMessages = await _firebaseService.GetChatMessagesAsync(
            AppSession.CurrentCommunityCode,
            "community_general"
        );

        if (recentMessages == null || recentMessages.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var msg in recentMessages.TakeLast(30))
        {
            sb.AppendLine($"[{msg.Timestamp:dd MMM h:mm tt}] {msg.SenderName} ({msg.SenderUnit}): {msg.MessageText}");
        }

        return sb.ToString();
    }

    // Action button handler inside bot response cards
    private async void OnBotActionClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            if (btn.Text == "Summarize Chat")
            {
                await ProcessUserQueryAsync("Summarize the latest community group chat");
                return;
            }

            if (btn.Text == "Explore Rules")
            {
                await ProcessUserQueryAsync("Give me a summary of the main house rules, facility hours, and FAQs in our handbook.");
                return;
            }

            if (btn.Text == "Open Chat")
            {
                await Shell.Current.GoToAsync(nameof(Views.ChatRoomPage));
                return;
            }

            await DisplayAlert(
                btn.Text,
                $"Your '{btn.Text}' request for {AppSession.CurrentCommunityName} has been noted. Please refer to the JMB office if confirmation is required.",
                "Understood"
            );
        }
    }
}