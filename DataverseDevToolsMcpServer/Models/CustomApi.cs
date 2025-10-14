using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Models
{
    public class CustomApi
    {
        public Guid customApiId { get; set; }
        public string uniqueName { get; set; }
        public string displayName { get; set; }
        public string description { get; set; }
        public int? bindingType { get; set; }
        public string bindingTypeName { get; set; }
        public string boundEntityLogicalName { get; set; }
        public int? allowedCustomProcessingStepType { get; set; }
        public string allowedCustomProcessingStepTypeName { get; set; }
        public bool? isFunction { get; set; }
        public bool? isPrivate { get; set; }
        public string pluginTypeId { get; set; }
    }
}
