using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Models
{
    public class PrivilegeDetail
    {
        public string name { get; set; }
        public int access { get; set; }
        public string accessRightStr { get; set; }
        public string depthStr { get; set; }
    }
}
