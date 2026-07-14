using System.Net;
using Apps.Optimizely.Constants;
using Apps.Optimizely.Models.Dtos;
using Apps.Optimizely.Utils;
using Blackbird.Applications.Sdk.Common.Authentication;
using Blackbird.Applications.Sdk.Common.Exceptions;
using Blackbird.Applications.Sdk.Utils.Extensions.Sdk;
using Blackbird.Applications.Sdk.Utils.RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace Apps.Optimizely.Api;

public class Client : BlackBirdRestClient
{
    private readonly AuthenticationCredentialsProvider[] _creds;
    private string? _accessToken;

    protected override JsonSerializerSettings? JsonSettings => new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    public Client(IEnumerable<AuthenticationCredentialsProvider> creds) : base(new RestClientOptions
    {
        BaseUrl = new Uri(UrlHelper.Normalize(creds.Get(CredsNames.BaseUrl).Value)),
        MaxTimeout = 180000,
        RemoteCertificateValidationCallback = (_, _, _, _) => true
    })
    {
        _creds = creds.ToArray();
    }

    public RestRequest CreateTokenRequest()
    {
        var request = BuildAuthorizationRequest();
        return request;
    }

    public virtual async Task<List<OptimizelyContentSummaryDto>> GetChildrenAsync(string rootContentId, CancellationToken cancellationToken = default)
    {
        var request = await CreateAuthenticatedRequestAsync(string.Format(ApiConstants.ContentChildrenResource, rootContentId), Method.Get, cancellationToken);
        return await ExecuteWithErrorHandling<List<OptimizelyContentSummaryDto>>(request);
    }

    public async Task<JObject> GetContentAsync(string contentId, string? locale = null, CancellationToken cancellationToken = default)
    {
        var request = await CreateAuthenticatedRequestAsync(string.Format(ApiConstants.ContentManagementResource, contentId), Method.Get, cancellationToken);
        AddLanguageHeaders(request, locale);

        return await ExecuteWithErrorHandling<JObject>(request);
    }

    public async Task PatchContentAsync(string contentId, string locale, JObject patchBody, CancellationToken cancellationToken = default)
    {
        var request = await CreateAuthenticatedRequestAsync(string.Format(ApiConstants.ContentManagementResource, contentId), Method.Patch, cancellationToken);
        AddLanguageHeaders(request, locale);
        request.AddStringBody(patchBody.ToString(Formatting.None), ContentType.Json);

        await ExecuteWithErrorHandling(request);
    }

    public async Task PutContentAsync(string contentGuid, JObject contentBody, CancellationToken cancellationToken = default)
    {
        var request = await CreateAuthenticatedRequestAsync(string.Format(ApiConstants.ContentManagementResource, contentGuid), Method.Put, cancellationToken);
        request.AddStringBody(contentBody.ToString(Formatting.None), ContentType.Json);

        await ExecuteWithErrorHandling(request);
    }

    public async Task<List<OptimizelyLanguageDto>> GetLanguagesAsync(CancellationToken cancellationToken = default)
    {
        var request = await CreateAuthenticatedRequestAsync(ApiConstants.SiteResource, Method.Get, cancellationToken);
        var sites = await ExecuteWithErrorHandling<List<OptimizelySiteDto>>(request);

        return sites.SelectMany(site => site.Languages).ToList();
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_accessToken))
        {
            return _accessToken;
        }

        var tokenResponse = await ExecuteWithErrorHandling<TokenResponse>(CreateTokenRequest());
        _accessToken = tokenResponse.AccessToken;

        return _accessToken;
    }

    public override async Task<RestResponse> ExecuteWithErrorHandling(RestRequest request)
    {
        var response = await ExecuteAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw ConfigureErrorException(response);
        }

        return response;
    }

    public override async Task<T> ExecuteWithErrorHandling<T>(RestRequest request)
    {
        var response = await ExecuteWithErrorHandling(request);
        var value = JsonConvert.DeserializeObject<T>(response.Content ?? string.Empty, JsonSettings);
        if (value is null)
        {
            throw new PluginApplicationException($"Could not parse server response to {typeof(T).Name}: {response.Content}");
        }

        return value;
    }

    protected override Exception ConfigureErrorException(RestResponse response)
    {
        var message = ExtractErrorMessage(response);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new PluginApplicationException($"Unauthorized: {message}");
        }

        return new PluginApplicationException(message);
    }

    public static string ExtractErrorMessage(RestResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.Content))
        {
            return response.ErrorMessage ?? response.StatusDescription ?? response.StatusCode.ToString();
        }

        try
        {
            var token = JToken.Parse(response.Content);
            return token["message"]?.ToString()
                   ?? token["error_description"]?.ToString()
                   ?? token["error"]?.ToString()
                   ?? token["title"]?.ToString()
                   ?? response.Content;
        }
        catch (JsonReaderException)
        {
            return response.Content;
        }
    }

    private async Task<RestRequest> CreateAuthenticatedRequestAsync(string resource, Method method, CancellationToken cancellationToken)
    {
        var request = new RestRequest(resource, method);
        request.AddHeader("Authorization", $"Bearer {await GetAccessTokenAsync(cancellationToken)}");
        request.AddHeader("Accept", "application/json");
        return request;
    }

    private static void AddLanguageHeaders(RestRequest request, string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return;
        }

        request.AddHeader("X-EPiServer-Language", locale);
        request.AddHeader("Accept-Language", locale);
    }
    
    private RestRequest BuildAuthorizationRequest()
    {
        var request = new RestRequest(ApiConstants.TokenResource, Method.Post);
        request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
        request.AddParameter("client_id", _creds.Get(CredsNames.ClientId).Value);
        request.AddParameter("client_secret", _creds.Get(CredsNames.ClientSecret).Value);
        
        var connectionType = _creds.Get(CredsNames.ConnectionType).Value;
        if (connectionType == ConnectionTypes.UserCredentials)
        {
            var username = _creds.Get(CredsNames.Username)?.Value;
            var password = _creds.Get(CredsNames.Password)?.Value;
            
            if(!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                request.AddParameter("username", username);
                request.AddParameter("password", password);
                request.AddParameter("grant_type", "password");
                request.AddParameter("scope", ApiConstants.Scope);
            }
        }
        else
        {
            request.AddParameter("grant_type", "client_credentials");
            request.AddParameter("scope", ApiConstants.ScopeForClientCredentials);
        }
        
        return request;
    }
}
