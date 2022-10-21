using System.Text;
using Backend.Service.Sample.Config;
using Backend.Service.Sample.Dto;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

// app.MapControllers();

app.MapPost("/token", async (TokenRequestDto tokenRequest, IOptions<AuthenticationConfig> authenticationConfig, IDistributedCache cache, IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient(MsIdentityHttpClientName);

    var contentDictionary = new Dictionary<string, string>()
    {
        { "client_id", authenticationConfig.Value.ClientId! },
        { "client_secret", authenticationConfig.Value.ClientSecret! },
        { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
        { "requested_token_use", "on_behalf_of" },
        { "assertion", tokenRequest.SsoIdToken! },
        { "scope", authenticationConfig.Value.Scope! }
    };

    var httpMessage = new HttpRequestMessage(HttpMethod.Post, $"/{tokenRequest.TenantId}/oauth2/v2.0/token")
    {
        Content = new FormUrlEncodedContent(contentDictionary)
    };

    var response = await httpClient.SendAsync(httpMessage);

    var cacheString = await cache.GetStringAsync("blub");
    if(string.IsNullOrEmpty(cacheString))
    {
        // add cache string
        await cache.SetStringAsync("blub", "I'm blue dabedidabedei");
        return Results.NotFound();
    }

    return Results.Ok(cacheString);
});

app.Run();
