using System;
using System.Collections.Generic;
using System.Text;

namespace AzureDatabaseExport.Models
{
    public class Destination
    {
        public string StorageAccountResourceGroup { get; set; }

        public string StorageAccountName { get; set; }

        public string StorageContainerName { get; set; }

        public string StorageContainerConnectionString { get; set; }

        public bool DeleteFromStorageAfterDownload { get; set; }

        public string LocalDirectory { get; set; }
    }
}
