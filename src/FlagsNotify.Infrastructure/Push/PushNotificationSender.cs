using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlagsNotify.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace FlagsNotify.Infrastructure.Push;

public sealed class PushNotificationSender : IPushNotificationSender
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ExpoPushOptions _options;

    public PushNotificationSender(
        HttpClient httpClient,
        IOptions<ExpoPushOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyCollection<PushTicket>> SendAsync(
        IReadOnlyCollection<PushMessage> messages,
        CancellationToken cancellationToken)
    {
        if (messages.Count == 0)
        {
            return Array.Empty<PushTicket>();
        }

        // Chunks creation
        var tasks = messages.Chunk(_options.MaxBatchSize).Select(batch => SendBatchAsync(batch, cancellationToken));

        // Parallel send
        var results = await Task.WhenAll(tasks);
        var tickets = results.SelectMany(batchTickets => batchTickets).ToArray();
        
        // todo -> Se precisar enviar milhares de notificações, o ideal é controlar a concorrência com SemaphoreSlim ou outro mecanismo de limitação.
        
        // var tickets = new List<PushTicket>(messages.Count);
        // foreach (PushMessage[] batch in messages.Chunk(_options.MaxBatchSize))
        // {
        //     IReadOnlyCollection<PushTicket> batchTickets =
        //         await SendBatchAsync(batch, cancellationToken);
        //     tickets.AddRange(batchTickets);
        // }

        return tickets;
    }

    // Http send method
    private async Task<IReadOnlyCollection<PushTicket>> SendBatchAsync(
        IReadOnlyCollection<PushMessage> messages,
        CancellationToken cancellationToken)
    {
        var payload = messages
            .Select(message => new ExpoPushMessage(
                message.To,
                message.Title,
                message.Body,
                "default",
                message.Data))
            .ToArray();

        for (int attempt = 0; ; attempt++)
        {
            try
            {
                using HttpRequestMessage request = CreateRequest(payload);
                using HttpResponseMessage response = await _httpClient.SendAsync(
                    request,
                    cancellationToken);

                if (ShouldRetry(response.StatusCode) && attempt < _options.RetryAttempts)
                {
                    await DelayAsync(attempt, cancellationToken);
                    continue;
                }

                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Expo Push API retornou {(int)response.StatusCode}: {responseBody}",
                        null,
                        response.StatusCode);
                }

                ExpoPushResponse? expoResponse = JsonSerializer.Deserialize<ExpoPushResponse>(
                    responseBody,
                    JsonOptions);

                if (expoResponse?.Errors is { Length: > 0 })
                {
                    string errors = string.Join(
                        "; ",
                        expoResponse.Errors.Select(error => $"{error.Code}: {error.Message}"));
                    throw new InvalidOperationException($"Expo Push API rejeitou o lote: {errors}");
                }

                return ParseTickets(expoResponse?.Data);
            }
            catch (HttpRequestException) when (attempt < _options.RetryAttempts)
            {
                await DelayAsync(attempt, cancellationToken);
            }
        }
    }

    private HttpRequestMessage CreateRequest(ExpoPushMessage[] payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.Endpoint)
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    _options.AccessToken);
        }

        return request;
    }

    private static IReadOnlyCollection<PushTicket> ParseTickets(JsonElement? data)
    {
        if (data is null)
        {
            return Array.Empty<PushTicket>();
        }

        IEnumerable<JsonElement> elements = data.Value.ValueKind switch
        {
            JsonValueKind.Array => data.Value.EnumerateArray(),
            JsonValueKind.Object => new[] { data.Value },
            _ => Enumerable.Empty<JsonElement>()
        };

        return elements.Select(element =>
        {
            string status = GetString(element, "status") ?? "error";
            string? id = GetString(element, "id");
            string? message = GetString(element, "message");
            string? errorCode = null;

            if (element.TryGetProperty("details", out JsonElement details))
            {
                errorCode = GetString(details, "error");
            }

            return new PushTicket(id, status, message, errorCode);
        }).ToArray();
    }

    private static string? GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement property)
            ? property.GetString()
            : null;

    private static bool ShouldRetry(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.TooManyRequests ||
        (int)statusCode >= 500;

    private static Task DelayAsync(int attempt, CancellationToken cancellationToken)
    {
        int delayInSeconds = Math.Min(1 << attempt, 8);
        return Task.Delay(TimeSpan.FromSeconds(delayInSeconds), cancellationToken);
    }

    private sealed record ExpoPushMessage(
        [property: JsonPropertyName("to")] string To,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("body")] string Body,
        [property: JsonPropertyName("sound")] string Sound,
        [property: JsonPropertyName("data")] IReadOnlyDictionary<string, object?> Data);

    private sealed record ExpoPushResponse(
        [property: JsonPropertyName("data")] JsonElement? Data,
        [property: JsonPropertyName("errors")] ExpoError[]? Errors);

    private sealed record ExpoError(
        [property: JsonPropertyName("code")] string Code,
        [property: JsonPropertyName("message")] string Message);
}
