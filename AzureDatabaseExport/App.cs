using AzureDatabaseExport.Models;
using AzureDatabaseExport.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AzureDatabaseExport
{
    class App
    {
        private readonly IAzureDatabaseExportService _azureDatabaseExportService;
        private readonly ILogger<App> _logger;
        private readonly IConfigurationRoot _config;

        public App(IAzureDatabaseExportService azureDatabaseExportService, IConfigurationRoot config, ILogger<App> logger)
        {
            _azureDatabaseExportService = azureDatabaseExportService;
            _logger = logger;
            _config = config;
        }

        public async Task Run(Backup backup)
        {
            string logKey = Guid.NewGuid().ToString();
            DateTime start = DateTime.Now;

            _logger.LogInformation("Starting Service on Thread {@ThreadId} for database {@DatbaseName} on {@ServerName} with LogKey {@LogKey}", System.Threading.Thread.CurrentThread.ManagedThreadId, backup.Source.SqlDatabaseName, backup.Source.SqlServerName, logKey);

            // Push ID to log
            using (LogContext.PushProperty("LogKey", logKey))
            {
                await _azureDatabaseExportService.Run(backup);
            }

            DateTime end = DateTime.Now;
            TimeSpan elapsed = end - start;
            _logger.LogInformation("Ending Service on Thread {@ThreadId} for database {@DatbaseName} on {@ServerName} with LogKey {@LogKey}. Elapsed time: {@ElapsedTime}ms", System.Threading.Thread.CurrentThread.ManagedThreadId, backup.Source.SqlDatabaseName, backup.Source.SqlServerName, logKey, elapsed.TotalMilliseconds.ToString());
        }
    }
}
