using GemLevelProtScraper.Poe;
using GemLevelProtScraper.PoeDb;
using GemLevelProtScraper.PoeNinja;
using GemLevelProtScraper.Skills;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Migration;
using MongoDB.Migration.Core;

namespace GemLevelProtScraper;

public sealed record PoeDatabaseSettings : IOptions<PoeDatabaseSettings>, IMongoMigratable
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }
    public required string PoeLeagueCollectionName { get; init; }
    public required string PoeDbSkillCollectionName { get; init; }
    public required string PoeNinjaGemCollectionName { get; init; }
    public required string PoeNinjaCurrencyCollectionName { get; init; }
    public required string SkillGemViewName { get; init; }

    public const string Alias = "Poe";

    PoeDatabaseSettings IOptions<PoeDatabaseSettings>.Value => this;

    public MongoMigrableDefinition GetMigratableDefinition()
    {
        return new()
        {
            ConnectionString = ConnectionString,
            Database = new(Alias, DatabaseName),
            MirgrationStateCollectionName = "DATABASE_MIGRATIONS"
        };
    }

    internal IMongoDatabase GetDatabase()
    {
        MongoClient client = new(ConnectionString);
        return client.GetDatabase(DatabaseName);
    }

    internal IMongoCollection<PoeLeague> GetPoeLeagueCollection(IMongoDatabase? database = null)
    {
        database ??= GetDatabase();
        return database.GetCollection<PoeLeague>(PoeLeagueCollectionName);
    }
    internal IMongoCollection<PoeDbSkillEnvalope> GetPoeDbSkillCollection(IMongoDatabase? database = null)
    {
        database ??= GetDatabase();
        return database.GetCollection<PoeDbSkillEnvalope>(PoeDbSkillCollectionName);
    }

    internal IMongoCollection<PoeNinjaApiGemPriceEnvalope> GetPoeNinjaGemCollection(IMongoDatabase? database = null)
    {
        database ??= GetDatabase();
        return database.GetCollection<PoeNinjaApiGemPriceEnvalope>(PoeNinjaGemCollectionName);
    }

    internal IMongoCollection<PoeNinjaApiCurrencyPriceEnvalope> GetPoeNinjaCurrencyCollection(IMongoDatabase? database = null)
    {
        database ??= GetDatabase();
        return database.GetCollection<PoeNinjaApiCurrencyPriceEnvalope>(PoeNinjaCurrencyCollectionName);
    }

    internal IMongoCollection<SkillGem> GetSkillGemCollection(IMongoDatabase? database = null)
    {
        database ??= GetDatabase();
        return database.GetCollection<SkillGem>(SkillGemViewName);
    }
}

[MongoMigration(PoeDatabaseSettings.Alias, 8, 9, Description = $"Add unique composite index {CurrencyIdentifierIndexName}.")]
public sealed class PoeNinjaCurrencyAddIdentifierIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string CurrencyIdentifierIndexName = "CurrencyIdentifier";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var currencyCollection = optionsAccessor.Value.GetPoeNinjaCurrencyCollection(database);
        await currencyCollection.Indexes.DropOneAsync(CurrencyIdentifierIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeNinjaCurrencyCollection(database);
        IndexKeysDefinitionBuilder<PoeNinjaApiCurrencyPriceEnvalope> builder = new();
        var index = builder.Combine(
            builder.Ascending(e => e.Price.CurrencyTypeName),
            builder.Ascending(e => e.League)
        );

        CreateIndexModel<PoeNinjaApiCurrencyPriceEnvalope> model = new(index, new()
        {
            Unique = true,
            Name = CurrencyIdentifierIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}


[MongoMigration(PoeDatabaseSettings.Alias, 7, 8, Description = $"Add text index {CurrencyLeagueIndexName}.")]
public sealed class PoeNinjaCurrencyAddLeagueIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string CurrencyLeagueIndexName = "CurrencyLeague";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeNinjaGemCollection(database);
        await col.Indexes.DropOneAsync(CurrencyLeagueIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeNinjaCurrencyCollection(database);
        IndexKeysDefinitionBuilder<PoeNinjaApiCurrencyPriceEnvalope> builder = new();
        var index = builder.Ascending(e => e.League);

        CreateIndexModel<PoeNinjaApiCurrencyPriceEnvalope> model = new(index, new()
        {
            Name = CurrencyLeagueIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}

[MongoMigration(PoeDatabaseSettings.Alias, 6, 7, Description = $"Add text index {CurrencyTypeNameIndexName}.")]
public sealed class PoeNinjaCurrencyAddNameIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string CurrencyTypeNameIndexName = "CurrencyTypeName";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeNinjaCurrencyCollection(database);
        await col.Indexes.DropOneAsync(CurrencyTypeNameIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeNinjaCurrencyCollection(database);
        IndexKeysDefinitionBuilder<PoeNinjaApiCurrencyPriceEnvalope> builder = new();
        var index = builder.Hashed(e => e.Price.CurrencyTypeName);

        CreateIndexModel<PoeNinjaApiCurrencyPriceEnvalope> model = new(index, new()
        {
            Name = CurrencyTypeNameIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}


[MongoMigration(PoeDatabaseSettings.Alias, 5, 6, Description = $"Create view SkillGemVeiw.")]
public sealed class CreateSkillViewMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        await database.DropCollectionAsync(optionsAccessor.Value.SkillGemViewName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var createViewMql = /*lang=mql*/ $$"""
        {
            'create': '{{optionsAccessor.Value.SkillGemViewName}}',
            'viewOn': '{{optionsAccessor.Value.PoeDbSkillCollectionName}}',
            'pipeline': [
                {
                    '$project': {
                        'Skill': 1,
                        'LevelEffects': {
                            '$filter': {
                                'input': '$Skill.LevelEffects',
                                'cond': { '$ne': [ '$$this.Experience', null] }
                            }
                        },
                    }
                },
                {
                    '$project': {
                        'Name': '$Skill.Name._id',
                        'RelativeUrl': '$Skill.Name.RelativeUrl',
                        'Color': '$Skill.Name.Color',
                        'League': '$Price.League',
                        'IconUrl': '$Skill.IconUrl',
                        'BaseType': '$Skill.Stats.BaseType',
                        'Discriminator': '$Skill.Discriminator',
                        'SumExperience': {
                            '$reduce': {
                                'input': '$LevelEffects.Experience',
                                'initialValue': 0.0,
                                'in': {
                                '$add': [
                                    { '$toDouble': '$$value' },
                                    { '$toDouble': '$$this' },
                                ]
                                }
                            }
                        },
                        'MaxLevel': {
                            '$reduce': {
                                'input': '$LevelEffects.Level',
                                'initialValue': 0,
                                'in': {
                                    '$add': [
                                        {
                                            '$max': [
                                                { '$toDouble': '$$value' },
                                                { '$toDouble': '$$this' },
                                            ]
                                        },
                                        1
                                    ]
                                }
                            }
                        }
                    }
                },
            ]
        }
        """;
        var command = new JsonCommand<BsonDocument>(createViewMql);
        _ = await database.RunCommandAsync(command, null, cancellationToken).ConfigureAwait(false);
    }
}

[MongoMigration(PoeDatabaseSettings.Alias, 4, 5, Description = $"Add text index {GemLeagueIndexName}.")]
public sealed class PoeNinjaGemAddLeagueIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string GemLeagueIndexName = "GemLeague";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeNinjaGemCollection(database);
        await col.Indexes.DropOneAsync(GemLeagueIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeNinjaGemCollection(database);
        IndexKeysDefinitionBuilder<PoeNinjaApiGemPriceEnvalope> builder = new();
        var index = builder.Ascending(e => e.League);

        CreateIndexModel<PoeNinjaApiGemPriceEnvalope> model = new(index, new()
        {
            Name = GemLeagueIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}

[MongoMigration(PoeDatabaseSettings.Alias, 3, 4, Description = $"Add text index {GemNameIndexName}.")]
public sealed class PoeNinjaGemAddNameIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string GemNameIndexName = "GemName";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeNinjaGemCollection(database);
        await col.Indexes.DropOneAsync(GemNameIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeNinjaGemCollection(database);
        IndexKeysDefinitionBuilder<PoeNinjaApiGemPriceEnvalope> builder = new();
        var index = builder.Hashed(e => e.Price.Name);

        CreateIndexModel<PoeNinjaApiGemPriceEnvalope> model = new(index, new()
        {
            Name = GemNameIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}

[MongoMigration(PoeDatabaseSettings.Alias, 2, 3, Description = $"Add unique composite index {GemIdentifierIndexName}.")]
public sealed class PoeNinjaGemAddIdentifierIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string GemIdentifierIndexName = "GemIdentifier";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var gemPriceCollection = optionsAccessor.Value.GetPoeNinjaGemCollection(database);
        await gemPriceCollection.Indexes.DropOneAsync(GemIdentifierIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeNinjaGemCollection(database);
        IndexKeysDefinitionBuilder<PoeNinjaApiGemPriceEnvalope> builder = new();
        var index = builder.Combine(
            builder.Ascending(e => e.Price.Name),
            builder.Ascending(e => e.League),
            builder.Ascending(e => e.Price.GemLevel),
            builder.Ascending(e => e.Price.GemQuality)
        );

        CreateIndexModel<PoeNinjaApiGemPriceEnvalope> model = new(index, new()
        {
            Unique = true,
            Name = GemIdentifierIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}

[MongoMigration(PoeDatabaseSettings.Alias, 1, 2, Description = $"Add unique index {SkillNameIndexName}.")]
public sealed class PoeDbAddNameIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string SkillNameIndexName = "Name";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeDbSkillCollection(database);
        await col.Indexes.DropOneAsync(SkillNameIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeDbSkillCollection(database);
        IndexKeysDefinitionBuilder<PoeDbSkillEnvalope> builder = new();
        var index = builder.Ascending(e => e.Skill.Name.Id);

        CreateIndexModel<PoeDbSkillEnvalope> model = new(index, new()
        {
            Unique = true,
            Name = SkillNameIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}

[MongoMigration(PoeDatabaseSettings.Alias, 0, 1, Description = $"Add unique index {LeagueNameRealmIndexName}.")]
public sealed class PoeApiAddNameIndexMigration(IOptions<PoeDatabaseSettings> optionsAccessor) : IMongoMigration
{
    public const string LeagueNameRealmIndexName = "NameRealm";

    public async Task DownAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeLeagueCollection(database);
        await col.Indexes.DropOneAsync(LeagueNameRealmIndexName, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken = default)
    {
        var col = optionsAccessor.Value.GetPoeLeagueCollection(database);
        IndexKeysDefinitionBuilder<PoeLeague> builder = new();
        var index = builder.Combine(
            builder.Ascending(e => e.Name),
            builder.Ascending(e => e.Realm)
        );

        CreateIndexModel<PoeLeague> model = new(index, new()
        {
            Unique = true,
            Name = LeagueNameRealmIndexName
        });
        _ = await col.Indexes.CreateOneAsync(model, null, cancellationToken).ConfigureAwait(false);
    }
}
