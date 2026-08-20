using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace SulthanERP.Cashier.Services;

public sealed class ApiService
{
    private readonly RestClient _client;
    private string? _accessToken;

    public ApiService()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var baseUrl = configuration["ApiSettings:BaseUrl"]
            ?? throw new InvalidOperationException("API Base URL not configured.");

        _client = new RestClient(baseUrl);
    }

    public async Task<RestResponse> GetAsync(string endpoint)
    {
        var request = new RestRequest(endpoint, Method.Get);
        AddAuthorization(request);
        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> PostAsync(string endpoint, object data)
    {
        var request = new RestRequest(endpoint, Method.Post);
        request.AddJsonBody(data);
        AddAuthorization(request);
        return await _client.ExecuteAsync(request);
    }

    public async Task<RestResponse> PutAsync(string endpoint, object data)
    {
        var request = new RestRequest(endpoint, Method.Put);
        request.AddJsonBody(data);
        AddAuthorization(request);
        return await _client.ExecuteAsync(request);
    }

    public void SetAccessToken(string token)
    {
        _accessToken = token;
    }

    public static string? ReadString(string? json, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        return Find(JToken.Parse(json), names)?.Value<string>();
    }

    public static int? ReadInt(string? json, params string[] names)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var token = Find(JToken.Parse(json), names);

        return token != null &&
               int.TryParse(token.ToString(), out var value)
            ? value
            : null;
    }

    public static int? ReadUserIdFromJwt(string token)
    {
        try
        {
            var part = token.Split('.')[1]
                .Replace('-', '+')
                .Replace('_', '/');

            part = part.PadRight(
                part.Length + (4 - part.Length % 4) % 4,
                '=');

            var payload = JObject.Parse(
                System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(part)));

            foreach (var name in new[]
            {
                "nameid",
                "sub",
                "id",
                "userId",
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
            })
            {
                if (int.TryParse(payload[name]?.ToString(), out var value))
                    return value;
            }
        }
        catch
        {
        }

        return null;
    }

    private void AddAuthorization(RestRequest request)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken))
        {
            request.AddOrUpdateHeader(
                "Authorization",
                $"Bearer {_accessToken}");
        }
    }

    private static JToken? Find(JToken? token, string[] names)
    {
        if (token is JObject obj)
        {
            foreach (var property in obj.Properties())
            {
                if (names.Any(name =>
                    string.Equals(
                        name,
                        property.Name,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    return property.Value;
                }
            }

            foreach (var property in obj.Properties())
            {
                var found = Find(property.Value, names);

                if (found != null)
                    return found;
            }
        }

        if (token is JArray array)
        {
            foreach (var item in array)
            {
                var found = Find(item, names);

                if (found != null)
                    return found;
            }
        }

        return null;
    }
}