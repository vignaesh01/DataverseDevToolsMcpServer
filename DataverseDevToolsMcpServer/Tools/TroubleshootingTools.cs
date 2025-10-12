using DataverseDevToolsMcpServer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace DataverseDevToolsMcpServer.Tools
{
    [McpServerToolType]
    public class TroubleshootingTools
    {
        private readonly ILogger<TroubleshootingTools> _logger;
        public TroubleshootingTools(ILogger<TroubleshootingTools> logger)
        {
            _logger = logger;
        }

        [McpServerTool, Description("Get Plugin Trace Logs for a plugin by name")]
        public async Task<string> GetPluginTracesByName(ServiceClient serviceClient,
            [Description("Plugin Class Name")] string pluginClassName,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Pagination Cookie")] string pagingCookie = null,
            [Description("Number of records to be returned per page")] int numOfRecordsPerPage = 10)
        {
            string result = string.Empty;
            try
            {
                string[] columnSet = new string[] { "configuration", "correlationid", "createdby", "createdon", "depth","exceptiondetails",
                    "issystemcreated","messageblock","messagename","mode","operationtype","performanceexecutionduration","pluginstepid","plugintracelogid",
                "primaryentity","profile","requestid","secureconfiguration","typename"};
                // QueryExpression to fetch plugin trace logs where type name contains pluginClassName
                var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("plugintracelog")
                {
                    ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet(columnSet),
                    Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                    {
                        Conditions =
                        {
                            new Microsoft.Xrm.Sdk.Query.ConditionExpression("typename", Microsoft.Xrm.Sdk.Query.ConditionOperator.Like, $"%{pluginClassName}%")
                        }
                    },
                    PageInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo
                    {
                        Count = numOfRecordsPerPage,
                        PageNumber = pageNumber,
                        PagingCookie = pagingCookie
                    },
                    Orders =
                    {
                        new Microsoft.Xrm.Sdk.Query.OrderExpression("createdon", Microsoft.Xrm.Sdk.Query.OrderType.Descending)
                    }
                };

                var response = await serviceClient.RetrieveMultipleAsync(query);
                if (response.Entities.Count == 0)
                {
                    return $"No plugin trace logs found for {pluginClassName}";
                }

                List<PluginTraceLog> pluginTraceLogs = new List<PluginTraceLog>();
                foreach (Entity ptl in response.Entities)
                {
                    pluginTraceLogs.Add(new PluginTraceLog
                    {
                        configuration = ptl.GetAttributeValue<string>("configuration"),
                        correlationId = ptl.GetAttributeValue<Guid?>("correlationid"),
                        createdbyId = ptl.GetAttributeValue<EntityReference>("createdby")?.Id,
                        createdByName = ptl.GetAttributeValue<EntityReference>("createdby")?.Name,
                        createdOn = ptl.GetAttributeValue<DateTime?>("createdon"),
                        depth = ptl.GetAttributeValue<int?>("depth"),
                        exceptionDetails = ptl.GetAttributeValue<string>("exceptiondetails"),
                        isSystemCreated = ptl.GetAttributeValue<bool?>("issystemcreated"),
                        messageBlock = ptl.GetAttributeValue<string>("messageblock"),
                        messageName = ptl.GetAttributeValue<string>("messagename"),
                        mode = ptl.GetAttributeValue<OptionSetValue?>("mode")?.Value,
                        modeName = ptl.FormattedValues.Contains("mode") ? ptl.FormattedValues["mode"] : null,
                        operationType = ptl.GetAttributeValue<OptionSetValue?>("operationtype")?.Value,
                        operationTypeName = ptl.FormattedValues.Contains("operationtype") ? ptl.FormattedValues["operationtype"] : null,
                        performanceExecutionDuration = ptl.GetAttributeValue<int?>("performanceexecutionduration"),
                        pluginStepId = ptl.GetAttributeValue<Guid?>("pluginstepid"),
                        pluginTraceLogId = ptl.GetAttributeValue<Guid?>("plugintracelogid"),
                        primaryEntity = ptl.GetAttributeValue<string>("primaryentity"),
                        profile = ptl.GetAttributeValue<string>("profile"),
                        requestId = ptl.GetAttributeValue<Guid?>("requestid"),
                        secureConfiguration = ptl.GetAttributeValue<string>("secureconfiguration"),
                        typeName = ptl.GetAttributeValue<string>("typename")
                    });
                }

                result += string.Join("\n", JsonSerializer.Serialize(pluginTraceLogs));


                if (response.MoreRecords)
                {
                    var nextPageNumber = pageNumber + 1;
                    var nextPagingCookie = response.PagingCookie;
                    result += $"\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber} and Paging Cookie: {nextPagingCookie}";
                }
                else
                {
                    result += "\nNo more records available.";
                }

                    return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching plugin trace logs for plugin: {PluginClassName}", pluginClassName);
                return $"Error fetching plugin trace logs: {ex.Message}";
            }

        }

        [McpServerTool, Description("Get Plugin Trace Logs for a plugin by correlation id")]
        public async Task<string> GetPluginTracesByCorrId(ServiceClient serviceClient,
            [Description("Correlation Id")] string correlationId,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Pagination Cookie")] string pagingCookie = null,
            [Description("Number of records to be returned per page")] int numOfRecordsPerPage = 10)
        {
            string result = string.Empty;
            try
            {
                string[] columnSet = new string[] { "configuration", "correlationid", "createdby", "createdon", "depth", "exceptiondetails",
                    "issystemcreated","messageblock","messagename","mode","operationtype","performanceexecutionduration","pluginstepid","plugintracelogid",
                "primaryentity","profile","requestid","secureconfiguration","typename"};
                // QueryExpression to fetch plugin trace logs where
                var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("plugintracelog")
                {
                    ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet(columnSet),
                    Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                    {
                        Conditions =
                        {
                            new Microsoft.Xrm.Sdk.Query.ConditionExpression("correlationid", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, correlationId)
                        }
                    },
                    PageInfo = new Microsoft.Xrm.Sdk.Query.PagingInfo
                    {
                        Count = numOfRecordsPerPage,
                        PageNumber = pageNumber,
                        PagingCookie = pagingCookie
                    },
                    Orders =
                    {
                        new Microsoft.Xrm.Sdk.Query.OrderExpression("createdon", Microsoft.Xrm.Sdk.Query.OrderType.Descending)
                    }
                };
                var response = await serviceClient.RetrieveMultipleAsync(query);
                if (response.Entities.Count == 0)
                {
                    return $"No plugin trace logs found for Correlation Id: {correlationId}";
                }
                
                List<PluginTraceLog> pluginTraceLogs = new List<PluginTraceLog>();
                foreach (Entity ptl in response.Entities)
                {
                    pluginTraceLogs.Add(new PluginTraceLog
                    {
                        configuration = ptl.GetAttributeValue<string>("configuration"),
                        correlationId = ptl.GetAttributeValue<Guid?>("correlationid"),
                        createdbyId = ptl.GetAttributeValue<EntityReference>("createdby")?.Id,
                        createdByName = ptl.GetAttributeValue<EntityReference>("createdby")?.Name,
                        createdOn = ptl.GetAttributeValue<DateTime?>("createdon"),
                        depth = ptl.GetAttributeValue<int?>("depth"),
                        exceptionDetails = ptl.GetAttributeValue<string>("exceptiondetails"),
                        isSystemCreated = ptl.GetAttributeValue<bool?>("issystemcreated"),
                        messageBlock = ptl.GetAttributeValue<string>("messageblock"),
                        messageName = ptl.GetAttributeValue<string>("messagename"),
                        mode = ptl.GetAttributeValue<OptionSetValue?>("mode")?.Value,
                        modeName = ptl.FormattedValues.Contains("mode") ? ptl.FormattedValues["mode"] : null,
                        operationType = ptl.GetAttributeValue<OptionSetValue?>("operationtype")?.Value,
                        operationTypeName = ptl.FormattedValues.Contains("operationtype") ? ptl.FormattedValues["operationtype"] : null,
                        performanceExecutionDuration = ptl.GetAttributeValue<int?>("performanceexecutionduration"),
                        pluginStepId = ptl.GetAttributeValue<Guid?>("pluginstepid"),
                        pluginTraceLogId = ptl.GetAttributeValue<Guid?>("plugintracelogid"),
                        primaryEntity = ptl.GetAttributeValue<string>("primaryentity"),
                        profile = ptl.GetAttributeValue<string>("profile"),
                        requestId = ptl.GetAttributeValue<Guid?>("requestid"),
                        secureConfiguration = ptl.GetAttributeValue<string>("secureconfiguration"),
                        typeName = ptl.GetAttributeValue<string>("typename")
                    });
                }


                result += string.Join("\n", JsonSerializer.Serialize(pluginTraceLogs));

                if (response.MoreRecords)
                {
                    var nextPageNumber = pageNumber + 1;
                    var nextPagingCookie = response.PagingCookie;
                    result += $"\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber} and Paging Cookie: {nextPagingCookie}";
                }
                else
                {
                    result += "\nNo more records available.";
                }
                    return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching plugin trace logs for Correlation Id: {CorrelationId}", correlationId);
                return $"Error fetching plugin trace logs: {ex.Message}";
            }
        }
    }
}
