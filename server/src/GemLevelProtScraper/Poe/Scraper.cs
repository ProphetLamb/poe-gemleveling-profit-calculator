using System.Collections.Immutable;
using System.Text.Json;
using ScrapeAAS;

namespace GemLevelProtScraper.Poe;

internal record struct PoeLeagueListRepsonse(ImmutableArray<PoeLeagueListResponseItem> Result);
public sealed record PoeLeagueListResponseItem(string Id, string Realm, string Text);

internal sealed class PoeScraper(IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var rootPublisher = scope.ServiceProvider.GetRequiredService<IDataflowPublisher<UpdatePoeLeauges>>();
            await rootPublisher.PublishAsync(new("https://www.pathofexile.com/api"), stoppingToken).ConfigureAwait(false);

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken).ConfigureAwait(false);
            stoppingToken.ThrowIfCancellationRequested();
        }
    }
}

public sealed class PoeLeaguesSpider(IDataflowPublisher<PoeLeauge> publisher, IStaticPageLoader pageLoader) : IDataflowHandler<UpdatePoeLeauges>
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web);
    public async ValueTask HandleAsync(UpdatePoeLeauges message, CancellationToken cancellationToken = default)
    {
        Uri apiUrl = new(new Uri(message.ApiUrl), "/trade/data/leagues");
        var content = await pageLoader.LoadAsync(apiUrl, cancellationToken).ConfigureAwait(false);
        var response = await content.ReadFromJsonAsync<PoeLeagueListRepsonse>(_jsonSerializerOptions, cancellationToken).ConfigureAwait(false);

        var items = response.Result
            .Select(item => new PoeLeauge(item.Id, item.Text, ParseRealm(item.Realm), ParseLeagueModes(item.Id)));

        var tasks = items
            .Select(item => publisher.PublishAsync(item))
            .SelectTruthy(task => task.IsCompletedSuccessfully ? null : task.AsTask());

        await Task.WhenAll(tasks).ConfigureAwait(false);

        static LeaugeMode ParseLeagueModes(string text)
        {
            LeaugeMode mode = default;
            if (text.StartsWith("Hardcore ", StringComparison.InvariantCultureIgnoreCase))
            {
                mode |= LeaugeMode.Hardcore;
            }
            else if (text.StartsWith("Ruthless ", StringComparison.InvariantCultureIgnoreCase))
            {
                mode |= LeaugeMode.Ruthless;
            }
            else if (text.StartsWith("HC Ruthless ", StringComparison.InvariantCultureIgnoreCase))
            {
                mode |= LeaugeMode.HardcoreRuthless;
            }
            else
            {
                mode |= LeaugeMode.Softcore;
            }

            if (text.EndsWith(" Standard", StringComparison.InvariantCultureIgnoreCase) || text.Equals("Standard", StringComparison.OrdinalIgnoreCase))
            {
                mode |= LeaugeMode.Standard;
            }
            return mode;
        }

        static Realm ParseRealm(string text)
        {
            if (text.Equals("pc", StringComparison.OrdinalIgnoreCase))
            {
                return Realm.Pc;
            }
            if (text.Equals("xbox", StringComparison.OrdinalIgnoreCase))
            {
                return Realm.Xbox;
            }
            if (text.Equals("sony", StringComparison.OrdinalIgnoreCase))
            {
                return Realm.Sony;
            }
            throw new ArgumentException("Unknown realm text", nameof(text));
        }
    }
}

public sealed class PoeSink(PoeRepository repository) : IDataflowHandler<PoeLeauge>
{
    public async ValueTask HandleAsync(PoeLeauge message, CancellationToken cancellationToken = default)
    {
        _ = await repository.AddOrUpdateAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
