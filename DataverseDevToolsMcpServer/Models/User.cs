using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Models
{
    public class User
    {
        public Guid userId { get; set; }
        public string fullName { get; set; }
        public string domainName { get; set; }
        public Guid businessUnitId { get; set; }
        public string businessUnitName { get; set; }
    }
}
