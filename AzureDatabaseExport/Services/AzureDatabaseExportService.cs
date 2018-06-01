using AzureDatabaseExport.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Sql.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AzureDatabaseExport.Services
{
    public interface IAzureDatabaseExportService
    {
        Task Run(Backup backup);
    }

    class AzureDatabaseExportService : IAzureDatabaseExportService
    {
        private readonly ILogger<AzureDatabaseExportService> _logger;
        private readonly IConfigurationRoot _config;

        public AzureDatabaseExportService(ILogger<AzureDatabaseExportService> logger, IConfigurationRoot config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task Run(Backup backup)
        {
            try
            {
                await BackupDatabase(backup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backing up database {@DatbaseName} on server {@ServerName}", backup.Source.SqlDatabaseName, backup.Source.SqlServerName);
            }
        }

        public async Task BackupDatabase(Backup backup)
        {
            _logger.LogInformation("Authenticating Azure Management object");
            AzureCredentials credentials = SdkContext.AzureCredentialsFactory.FromFile(Directory.GetCurrentDirectory() + "\\Azure_Credentials.txt");

            IAzure azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithDefaultSubscription();

            _logger.LogInformation("Get SQL database reference");
            ISqlServer sqlServer = await azure.SqlServers.GetByResourceGroupAsync(backup.Source.SqlServerResourceGroup, backup.Source.SqlServerName);

            ISqlDatabase sqlDatabase = await sqlServer.Databases.GetAsync(backup.Source.SqlDatabaseName);

            _logger.LogInformation("Get storage account reference");
            IStorageAccount storageAccount = azure.StorageAccounts.GetByResourceGroup(backup.Destination.StorageAccountResourceGroup, backup.Destination.StorageAccountName);

            _logger.LogInformation("Export database to storage account");
            string blobPath = backup.Source.SqlDatabaseName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".bacpac";

            ISqlDatabaseImportExportResponse exportedSqlDatabase = sqlDatabase.ExportTo(storageAccount, backup.Destination.StorageContainerName, blobPath)
                .WithSqlAdministratorLoginAndPassword(backup.Source.SqlAdminUsername, backup.Source.SqlAdminPassword)
                .Execute();

            _logger.LogInformation("Get reference to storage account");
            CloudBlobContainer container = new CloudBlobContainer(new Uri(backup.Destination.StorageContainerConnectionString));

            _logger.LogInformation("Get reference to blob");
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobPath);

            _logger.LogInformation("Download blob");
            if (await blockBlob.ExistsAsync())
            {
                string filePath = backup.Destination.LocalDirectory + blobPath;
                await blockBlob.DownloadToFileAsync(filePath, FileMode.Create);
            }
            else
            {
                throw new FileNotFoundException("Target blob not found in storage account");
            }

            if (backup.Destination.DeleteFromStorageAfterDownload)
            {
                _logger.LogInformation("Removing file from storage");
                await blockBlob.DeleteAsync();
            }
        }
    }
}
