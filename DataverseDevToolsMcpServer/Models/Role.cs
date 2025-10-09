using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Models
{
    public class Role
    {
        public string name { get; set; }
        public Guid roleId { get; set; }
        public Guid businessUnitId { get; set; }
        public string businessUnitName { get; set; }
    }
}
