using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Models
{
    public class PluginTraceLog
    {
        // Properties added for serialization of plugin trace log fields
        public string configuration { get; set; }
        public Guid? correlationId { get; set; }
        public Guid? createdbyId { get; set; }
        public string createdByName { get; set; }

        public DateTime? createdOn { get; set; }
        public int? depth { get; set; }
        public string exceptionDetails { get; set; }
        public bool? isSystemCreated { get; set; }
        public string messageBlock { get; set; }
        public string messageName { get; set; }
        public int? mode { get; set; }
        public string modeName { get; set; }
  
        public int? operationType { get; set; }
        public string operationTypeName { get; set; }
        
        public int? performanceExecutionDuration { get; set; }
        public Guid? pluginStepId { get; set; }
        public Guid? pluginTraceLogId { get; set; }
        public string primaryEntity { get; set; }
        public string profile { get; set; }
        public Guid? requestId { get; set; }
        public string secureConfiguration { get; set; }
        public string typeName { get; set; }
    }
}
