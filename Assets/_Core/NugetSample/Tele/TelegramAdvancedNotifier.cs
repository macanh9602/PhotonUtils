using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using UnityEngine;
using UnityEngine.Networking;

public class TelegramAdvancedNotifier : MonoBehaviour
{

    #region === INSPECTOR ===
    [SerializeField] private string botToken;
    [SerializeField] private long chatId;
    [SerializeField] private string apiKey;
    #endregion

    #region === RUNTIME DATA ===
    private ITelegramBotClient bot;

    Dictionary<string, ScriptCheckContext> scriptChecks = new();
    #endregion

    #region === UNITY LIFECYCLE ===
    void OnEnable()
    {
        Application.logMessageReceived += OnLog;
    }
    #endregion

    #region Unity Methods
    void Start()
    {
        bot = new TelegramBotClient(botToken);
        bot.StartReceiving(UpdateHandler, ErrorHandler);
    }

    void OnDisable()
    {
        Application.logMessageReceived -= OnLog;
    }
    #endregion

    #region === PUBLIC API ===
    public async Task SendErrorAlert(string errorMsg, string script, string line)
    {
        string errorId = Guid.NewGuid().ToString("N");

        ErrorCache.Add(errorId, new ErrorContext
        {
            ErrorMessage = errorMsg,
            Script = script,
            LineInfo = line,
            Time = DateTime.Now
        });

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🤖 AI Debug", $"ai|{errorId}") },
            new[] { InlineKeyboardButton.WithCallbackData("📸 Screenshot", $"shot|{errorId}") }
        });

        await bot.SendMessage(
            chatId,
            $"<b>🚨 New Error</b>\n<pre>{errorMsg}</pre>\n📍 {line}",
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            replyMarkup: keyboard
        );
    }
    #endregion

    #region === INTERNAL LOGIC ===
    void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Exception) return;

        _ = SendErrorAlert(
            condition,
            stackTrace,
            "Runtime Exception"
        );
    }



    async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken ct)
    {
        if (update.CallbackQuery == null) return;

        var parts = update.CallbackQuery.Data.Split('|');
        var cmd = parts[0];
        var id = parts[1];

        if (!ErrorCache.TryGet(id, out var ctx))
        {
            await bot.AnswerCallbackQuery(update.CallbackQuery.Id, "❌ Error context expired");
            return;
        }

        if (cmd == "ai")
        {
            await bot.AnswerCallbackQuery(update.CallbackQuery.Id, "🤖 Asking AI...");
            var aiResult = await AskAI(ctx.ErrorMessage, ctx.Script);
            await bot.SendMessage(
                chatId,
                FormatAIResultForTelegram(aiResult),
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );
        }

        if (cmd == "shot")
        {
            await bot.AnswerCallbackQuery(update.CallbackQuery.Id, "📸 Capturing...");
            MainThreadDispatcher.Enqueue(() => StartCoroutine(CaptureAndSend()));
        }
    }

    string Escape(string s)
    {
        return s
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    string FormatAIResultForTelegram(string aiResult)
    {
        return
    $@"<b>🤖 AI Debug Result</b>

{Escape(aiResult)}";
    }

    IEnumerator CaptureAndSend()
    {
        yield return new WaitForEndOfFrame();
        Debug.Log("[TelegramNotifier] Capturing screenshot...");
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        var bytes = tex.EncodeToJPG();
        Destroy(tex);

        using var ms = new MemoryStream(bytes);

        var task = bot.SendPhoto(
            chatId: chatId,
            photo: InputFile.FromStream(ms, "screen.jpg"),
            caption: "🖼 Screenshot from Unity"
        );

        // chờ upload xong TRƯỚC khi dispose stream
        while (!task.IsCompleted)
            yield return null;

        if (task.Exception != null)
            Debug.LogException(task.Exception);
    }

    Task ErrorHandler(ITelegramBotClient c, Exception ex, CancellationToken ct)
    {
        Debug.LogException(ex);
        return Task.CompletedTask;
    }

    async Task<string> AskAI(string error, string stack)
    {
        string prompt = BuildPrompt(error, stack);

        string url =
        $"https://generativelanguage.googleapis.com/v1/models/gemini-2.5-flash:generateContent?key={apiKey}";

        var payload = new
        {
            contents = new[]
            {
            new
            {
                role = "user",
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        }
        };

        string json = JsonConvert.SerializeObject(payload);

        using var client = new HttpClient();
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        var result = await response.Content.ReadAsStringAsync();

        Debug.Log("[GEMINI RAW]\n" + result);

        return ExtractAIContent(result);
    }

    string ExtractAIContent(string json)
    {
        var root = JObject.Parse(json);

        if (root["error"] != null)
            return "AI ERROR: " + root["error"]?["message"];

        return root["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString()
               ?? "No Gemini response.";
    }

    string BuildPrompt(string error, string stack)
    {
        return $@"
You are a Unity debugging assistant.

Analyze the error below and respond in this EXACT format:

TLDR:
- One short sentence describing the root cause

FIX:
- Bullet list of concrete steps to fix

DETAILS:
- Short explanation (max 5 lines)

Do NOT use markdown headings.
Do NOT use code blocks unless necessary.
Keep it concise and readable in Telegram.
Language: Vietnamese
ERROR:
{error}

STACKTRACE:
{stack}

";
    }
    #endregion

    #region === DEBUG ===
    [ContextMenu("TEST ERROR")]
    async void Test()
    {
        await SendErrorAlert(
            "NullReferenceException",
            "player.Move();",
            "PlayerController.cs:42"
        );
    }
    #endregion

}
