using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JiranKita.Models;

namespace JiranKita.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient = new HttpClient();

    // Keep your working Gemini API Key here
    private const string ApiKey = Secrets.GeminiApiKey;

    private const string Endpoint =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent";

    // In-memory cache keyed by CommunityCode so each PDF is read from disk only once
    private static readonly Dictionary<string, (string Base64, string FileName)> _pdfCacheByCode =
        new(StringComparer.OrdinalIgnoreCase);

    // Dynamically loads rules_{communityCode}.pdf from Resources/Raw, falling back to rules_default.pdf
    private async Task<(string? Base64, string LoadedFileName)> GetCommunityRulesPdfBase64Async(string communityCode)
    {
        string code = string.IsNullOrWhiteSpace(communityCode) ? "default" : communityCode.Trim().ToLower();
        string targetFile = $"rules_{code}.pdf";

        if (_pdfCacheByCode.TryGetValue(code, out var cached))
            return cached;

        // 1. Try loading community-specific handbook (e.g., rules_kaj82.pdf or rules_bgi99.pdf)
        string? loadedBase64 = await TryReadAssetAsBase64Async(targetFile);
        string resolvedName = targetFile;

        // 2. Fallback to general handbook (rules_default.pdf) if no community-specific file exists
        if (string.IsNullOrWhiteSpace(loadedBase64))
        {
            loadedBase64 = await TryReadAssetAsBase64Async("rules_default.pdf");
            resolvedName = "rules_default.pdf";
        }

        if (!string.IsNullOrWhiteSpace(loadedBase64))
        {
            var entry = (loadedBase64, resolvedName);
            _pdfCacheByCode[code] = entry;
            return entry;
        }

        return (null, "NO_PDF_LOADED");
    }

    // Helper to safely read a MauiAsset PDF file from Resources/Raw as Base64
    private static async Task<string?> TryReadAssetAsBase64Async(string assetFileName)
    {
        try
        {
            var assembly = typeof(GeminiService).Assembly;
            string? resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(assetFileName, StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(resourceName))
                return null;

            using Stream? stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return Convert.ToBase64String(memoryStream.ToArray());
        }
        catch
        {
            return null;
        }
    }

    // Answers any resident question strictly grounded in the active community's Rules & FAQ PDF
    public async Task<BotMessageModel> AskJiranBotAsync(
        string userQuery,
        string communityCode,
        string communityName,
        string residentName,
        IEnumerable<BotMessageModel>? recentHistory = null)
    {
        if (string.IsNullOrWhiteSpace(ApiKey) || ApiKey == "YOUR_GEMINI_API_KEY")
            throw new InvalidOperationException("Gemini API key is not configured in GeminiService.cs.");

        string activeCommunity = string.IsNullOrWhiteSpace(communityName)
            ? "the residential community"
            : communityName;

        var (pdfBase64, loadedPdfName) = await GetCommunityRulesPdfBase64Async(communityCode);

        // Stop immediately and show diagnostic card if PDF could not be read from Resources/Raw
        if (string.IsNullOrWhiteSpace(pdfBase64))
        {
            string expectedCode = string.IsNullOrWhiteSpace(communityCode) ? "default" : communityCode.Trim().ToLower();
            return new BotMessageModel
            {
                IsUser = false,
                Title = "Handbook Not Loaded",
                MessageText = $"Could not load 'rules_{expectedCode}.pdf' or 'rules_default.pdf' from Resources/Raw. Check that the file is lowercase and Build Action is set to MauiAsset.",
                SourceCitation = $"Diagnostic · Code: {communityCode}",
                ActionButtonText = "Understood",
                RulePoints = new List<string>
                {
                    $"Expected asset: Resources/Raw/rules_{expectedCode}.pdf",
                    "Rebuild the project after adding PDF files to Resources/Raw"
                },
                Timestamp = DateTime.Now
            };
        }

        // Format the last 6 messages so JiranBot understands follow-up questions
        var historyBuilder = new StringBuilder();
        if (recentHistory != null)
        {
            foreach (var msg in recentHistory.TakeLast(6))
            {
                string role = msg.IsUser ? residentName : "JiranBot";
                historyBuilder.AppendLine($"{role}: {msg.MessageText}");
            }
        }

        string prompt =
    $"You are JiranBot, a friendly, warm, and casual neighbourhood assistant for '{activeCommunity}' (Code: {communityCode}) on the JiranKita app. " +
    $"You are chatting with your neighbour '{residentName}'.\n\n" +
    "TONE & GROUNDING RULES:\n" +
    "1. Speak like a helpful, approachable neighbour—keep it warm, natural, conversational, and easy to read in English (avoid stiff corporate or legal jargon).\n" +
    "2. Base all facts, fees, hours, and rules strictly on the attached PDF handbook. Never make up or guess details not in the PDF.\n" +
    "3. If the PDF doesn't mention what they're asking about, say so casually (e.g., \"Hmm, that's not mentioned in our handbook—best to double-check with the JMB office!\") and leave rulePoints empty [].\n" +
    "4. Answer ONLY what was asked. Keep bullet points short and scannable.\n" +
    "5. Use Recent Chat History to understand follow-up questions.\n" +
    "6. Return ONLY raw JSON (no markdown fences) matching this exact schema:\n" +
    "- \"title\": casual, friendly 2-4 word header (e.g., \"BBQ Pit Info\", \"Pool Hours & Rules\")\n" +
    "- \"messageText\": friendly, conversational direct answer (1-2 sentences)\n" +
    "- \"rulePoints\": short, easy-to-read bullet points ONLY if multiple rules/fees/steps apply; otherwise return []\n" +
    $"- \"sourceCitation\": \"{activeCommunity} · {loadedPdfName}\"\n" +
    "- \"actionButtonText\": casual 2-word button text (e.g., \"Got It!\", \"Ask JMB\", \"Book Now\")\n\n" +
    (historyBuilder.Length > 0 ? $"Recent Chat History:\n{historyBuilder}\n" : "") +
    $"Neighbour's Message: {userQuery}";

        var partsList = new List<object>
        {
            new
            {
                inlineData = new
                {
                    mimeType = "application/pdf",
                    data = pdfBase64
                }
            },
            new { text = prompt }
        };

        return await ExecuteStructuredGeminiRequestAsync(partsList, $"{activeCommunity} · {loadedPdfName}");
    }

    // Summarizes recent community group chat messages into key highlights and action items
    public async Task<BotMessageModel> SummarizeCommunityChatAsync(
        string chatTranscript,
        string communityName,
        string residentName)
    {
        if (string.IsNullOrWhiteSpace(ApiKey) || ApiKey == "YOUR_GEMINI_API_KEY")
            throw new InvalidOperationException("Gemini API key is not configured in GeminiService.cs.");

        string activeCommunity = string.IsNullOrWhiteSpace(communityName)
            ? "the residential community"
            : communityName;

        if (string.IsNullOrWhiteSpace(chatTranscript))
        {
            return new BotMessageModel
            {
                IsUser = false,
                Title = "Community Chat Summary",
                MessageText = $"There are no recent messages in the {activeCommunity} group chat to summarize right now.",
                SourceCitation = $"{activeCommunity} Group Chat",
                ActionButtonText = "Open Chat",
                RulePoints = new List<string>
                {
                    "Start a conversation with your neighbours in the Community Chat tab"
                },
                Timestamp = DateTime.Now
            };
        }

        string prompt =
    $"You are JiranBot, a friendly neighbourhood assistant for '{activeCommunity}' on the JiranKita app. " +
    $"Your neighbour '{residentName}' wants a quick, casual catch-up on what's been happening in the community group chat below.\n\n" +
    "Summarize the main topics, announcements, or neighbour requests in a warm, conversational, easy-to-read tone in English (like catching a neighbour up over coffee). " +
    "Return ONLY raw JSON (no markdown fences) matching this exact schema: " +
    "\"title\" (use \"Group Chat Catch-Up\"), " +
    "\"messageText\" (1-2 casual sentences summarizing the vibe/main updates), " +
    "\"rulePoints\" (array of 2 to 4 short, friendly bullet points covering key highlights or action items), " +
    $"\"sourceCitation\" (use \"{activeCommunity} · Group Chat\"), " +
    "\"actionButtonText\" (use \"Open Chat\").\n\n" +
    $"Recent Group Chat Transcript:\n{chatTranscript}";

        var partsList = new List<object>
        {
            new { text = prompt }
        };

        return await ExecuteStructuredGeminiRequestAsync(partsList, $"{activeCommunity} · Recent Group Chat");
    }

    // Executes the Gemini REST call with automatic retry on temporary 503/429 server overload
    private async Task<BotMessageModel> ExecuteStructuredGeminiRequestAsync(List<object> partsList, string fallbackCitation)
    {
        var requestBody = new
        {
            contents = new[]
            {
                new { parts = partsList }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.1
            }
        };

        HttpResponseMessage? response = null;
        const int maxRetries = 3;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            response = await _httpClient.PostAsJsonAsync($"{Endpoint}?key={ApiKey}", requestBody);

            if (response.IsSuccessStatusCode)
                break;

            int statusCode = (int)response.StatusCode;

            if ((statusCode == 503 || statusCode == 429) && attempt < maxRetries)
            {
                await Task.Delay(2000 * attempt);
                continue;
            }

            string errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API error ({response.StatusCode}): {errorBody}");
        }

        var apiJson = await response!.Content.ReadFromJsonAsync<JsonElement>();
        string rawJsonText = apiJson
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        using var parsedDoc = JsonDocument.Parse(rawJsonText);
        var root = parsedDoc.RootElement;

        var points = new List<string>();
        if (root.TryGetProperty("rulePoints", out var pointsArray) && pointsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in pointsArray.EnumerateArray())
            {
                string? pointText = item.GetString();
                if (!string.IsNullOrWhiteSpace(pointText))
                    points.Add(pointText);
            }
        }

        return new BotMessageModel
        {
            IsUser = false,
            Title = root.TryGetProperty("title", out var t) ? t.GetString() ?? "JiranBot AI Response" : "JiranBot AI Response",
            MessageText = root.TryGetProperty("messageText", out var m) ? m.GetString() ?? string.Empty : string.Empty,
            SourceCitation = root.TryGetProperty("sourceCitation", out var s) ? s.GetString() ?? fallbackCitation : fallbackCitation,
            ActionButtonText = root.TryGetProperty("actionButtonText", out var a) ? a.GetString() ?? "Understood" : "Understood",
            RulePoints = points,
            Timestamp = DateTime.Now
        };
    }
}