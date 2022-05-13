﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using MccSoft.IntegreSql.EF;
using MccSoft.IntegreSql.EF.DatabaseInitialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExampleWeb;

public class IntegrationTestBase
{
    protected readonly HttpClient _httpClient;
    private readonly IDatabaseInitializer _databaseInitializer;

    protected IntegrationTestBase(DatabaseType databaseType)
    {
        _databaseInitializer = CreateDatabaseInitializer(databaseType);
        var connectionString = _databaseInitializer.CreateDatabaseGetConnectionStringSync(
            new DatabaseSeedingOptions<ExampleDbContext>(Name: "Integration")
        );

        var webAppFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(
            builder =>
            {
                builder.ConfigureAppConfiguration(
                    (context, configuration) =>
                    {
                        configuration.AddInMemoryCollection(
                            new KeyValuePair<string, string>[] { new("DisableSeed", "true") }
                        );
                    }
                );
                builder.ConfigureServices(
                    services =>
                    {
                        var descriptor = services.Single(
                            d => d.ServiceType == typeof(DbContextOptions<ExampleDbContext>)
                        );
                        services.Remove(descriptor);

                        services.AddDbContext<ExampleDbContext>(
                            options => _databaseInitializer.UseProvider(options, connectionString)
                        );
                    }
                );
            }
        );

        _httpClient = webAppFactory.CreateDefaultClient();
    }

    private IDatabaseInitializer CreateDatabaseInitializer(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.Postgres
              => new NpgsqlDatabaseInitializer(
                  // This is needed if you run tests NOT inside the container.
                  // 5434 is the public port number of Postgresql instance
                  connectionStringOverride: new() { Host = "localhost", Port = 5434, }
              ),
            DatabaseType.Sqlite => new SqliteDatabaseInitializer(),
            _ => throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, null)
        };
    }
}
