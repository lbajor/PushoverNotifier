using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace PushoverNotifier;

public class PushoverNotifierFunction
{
    private readonly ILogger<PushoverNotifierFunction> logger;
    private const string pushoverApiUrl = "https://api.pushover.net/1/messages.json";
    private readonly string pushoverUserKey = Environment.GetEnvironmentVariable("PUSHOVER_USER_KEY");
    private readonly string pushoverAppToken = Environment.GetEnvironmentVariable("PUSHOVER_APP_TOKEN");

    public PushoverNotifierFunction(ILogger<PushoverNotifierFunction> logger)
    {
        this.logger = logger;
    }

    [Function(nameof(PushoverNotifierFunction))]
    public async Task Run([QueueTrigger("pushoverqueue")] QueueMessage message)
    {
        if (string.IsNullOrEmpty(pushoverUserKey) || string.IsNullOrEmpty(pushoverAppToken))
        {
            logger.LogError("Pushover user key or app token is not set in environment variables.");
            return;
        }

        var messageObject  = JsonSerializer.Deserialize<QueueNotification>(message.MessageText,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new JsonException("Nieprawidłowa wiadomość kolejki.");

        string title = string.IsNullOrWhiteSpace(messageObject.Title) ? "Notification" : messageObject.Title;
        string priority = (messageObject.Priority ?? 0).ToString();
        string messageText = messageObject.Message;

        using (HttpClient client = new HttpClient())
        {
            var values = new Dictionary<string, string>
            {
                { "token", pushoverAppToken },
                { "user", pushoverUserKey },
                { "message", messageText },
                { "title", title },
                { "priority", priority }
            };
            var content = new FormUrlEncodedContent(values);
            try
            {
                HttpResponseMessage response = await client.PostAsync(pushoverApiUrl, content);
                response.EnsureSuccessStatusCode();
                logger.LogInformation("Pushover notification sent successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error sending Pushover notification.");
            }
        }
    }

    private sealed class QueueNotification
    {
        public string Message { get; set; } = "";
        public string? Title { get; set; }
        public int? Priority { get; set; }
    }
}