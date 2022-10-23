using System.Net.Http.Headers;
using System.Text.Json;

using Backend.Service.Sample.Config;
using Backend.Service.Sample.Dto;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;

using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

var MsIdentityHttpClientName = "msIdentity";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<AuthenticationConfig>(builder.Configuration.GetSection("Authentication"));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpClient(MsIdentityHttpClientName, client =>
{
    client.BaseAddress = new Uri("https://login.microsoftonline.com");
});
// builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization();

// add cors config
// allow everything
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors();  // use cors

app.UseAuthorization();

// app.MapControllers();

app.MapPost("/token", async (TokenRequestDto tokenRequest, IOptions<AuthenticationConfig> authenticationConfig, IDistributedCache cache, IHttpClientFactory httpClientFactory) =>
{
    // var httpClient = httpClientFactory.CreateClient(MsIdentityHttpClientName);

    // var contentDictionary = new Dictionary<string, string>()
    // {
    //     { "client_id", authenticationConfig.Value.ClientId! },
    //     { "client_secret", authenticationConfig.Value.ClientSecret! },
    //     { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
    //     { "requested_token_use", "on_behalf_of" },
    //     { "assertion", tokenRequest.SsoIdToken! },
    //     { "scope", authenticationConfig.Value.Scope! }
    // };

    // var httpMessage = new HttpRequestMessage(HttpMethod.Post, $"/{tokenRequest.TenantId}/oauth2/v2.0/token")
    // {
    //     Content = new FormUrlEncodedContent(contentDictionary)
    // };

    // var response = await httpClient.SendAsync(httpMessage);
    // var responseContent = await response.Content.ReadAsStringAsync();

    var cachedToken = await cache.GetStringAsync($"{tokenRequest.TenantId}|{tokenRequest.SsoIdToken}");
    AccessTokenDto? cachedAccessToken = cachedToken == null ? null : JsonSerializer.Deserialize<AccessTokenDto>(cachedToken);

    if(cachedAccessToken == null || cachedAccessToken.ExpiresOn < DateTimeOffset.Now) {
        var app = ConfidentialClientApplicationBuilder.Create(authenticationConfig.Value.ClientId!)
                    .WithClientSecret(authenticationConfig.Value.ClientSecret!)
                    .WithAuthority($"https://login.microsoftonline.com/{tokenRequest.TenantId}")
                    .Build();

        UserAssertion assert = new UserAssertion(tokenRequest.SsoIdToken);
        var scopes = authenticationConfig.Value.Scope?.Split(' ');
        var responseToken = await app.AcquireTokenOnBehalfOf(scopes, assert).ExecuteAsync();

        var accessToken = new AccessTokenDto()
        {
            IdToken = responseToken.IdToken,
            TenantId = responseToken.TenantId,
            ExpiresOn = responseToken.ExpiresOn,
            AccessToken = responseToken.AccessToken
        };
        var serializedTokenReponse = JsonSerializer.Serialize(accessToken);
        await cache.SetStringAsync($"{tokenRequest.TenantId}|{tokenRequest.SsoIdToken}", serializedTokenReponse);
        cachedAccessToken = accessToken;
    }

    var graphClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) => {
        requestMessage
            .Headers
            .Authorization = new AuthenticationHeaderValue("Bearer", cachedAccessToken.AccessToken);

        return Task.CompletedTask;
    }));

    // get top 5 mails
    var messages = await graphClient.Me.Messages.Request().Select("sender,subject").Top(5).GetAsync();

    return Results.Ok(messages);
});

app.Run();
