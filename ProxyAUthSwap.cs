using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

public class CustomTransformProvider : ITransformProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly string _introspectionEndpoint;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public CustomTransformProvider(IHttpClientFactory httpClientFactory, IMemoryCache cache, string introspectionEndpoint, string clientId, string clientSecret)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _introspectionEndpoint = introspectionEndpoint;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    public void Apply(TransformBuilderContext context)
    {
        // Add request transformation to swap the token
        context.AddRequestTransform(async transformContext =>
        {
            var originalToken = transformContext.HttpContext.Request.Headers["Authorization"].ToString();

            if (!string.IsNullOrEmpty(originalToken) && originalToken.StartsWith("Bearer "))
            {
                // Remove "Bearer " prefix
                originalToken = originalToken.Substring("Bearer ".Length);

                // Store the original token in HttpContext.Items
                transformContext.HttpContext.Items["OriginalBearerToken"] = originalToken;

                // Try to parse the token as a JWT
                var handler = new JwtSecurityTokenHandler();
                string userIdClaim = null;

                if (handler.CanReadToken(originalToken))
                {
                    var jwtToken = handler.ReadJwtToken(originalToken);
                    userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                }
                else
                {
                    // Token is not a JWT, so treat it as a reference token
                    var (introspectionResponse, claimsPrincipal) = await GetCachedIntrospectionResponseAsync(originalToken);
                    if (introspectionResponse != null && introspectionResponse.TryGetValue("sub", out var sub))
                    {
                        userIdClaim = sub.ToString();
                        transformContext.HttpContext.User = claimsPrincipal;
                    }
                }

                // Log or use the claim as needed (for example, logging the user ID)
                // LogUserId(userIdClaim);

                // Generate a new internal token (implement your own logic here)
                string newInternalToken = GenerateInternalToken();

                // Replace the original token with the new token
                transformContext.ProxyRequest.Headers["Authorization"] = $"Bearer {newInternalToken}";
            }
        });

        // Add response transformation to restore the original token
        context.AddResponseTransform(async transformContext =>
        {
            if (transformContext.HttpContext.Items.TryGetValue("OriginalBearerToken", out var originalTokenObj))
            {
                string originalToken = originalTokenObj as string;

                // Update the Authorization header in the response with the original token
                if (!string.IsNullOrEmpty(originalToken))
                {
                    transformContext.HttpContext.Response.Headers["Authorization"] = $"Bearer {originalToken}";
                }
            }

            await ValueTask.CompletedTask;
        });
    }

    private async Task<(JsonElement?, ClaimsPrincipal)> GetCachedIntrospectionResponseAsync(string token)
    {
        if (_cache.TryGetValue(token, out Lazy<Task<(JsonElement?, ClaimsPrincipal)>> cachedResponse))
        {
            return await cachedResponse.Value;
        }

        var lazyIntrospection = new Lazy<Task<(JsonElement?, ClaimsPrincipal)>>(() => IntrospectTokenAsync(token));

        var cacheEntry = _cache.Set(token, lazyIntrospection, new MemoryCacheEntryOptions()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // default expiration time
        });

        return await cacheEntry.Value;
    }

    private async Task<(JsonElement?, ClaimsPrincipal)> IntrospectTokenAsync(string token)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, _introspectionEndpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var credentials = $"{_clientId}:{_clientSecret}";
        var encodedCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(credentials));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", encodedCredentials);

        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("token", token)
        });

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);

        if (jsonDoc.RootElement.TryGetProperty("active", out var active) && active.GetBoolean())
        {
            var expiration = GetTokenExpiration(jsonDoc.RootElement);
            if (expiration.HasValue)
            {
                // Adjust cache duration based on token expiration
                var cacheDuration = expiration.Value - DateTime.UtcNow;
                _cache.Set(token, new Lazy<Task<(JsonElement?, ClaimsPrincipal)>>(() => Task.FromResult<(JsonElement?, ClaimsPrincipal)>(ParseClaims(jsonDoc.RootElement))), cacheDuration);
            }
            return ParseClaims(jsonDoc.RootElement);
        }

        return (null, null);
    }

    private DateTime? GetTokenExpiration(JsonElement introspectionResponse)
    {
        if (introspectionResponse.TryGetProperty("exp", out var exp))
        {
            var expValue = exp.GetInt64();
            var expiration = DateTimeOffset.FromUnixTimeSeconds(expValue).UtcDateTime;
            return expiration;
        }

        return null;
    }

    private (JsonElement, ClaimsPrincipal) ParseClaims(JsonElement introspectionResponse)
    {
        var claims = new List<Claim>();

        foreach (var property in introspectionResponse.EnumerateObject())
        {
            claims.Add(new Claim(property.Name, property.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "Introspection");
        var principal = new ClaimsPrincipal(identity);

        return (introspectionResponse, principal);
    }

    private string GenerateInternalToken()
    {
        // Implement your token generation logic here
        return "new-internal-token";
    }
}
