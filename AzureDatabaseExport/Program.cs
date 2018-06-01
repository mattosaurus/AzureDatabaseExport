using System;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using AzureDatabaseExport.Services;
using AzureDatabaseExport.Models;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace AzureDatabaseExport
{
    class Program
    {
        public static IConfigurationRoot configuration;

        static int Main(string[] args)
        {
            try
            {
                // Start!
                MainAsync(args).Wait();
                return 0;
            }
            catch (Exception ex)
            {
                return 1;
            }
        }

        static async Task MainAsync(string[] args)
        {
            // Create service collection
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // Create service provider
            Log.Information("Building service provider");
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            // Get list of objects for backup
            List<Backup> backups = new List<Backup>();
            configuration.GetSection("Backups").Bind(backups);

            // Create a block with an asynchronous action
            Log.Information("Backing up databases");
            var block = new ActionBlock<Backup>(
                async x => await serviceProvider.GetService<App>().Run(x),
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = int.Parse(configuration["Threading:BoundedCapacity"]), // Cap the item count
                    MaxDegreeOfParallelism = int.Parse(configuration["Threading:MaxDegreeOfParallelism"])
                });

            // Add items to the block and asynchronously wait if BoundedCapacity is reached
            foreach (Backup backup in backups)
            {
                await block.SendAsync(backup);
            }

            block.Complete();
            await block.Completion;
        }

        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Add logging
            serviceCollection.AddSingleton(new LoggerFactory()
                .AddConsole()
                .AddSerilog()
                .AddDebug());
            serviceCollection.AddLogging();

            // Build configuration
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();

            // Initialize serilog logger
            Log.Logger = new LoggerConfiguration()
                 .WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
                 .MinimumLevel.Debug()
                 .Enrich.FromLogContext()
                 .CreateLogger();

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton<IConfigurationRoot>(configuration);

            // Add services
            serviceCollection.AddTransient<IAzureDatabaseExportService, AzureDatabaseExportService>();

            // Add app
            serviceCollection.AddTransient<App>();
        }
    }
}
