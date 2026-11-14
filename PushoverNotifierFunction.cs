using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace PushoverNotifier;

public class PushoverNotifierFunction
{
    private readonly ILogger<PushoverNotifierFunction> logger;
    private readonly HttpClient httpClient;
    private const string pushoverApiUrl = "https://api.pushover.net/1/messages.json";
    private readonly string pushoverUserKey = Environment.GetEnvironmentVariable("PUSHOVER_USER_KEY");
    private readonly string pushoverAppToken = Environment.GetEnvironmentVariable("PUSHOVER_APP_TOKEN");

    public PushoverNotifierFunction(ILogger<PushoverNotifierFunction> logger, HttpClient httpClient)
    {
        this.logger = logger;
        this.httpClient = httpClient;
    }

    [Function(nameof(PushoverNotifierFunction))]
    public async Task Run([QueueTrigger("pushoverqueue")] QueueMessage message)
    {
        if (string.IsNullOrEmpty(pushoverUserKey) || string.IsNullOrEmpty(pushoverAppToken))
        {
            string errorMessage = "Pushover user key or app token is not set in environment variables.";
            logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        var messageObject  = JsonSerializer.Deserialize<QueueNotification>(message.MessageText,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new JsonException("Nieprawidłowa wiadomość kolejki.");

        string title = string.IsNullOrWhiteSpace(messageObject.Title) ? "Notification" : messageObject.Title;
        string priority = (messageObject.Priority ?? 0).ToString();
        string messageText = messageObject.Message;

        if(priority != "-2" && priority != "-1" && priority != "0" && priority != "1")
        {
            string errorMessage = $"Invalid priority value: {priority}. Must be -2, -1, 0, or 1.";
            logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
        if (string.IsNullOrWhiteSpace(messageText))
        {
            string errorMessage = "Message text is empty. Cannot send notification.";
            logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

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
            var response = await httpClient.PostAsync(pushoverApiUrl, content);
            response.EnsureSuccessStatusCode();
            logger.LogInformation("Pushover notification sent successfully.");
        }
        catch (Exception ex)
        {
            string errorMessage = $"Failed to send Pushover notification. Message: {ex.Message}";
            logger.LogError(ex, errorMessage);
            throw;
        }
    }

    public sealed class QueueNotification
    {
        public string Message { get; set; } = "";
        public string? Title { get; set; }
        public int? Priority { get; set; }
    }
}