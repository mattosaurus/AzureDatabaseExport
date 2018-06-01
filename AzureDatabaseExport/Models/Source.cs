using System;
using System.Collections.Generic;
using System.Text;

namespace AzureDatabaseExport.Models
{
    public class Source
    {
        public string SqlServerResourceGroup { get; set; }

        public string SqlServerName { get; set; }

        public string SqlDatabaseName { get; set; }

        public string SqlAdminUsername { get; set; }

        public string SqlAdminPassword { get; set; }
    }
}
