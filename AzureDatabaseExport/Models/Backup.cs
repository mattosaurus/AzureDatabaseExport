using System;
using System.Collections.Generic;
using System.Text;

namespace AzureDatabaseExport.Models
{
    public class Backup
    {
        public Source Source { get; set; }

        public Destination Destination { get; set; }
    }
}
