using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Models
{
    public class CustomAction
    {
        public Guid workflowId { get; set; }
        public string name { get; set; }
        public string uniqueName { get; set; }
        public string description { get; set; }
        public int? category { get; set; }
        public string categoryName { get; set; }
        public int? statusCode { get; set; }
        public string statusCodeName { get; set; }
        public int? stateCode { get; set; }
        public string stateCodeName { get; set; }
        public string xaml { get; set; }
        public string primaryEntity { get; set; }
    }
}
