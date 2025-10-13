using System.Net.Http.Json;
using System.Text.Json;

namespace MentalHealthBar.Api.Tests;

/// <summary>
/// Extension methods for HttpClient that use pre-configured JSON serializer options
/// </summary>
public static class HttpClientJsonExtensions
{
    public static Task<HttpResponseMessage> PostAsJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        T value,
        TestWebApplicationFactory<Program> factory,
        CancellationToken cancellationToken = default)
    {
        return client.PostAsJsonAsync(requestUri, value, factory.JsonSerializerOptions, cancellationToken);
    }

    public static Task<HttpResponseMessage> PutAsJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        T value,
        TestWebApplicationFactory<Program> factory,
        CancellationToken cancellationToken = default)
    {
        return client.PutAsJsonAsync(requestUri, value, factory.JsonSerializerOptions, cancellationToken);
    }

    public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        T value,
        TestWebApplicationFactory<Program> factory,
        CancellationToken cancellationToken = default)
    {
        return client.PatchAsJsonAsync(requestUri, value, factory.JsonSerializerOptions, cancellationToken);
    }

    public static Task<T?> GetFromJsonAsync<T>(
        this HttpClient client,
        string requestUri,
        TestWebApplicationFactory<Program> factory,
        CancellationToken cancellationToken = default)
    {
        return client.GetFromJsonAsync<T>(requestUri, factory.JsonSerializerOptions, cancellationToken);
    }
}

/// <summary>
/// Extension methods for HttpContent that use pre-configured JSON serializer options
/// </summary>
public static class HttpContentJsonExtensions
{
    public static Task<T?> ReadFromJsonAsync<T>(
        this HttpContent content,
        TestWebApplicationFactory<Program> factory,
        CancellationToken cancellationToken = default)
    {
        return content.ReadFromJsonAsync<T>(factory.JsonSerializerOptions, cancellationToken);
    }
}
