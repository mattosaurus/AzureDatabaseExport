# AzureDatabaseExport
.NET Core console application to export Azure SQL database to a storage account and then to download the resulting BACPAC to a local directory.

# Setup
In order to run this project a service pronciple needs to be created with access to the target database as well as the storage account where it is to be exported to as specified [here](https://blog.bitscry.com/2018/06/01/exporting-an-azure-sql-databases-using-net/). The appsettings.json and Azure_credentials.txt files should then be updated with the relevant connection details.
