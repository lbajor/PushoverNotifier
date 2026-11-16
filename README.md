# Pushover Notifier

An Azure Functions app that receives messages from Azure Queue Storage and sends them as Pushover notifications. The project uses .NET 10 and the isolated worker model.

## Requirements

- .NET 10 SDK and Azure Functions Core Tools v4.
- A Pushover account, user key, and application token.
- Azure Queue Storage or a locally running Azurite instance with the Queue service.

## Configuration

For local development, configure `local.settings.json`:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "PUSHOVER_USER_KEY": "<user-key>",
    "PUSHOVER_APP_TOKEN": "<application-token>"
  }
}
```

`UseDevelopmentStorage=true` requires Azurite to be running. To use an Azure Storage account, set its connection string instead. In Azure, configure the corresponding values in the Function App settings. Do not store real keys or connection strings in the repository.

Optionally, set `APPLICATIONINSIGHTS_CONNECTION_STRING` to enable OpenTelemetry export to Azure Monitor.

## Run locally

1. Start Azurite or configure access to Azure Storage.
2. In the project directory, run:

   ```powershell
   func start
   ```

3. Add a message to the `pushoverqueue` queue in the configured Storage account. The function will process it and send a Pushover notification.

## Message format

A queue message must contain a JSON object, for example:

```json
{
  "message": "The server is unavailable",
  "title": "Alert",
  "priority": 1
}
```

| Field | Required | Description |
| --- | --- | --- |
| `message` | Yes | Notification body; it must not be empty or contain only whitespace. |
| `title` | No | Title; if omitted, `null`, or empty, it defaults to `Notification`. |
| `priority` | No | Integer: `-2`, `-1`, `0`, or `1`. Defaults to `0`. |

Field names are case-insensitive. Priority `2` (emergency) is not supported by this function.

The trigger expects Base64-encoded messages by default. The sender must use compatible encoding; do not encode messages twice if the tool or SDK does it automatically. Alternatively, set `extensions.queues.messageEncoding` to `none` in `host.json` and send plain JSON.

## Errors and retries

Invalid JSON, invalid data, missing configuration, and Pushover delivery errors cause the invocation to fail. Azure Functions retries processing; by default, after five failed attempts, the message is moved to the `pushoverqueue-poison` queue. Monitor that queue and the function logs. Retries may result in duplicate notifications if Pushover accepted a notification but the function did not receive confirmation.

## Documentation

- [Azure Queue Storage trigger](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger)
- [Queue Storage extension configuration](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue#hostjson-settings)
- [Pushover API](https://pushover.net/api)
