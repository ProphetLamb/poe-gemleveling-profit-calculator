using GemLevelProtScraper;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson.Serialization;
using MongoDB.Migration;
using ScrapeAAS;
using Yoh.Text.Json.NamingPolicies;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

BsonSerializer.RegisterSerializationProvider(new ImmutableArraySerializationProvider());

var apiKey = builder.Configuration["Authentication:ApiKey"];
var webShareKey = builder.Configuration["Authentication:WebShareApiKey"];

builder.Services
    .Configure<PoeNinjaDatabaseSettings>(builder.Configuration.GetSection("Database:PoeNinjaDatabaseSettings"))
    .Configure<PoeDbDatabaseSettings>(builder.Configuration.GetSection("Database:PoeDbDatabaseSettings"))
    .AddMigrations()
    .AddTransient<PoeDbRepository>()
    .AddTransient<PoeNinjaRepository>()
    .AddTransient<ProfitService>()
    .AddHostedService<PoeNinjaScraper>()
    .AddHostedService<PoeDbScraper>()
    .AddScrapeAAS(config => config
        .UseDefaultConfiguration()
        .AddDataFlow<PoeNinjaSpider>()
        .AddDataFlow<PoeNinjaSink>()
        .AddDataFlow<PoeDbSkillNameSpider>()
        .AddDataFlow<PoeDbSkillSpider>()
        .AddDataFlow<PoeDbSink>()
        .Use(ScrapeAASRole.ProxyProvider, s => s.AddWebShareProxyProvider(o => o.ApiKey = webShareKey))
    )
    .AddHttpContextAccessor()
    .AddMemoryCache()
    .AddOutputCache(o =>
    {
        o.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(10)));
        o.AddPolicy("expire30min", b => b.Cache().Expire(TimeSpan.FromMinutes(30)));
    });

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicies.SnakeCaseLower;
        o.JsonSerializerOptions.AllowTrailingCommas = true;
    });

builder.Services
    .AddAuthentication()
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("token", o => o.ApiKey = apiKey);

var app = builder.Build();

app
    .MapGet("gem-profit", async (
        [FromServices] ProfitService profitService,
        [AsParameters] ProfitRequest request,
        CancellationToken cancellationToken = default
    ) =>
    {
        var data = await profitService.GetProfitAsync(request, cancellationToken).ConfigureAwait(false);
        return data;
    }).CacheOutput("expire30min");
app.Run();
