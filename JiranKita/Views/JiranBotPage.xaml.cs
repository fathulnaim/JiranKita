using System.Collections.ObjectModel;
using JiranKita.Models;
using JiranKita.Services;

namespace JiranKita.Views;

public partial class JiranBotPage : ContentPage
{
    public ObservableCollection<BotMessageModel> Messages { get; set; } = new();

    public JiranBotPage()
    {
        InitializeComponent();
        CvBotMessages.ItemsSource = Messages;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // 1. Dynamic greeting based on the logged-in user name
        string userName = AppSession.CurrentUserName;
        LblBotGreeting.Text = $"Hello {userName}! How can I assist you?";

        // 2. Initial welcome message on first load
        if (Messages.Count == 0)
        {
            Messages.Add(new BotMessageModel
            {
                IsUser = false,
                Title = "JiranBot AI Knowledge Assistant",
                MessageText = $"Welcome {userName}! I am ready to help answer any questions regarding residential rules, facilities, and management for {AppSession.CurrentCommunityName}.",
                SourceCitation = "Official Community AI System",
                ActionButtonText = "Explore Rules",
                RulePoints = new List<string>
                {
                    "Tap popular topics above for common questions",
                    "Or type your custom question in the input field below"
                }
            });
        }
    }

    // Quick suggestion prompt chip buttons
    private async void OnQuickPromptClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            string prompt = btn.Text.Replace("🏊 ", "").Replace("🍖 ", "").Replace("🚗 ", "").Replace("🗑️ ", "").Replace("🔨 ", "");
            await ProcessUserQueryAsync($"Tell me about {prompt} guidelines");
        }
    }

    // Send query button (➤)
    private async void OnSendQueryClicked(object sender, EventArgs e)
    {
        string query = TxtBotQuery.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(query)) return;

        TxtBotQuery.Text = string.Empty;
        await ProcessUserQueryAsync(query);
    }

    // ==============================================================
    // SMART JMB KNOWLEDGE ENGINE (ANALYZE & GENERATE RESPONSE)
    // ==============================================================
    private async Task ProcessUserQueryAsync(string query)
    {
        // 1. Add user message to conversation thread
        var userMsg = new BotMessageModel
        {
            IsUser = true,
            MessageText = query,
            Timestamp = DateTime.Now
        };
        Messages.Add(userMsg);
        CvBotMessages.ScrollTo(userMsg, position: ScrollToPosition.End, animate: true);

        // Simulate AI thinking processing delay
        await Task.Delay(400);

        // 2. Analyze query keywords
        string q = query.ToLower();
        BotMessageModel botReply;

        // TOPIC A: SWIMMING POOL
        if (q.Contains("pool") || q.Contains("kolam") || q.Contains("renang") || q.Contains("swimming"))
        {
            botReply = new BotMessageModel
            {
                IsUser = false,
                Title = "Swimming Pool Guidelines",
                MessageText = "Here are the official rules for swimming pool usage:",
                SourceCitation = "JMB By-Law 2024 (Section 3.1)",
                ActionButtonText = "Check Rules",
                RulePoints = new List<string>
                {
                    "Operating Hours: 8:00 AM – 10:00 PM daily (Closed Mondays for cleaning)",
                    "Attire: Proper swimwear is mandatory at all times",
                    "Children under 12 must be accompanied by an adult guardian",
                    "Food, alcoholic beverages, and pets are strictly prohibited"
                }
            };
        }
        // TOPIC B: BBQ PIT
        else if (q.Contains("bbq") || q.Contains("barbeque") || q.Contains("panggang"))
        {
            botReply = new BotMessageModel
            {
                IsUser = false,
                Title = "BBQ Pit Booking Guidelines",
                MessageText = "Booking information and usage fees for the BBQ area:",
                SourceCitation = "JMB By-Law 2024 (Section 4.2)",
                ActionButtonText = "Book BBQ Slot",
                RulePoints = new List<string>
                {
                    "Slot Time: 5:00 PM – 10:00 PM",
                    "Booking Fee: RM 50 (Cleaning fee)",
                    "Security Deposit: RM 100 (Refundable after cleanliness inspection)",
                    "Advance booking is required at least 3 days prior to the date"
                }
            };
        }
        // TOPIC C: VISITOR PARKING
        else if (q.Contains("parking") || q.Contains("visitor") || q.Contains("pelawat") || q.Contains("kereta"))
        {
            botReply = new BotMessageModel
            {
                IsUser = false,
                Title = "Visitor Parking Regulations",
                MessageText = "Rules regarding visitor vehicle parking:",
                SourceCitation = "JMB Traffic Regulation (Clause 2)",
                ActionButtonText = "Register Visitor",
                RulePoints = new List<string>
                {
                    "Visitors must register at the guardhouse upon entry",
                    "Maximum 6 hours complimentary parking",
                    "Overnight Charge (12:00 AM – 7:00 AM): RM 10/night",
                    "Vehicles obstructing traffic will be wheel-clamped (RM 50 release fee)"
                }
            };
        }
        // TOPIC D: BULKY WASTE / FURNITURE
        else if (q.Contains("waste") || q.Contains("sampah") || q.Contains("bulky") || q.Contains("perabot"))
        {
            botReply = new BotMessageModel
            {
                IsUser = false,
                Title = "Bulky Waste Collection Schedule",
                MessageText = "Disposal schedule for furniture, mattresses, and renovation waste:",
                SourceCitation = "KDEB Waste Management Notice",
                ActionButtonText = "View Schedule",
                RulePoints = new List<string>
                {
                    "Collection Days: Every Tuesday & Friday morning",
                    "Please place items at the Main Waste Refuse Room (Not in front of lifts)",
                    "Electrical & electronic waste (E-waste) can be dropped at the Block A Lobby bin"
                }
            };
        }
        // TOPIC E: HOUSE RENOVATION
        else if (q.Contains("renovate") || q.Contains("renovasi") || q.Contains("tukul") || q.Contains("bising") || q.Contains("ubah suai"))
        {
            botReply = new BotMessageModel
            {
                IsUser = false,
                Title = "House Renovation Guidelines",
                MessageText = "Rules regarding unit renovation work:",
                SourceCitation = "Building Maintenance Act (Strata 2013)",
                ActionButtonText = "Apply Permit",
                RulePoints = new List<string>
                {
                    "Working Hours: Monday – Friday (9:00 AM – 5:00 PM only)",
                    "Noisy work is strictly prohibited on Saturdays, Sundays & Public Holidays",
                    "Renovation Deposit: RM 500 (Payable to the JMB office prior to work start)"
                }
            };
        }
        // TOPIC F: CONTACT JMB / OFFICE
        else if (q.Contains("jmb") || q.Contains("office") || q.Contains("pejabat") || q.Contains("telefon") || q.Contains("contact"))
        {
            botReply = new BotMessageModel
            {
                IsUser = false,
                Title = "JMB Management Office Contact",
                MessageText = $"Management office details for {AppSession.CurrentCommunityName}:",
                SourceCitation = "Official Management Directory",
                ActionButtonText = "Call Office",
                RulePoints = new List<string>
                {
                    "Operating Hours: Monday – Friday (9:00 AM – 5:30 PM), Saturday (9:00 AM – 1:00 PM)",
                    "Location: Management Office Ground Floor",
                    "Phone: +60 19-332 1100 (Office Line & Complaints)"
                }
            };
        }
        // GENERAL FALLBACK RESPONSE
        else
        {
            botReply = new BotMessageModel
            {
                IsUser = false,
                Title = "Community Query Assistant",
                MessageText = $"Thank you for your query. Regarding '{query}', the best approach is to contact the JMB management office directly or check the official community notice board.",
                SourceCitation = "JiranKita Knowledge Base",
                ActionButtonText = "Contact JMB",
                RulePoints = new List<string>
                {
                    "You can also create a post on the 'Feed' tab so fellow neighbours can assist you",
                    "For emergencies, please use the SOS button on the 'Alerts' tab"
                }
            };
        }

        // 3. Append bot reply to message collection thread
        Messages.Add(botReply);
        CvBotMessages.ScrollTo(botReply, position: ScrollToPosition.End, animate: true);
    }

    // Action button handler inside bot responses
    private async void OnBotActionClicked(object sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            await DisplayAlert(
                btn.Text,
                $"The '{btn.Text}' action has been recorded. Please contact the JMB management office to confirm your slot.",
                "Understood"
            );
        }
    }
}