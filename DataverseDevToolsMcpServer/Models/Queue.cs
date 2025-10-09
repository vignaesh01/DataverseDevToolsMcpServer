using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Models
{
    public class Queue
    {
        public Guid queueId { get; set; }
        public string name { get; set; }
        public string description { get; set; }
    }
}
