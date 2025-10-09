using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Models
{
    public class BusinessUnit
    {
        public Guid businessUnitId { get; set; }
        public string name { get; set; }

        public Guid? parentBusinessUnitId { get; set; }
        public string? parentBusinessUnitName { get; set; }
        public string description { get; set; }

    }
}
