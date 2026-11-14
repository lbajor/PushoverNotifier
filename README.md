# Pushover Notifier

Funkcja Azure Functions odbierająca wiadomości z Azure Queue Storage i wysyłająca je jako powiadomienia Pushover. Projekt korzysta z .NET 10 i modelu isolated worker.

## Wymagania

- .NET 10 SDK i Azure Functions Core Tools v4.
- Konto Pushover, klucz użytkownika oraz token aplikacji.
- Azure Queue Storage lub uruchomiony lokalnie Azurite z usługą Queue.

## Konfiguracja

Uzupełnij plik `local.settings.json` na potrzeby pracy lokalnej:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "PUSHOVER_USER_KEY": "<klucz-uzytkownika>",
    "PUSHOVER_APP_TOKEN": "<token-aplikacji>"
  }
}
```

`UseDevelopmentStorage=true` wymaga uruchomionego Azurite. Aby korzystać z konta Azure Storage, zamiast tego ustaw jego connection string. W środowisku Azure ustaw odpowiednie wartości w konfiguracji aplikacji Function App. Nie zapisuj prawdziwych kluczy ani connection stringów w repozytorium.

Opcjonalnie ustaw `APPLICATIONINSIGHTS_CONNECTION_STRING`, aby włączyć eksport telemetrii OpenTelemetry do Azure Monitor.

## Uruchomienie lokalne

1. Uruchom Azurite albo skonfiguruj dostęp do Azure Storage.
2. W katalogu projektu uruchom:

   ```powershell
   func start
   ```

3. Dodaj wiadomość do kolejki `pushoverqueue` w skonfigurowanym koncie Storage. Funkcja przetworzy ją i wyśle powiadomienie do Pushover.

## Format wiadomości

Wiadomość kolejki musi zawierać obiekt JSON, na przykład:

```json
{
  "message": "Serwer jest niedostępny",
  "title": "Alert",
  "priority": 1
}
```

| Pole | Wymagane | Opis |
| --- | --- | --- |
| `message` | Tak | Treść powiadomienia; nie może być pusta ani składać się wyłącznie z białych znaków. |
| `title` | Nie | Tytuł; brak, `null` lub pusty tekst oznacza `Notification`. |
| `priority` | Nie | Liczba całkowita: `-2`, `-1`, `0` lub `1`. Domyślnie `0`. |

Nazwy pól nie rozróżniają wielkości liter. Priorytet `2` (emergency) nie jest obsługiwany przez tę funkcję.

Trigger domyślnie oczekuje wiadomości zakodowanych w Base64. Klient wysyłający musi używać zgodnego kodowania; nie koduj wiadomości dwukrotnie, jeśli narzędzie lub SDK wykonuje to automatycznie. Można też skonfigurować `extensions.queues.messageEncoding` jako `none` w `host.json` i wysyłać zwykły JSON.

## Błędy i ponowienia

Nieprawidłowy JSON, nieprawidłowe dane, brak konfiguracji oraz błędy wysyłki do Pushover powodują niepowodzenie wywołania. Azure Functions ponawia przetwarzanie; domyślnie po pięciu nieudanych próbach wiadomość trafia do kolejki `pushoverqueue-poison`. Monitoruj tę kolejkę i logi funkcji. Ponowienia mogą prowadzić do duplikatów, jeśli Pushover przyjął powiadomienie, ale funkcja nie otrzymała potwierdzenia.

## Dokumentacja

- [Azure Queue Storage trigger](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger)
- [Konfiguracja rozszerzenia Queue Storage](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue#hostjson-settings)
- [Pushover API](https://pushover.net/api)
