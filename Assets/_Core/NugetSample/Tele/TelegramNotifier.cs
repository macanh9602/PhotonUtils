using UnityEngine;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Threading.Tasks;
using System;
/// <summary>
/// A simple Telegram notifier for Unity that sends messages to a specified chat.
/// </summary>
public class TelegramNotifier : MonoBehaviour
{
    #region === INSPECTOR ===
    private ITelegramBotClient botClient;
    private string botToken = "8426047103:AAEOaQ7YmcAMPUvHdfkTDUSp8gqgtdF7fho";
    private long chatId = -5131013586; // Replace with your chat ID
    #endregion


    #region === RUNTIME DATA ===


    #endregion


    #region === UNITY LIFECYCLE ===
    async void Start()
    {
        botClient = new TelegramBotClient(botToken);
        await SendTelegramMessage("Telegram Notifier Initialized.");
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    #endregion


    #region === PUBLIC API ===

    public void Initialize()
    {

    }
    [ContextMenu("Ping Bot")]
    public void PingBot()
    {
        SendTelegramMessage("Ping from Unity!").ConfigureAwait(false);
    }

    #endregion


    #region === INTERNAL LOGIC ===

    public async Task SendTelegramMessage(string message)
    {
        try
        {
            Message msg = await botClient.SendMessage(
                chatId: chatId,
                text: message
            );
            Debug.Log($"[Telegram] Đã gửi tin nhắn lúc: {msg.Date}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Telegram] Lỗi rồi: {e.Message}");
        }
    }

    async void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error)
        {
            string alert = $"🚨 *BUG DETECTED* 🚨\n\n*Log:* {logString}\n\n*Device:* {SystemInfo.deviceModel}";
            await SendTelegramMessage(alert);
        }
    }

    #endregion


    #region === DEBUG ===

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {

    }
#endif

    #endregion
}
