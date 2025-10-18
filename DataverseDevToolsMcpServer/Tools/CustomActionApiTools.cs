using DataverseDevToolsMcpServer.Models;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Tools
{
    [McpServerToolType]
    public class CustomActionApiTools
    {
        private readonly ILogger<CustomActionApiTools> _logger;
        
        public CustomActionApiTools(ILogger<CustomActionApiTools> logger)
        {
            _logger = logger;
        }

        [McpServerTool, Description("Find Dataverse Custom Actions using keyword. Returns metadata to help frame Web API requests.")]
        public async Task<string> FindCustomActionUsingKeyword(
            ServiceClient serviceClient,
            [Description("Keyword to search for custom action")] string keyword)
        {
            try
            {
                string result = string.Empty;

                // Query for custom actions (workflows with category = 3)
                var query = new QueryExpression("workflow")
                {
                    ColumnSet = new ColumnSet("workflowid", "name", "uniquename", "description", 
                        "category", "statuscode", "statecode", "xaml", "primaryentity"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                
                // Category 3 = Action
                query.Criteria.AddCondition("category", ConditionOperator.Equal, 3);
                
                // Add keyword filter for name or unique name
                var keywordFilter = new FilterExpression(LogicalOperator.Or);
                keywordFilter.AddCondition("name", ConditionOperator.Like, $"%{keyword}%");
                keywordFilter.AddCondition("uniquename", ConditionOperator.Like, $"%{keyword}%");
                query.Criteria.AddFilter(keywordFilter);

                var response = await serviceClient.RetrieveMultipleAsync(query);

                if (response.Entities.Count == 0)
                {
                    return "No matching custom actions found.";
                }

                result += $"Found {response.Entities.Count} matching custom action(s)\n";
                
                if (response.Entities.Count > 1)
                {
                    result += "Since there are multiple results, the user should be prompted to pick a unique name from the results to proceed with subsequent action.\n";
                }

                var actions = new List<CustomAction>();
                foreach (var entity in response.Entities)
                {
                    var action = new CustomAction
                    {
                        workflowId = entity.GetAttributeValue<Guid>("workflowid"),
                        name = entity.GetAttributeValue<string>("name"),
                        uniqueName = entity.GetAttributeValue<string>("uniquename"),
                        description = entity.GetAttributeValue<string>("description"),
                        category = entity.GetAttributeValue<OptionSetValue>("category")?.Value,
                        categoryName = entity.FormattedValues.Contains("category") ? entity.FormattedValues["category"] : null,
                        statusCode = entity.GetAttributeValue<OptionSetValue>("statuscode")?.Value,
                        statusCodeName = entity.FormattedValues.Contains("statuscode") ? entity.FormattedValues["statuscode"] : null,
                        stateCode = entity.GetAttributeValue<OptionSetValue>("statecode")?.Value,
                        stateCodeName = entity.FormattedValues.Contains("statecode") ? entity.FormattedValues["statecode"] : null,
                        xaml = entity.GetAttributeValue<string>("xaml"),
                        primaryEntity = entity.GetAttributeValue<string>("primaryentity")
                    };
                    actions.Add(action);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                result += JsonSerializer.Serialize(actions, options);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching custom actions.");
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get metadata details for a Dataverse Custom Action by unique name. Returns input/output parameters and other metadata to help frame Web API requests.")]
        public async Task<string> GetCustomActionMetadata(
            ServiceClient serviceClient,
            [Description("Unique name of the custom action")] string uniqueName)
        {
            try
            {
                // Query for the custom action
                var query = new QueryExpression("workflow")
                {
                    ColumnSet = new ColumnSet("workflowid", "name", "uniquename", "description", 
                        "category", "statuscode", "statecode", "xaml", "primaryentity"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                
                query.Criteria.AddCondition("category", ConditionOperator.Equal, 3);
                query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);

                var response = await serviceClient.RetrieveMultipleAsync(query);

                if (response.Entities.Count == 0)
                {
                    return $"Custom action with unique name '{uniqueName}' not found.";
                }

                var entity = response.Entities[0];
                var action = new CustomAction
                {
                    workflowId = entity.GetAttributeValue<Guid>("workflowid"),
                    name = entity.GetAttributeValue<string>("name"),
                    uniqueName = entity.GetAttributeValue<string>("uniquename"),
                    description = entity.GetAttributeValue<string>("description"),
                    category = entity.GetAttributeValue<OptionSetValue>("category")?.Value,
                    categoryName = entity.FormattedValues.Contains("category") ? entity.FormattedValues["category"] : null,
                    statusCode = entity.GetAttributeValue<OptionSetValue>("statuscode")?.Value,
                    statusCodeName = entity.FormattedValues.Contains("statuscode") ? entity.FormattedValues["statuscode"] : null,
                    stateCode = entity.GetAttributeValue<OptionSetValue>("statecode")?.Value,
                    stateCodeName = entity.FormattedValues.Contains("statecode") ? entity.FormattedValues["statecode"] : null,
                    xaml = entity.GetAttributeValue<string>("xaml"),
                    primaryEntity = entity.GetAttributeValue<string>("primaryentity")
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                string result = "Custom Action Metadata:\n";
                result += JsonSerializer.Serialize(action, options);
                result += "\n\nTo execute this custom action via Web API, use:\n";
                result += $"POST {{organizationUrl}}/api/data/v9.2/{uniqueName}\n";
                result += "Content-Type: application/json\n\n";
                result += "Body: {{ /* input parameters as JSON */ }}\n";
                result += "\nNote: Parse the XAML to extract input/output parameter details if needed.";

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving custom action metadata.");
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Find Dataverse Custom APIs using keyword. Returns metadata to help frame Web API requests.")]
        public async Task<string> FindCustomApiUsingKeyword(
            ServiceClient serviceClient,
            [Description("Keyword to search for custom API")] string keyword)
        {
            try
            {
                string result = string.Empty;

                // Query for custom APIs
                var query = new QueryExpression("customapi")
                {
                    ColumnSet = new ColumnSet("customapiid", "uniquename", "displayname", "description",
                        "bindingtype", "boundentitylogicalname", "allowedcustomprocessingsteptype",
                        "isfunction", "isprivate", "plugintypeid"),
                    Criteria = new FilterExpression(LogicalOperator.Or)
                };
                
                // Add keyword filter for unique name or display name
                query.Criteria.AddCondition("uniquename", ConditionOperator.Like, $"%{keyword}%");
                query.Criteria.AddCondition("displayname", ConditionOperator.Like, $"%{keyword}%");

                var response = await serviceClient.RetrieveMultipleAsync(query);

                if (response.Entities.Count == 0)
                {
                    return "No matching custom APIs found.";
                }

                result += $"Found {response.Entities.Count} matching custom API(s)\n";
                
                if (response.Entities.Count > 1)
                {
                    result += "Since there are multiple results, the user should be prompted to pick a unique name from the results to proceed with subsequent action.\n";
                }

                var apis = new List<CustomApi>();
                foreach (var entity in response.Entities)
                {
                    var api = new CustomApi
                    {
                        customApiId = entity.GetAttributeValue<Guid>("customapiid"),
                        uniqueName = entity.GetAttributeValue<string>("uniquename"),
                        displayName = entity.GetAttributeValue<string>("displayname"),
                        description = entity.GetAttributeValue<string>("description"),
                        bindingType = entity.GetAttributeValue<OptionSetValue>("bindingtype")?.Value,
                        bindingTypeName = entity.FormattedValues.Contains("bindingtype") ? entity.FormattedValues["bindingtype"] : null,
                        boundEntityLogicalName = entity.GetAttributeValue<string>("boundentitylogicalname"),
                        allowedCustomProcessingStepType = entity.GetAttributeValue<OptionSetValue>("allowedcustomprocessingsteptype")?.Value,
                        allowedCustomProcessingStepTypeName = entity.FormattedValues.Contains("allowedcustomprocessingsteptype") ? entity.FormattedValues["allowedcustomprocessingsteptype"] : null,
                        isFunction = entity.GetAttributeValue<bool?>("isfunction"),
                        isPrivate = entity.GetAttributeValue<bool?>("isprivate"),
                        pluginTypeId = entity.GetAttributeValue<EntityReference>("plugintypeid")?.Id.ToString()
                    };
                    apis.Add(api);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                
                result += JsonSerializer.Serialize(apis, options);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching custom APIs.");
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get metadata details for a Dataverse Custom API by unique name. Returns request/response parameters and other metadata to help frame Web API requests.")]
        public async Task<string> GetCustomApiMetadata(
            ServiceClient serviceClient,
            [Description("Unique name of the custom API")] string uniqueName)
        {
            try
            {
                // Query for the custom API
                var query = new QueryExpression("customapi")
                {
                    ColumnSet = new ColumnSet("customapiid", "uniquename", "displayname", "description",
                        "bindingtype", "boundentitylogicalname", "allowedcustomprocessingsteptype",
                        "isfunction", "isprivate", "plugintypeid"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                
                query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);

                var response = await serviceClient.RetrieveMultipleAsync(query);

                if (response.Entities.Count == 0)
                {
                    return $"Custom API with unique name '{uniqueName}' not found.";
                }

                var entity = response.Entities[0];
                var api = new CustomApi
                {
                    customApiId = entity.GetAttributeValue<Guid>("customapiid"),
                    uniqueName = entity.GetAttributeValue<string>("uniquename"),
                    displayName = entity.GetAttributeValue<string>("displayname"),
                    description = entity.GetAttributeValue<string>("description"),
                    bindingType = entity.GetAttributeValue<OptionSetValue>("bindingtype")?.Value,
                    bindingTypeName = entity.FormattedValues.Contains("bindingtype") ? entity.FormattedValues["bindingtype"] : null,
                    boundEntityLogicalName = entity.GetAttributeValue<string>("boundentitylogicalname"),
                    allowedCustomProcessingStepType = entity.GetAttributeValue<OptionSetValue>("allowedcustomprocessingsteptype")?.Value,
                    allowedCustomProcessingStepTypeName = entity.FormattedValues.Contains("allowedcustomprocessingsteptype") ? entity.FormattedValues["allowedcustomprocessingsteptype"] : null,
                    isFunction = entity.GetAttributeValue<bool?>("isfunction"),
                    isPrivate = entity.GetAttributeValue<bool?>("isprivate"),
                    pluginTypeId = entity.GetAttributeValue<EntityReference>("plugintypeid")?.Id.ToString()
                };

                // Query for request parameters
                var requestParamsQuery = new QueryExpression("customapirequestparameter")
                {
                    ColumnSet = new ColumnSet("uniquename", "displayname", "description", "type", "isoptional"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                requestParamsQuery.Criteria.AddCondition("customapiid", ConditionOperator.Equal, api.customApiId);

                var requestParamsResponse = await serviceClient.RetrieveMultipleAsync(requestParamsQuery);

                // Query for response parameters
                var responseParamsQuery = new QueryExpression("customapiresponseproperty")
                {
                    ColumnSet = new ColumnSet("uniquename", "displayname", "description", "type"),
                    Criteria = new FilterExpression(LogicalOperator.And)
                };
                responseParamsQuery.Criteria.AddCondition("customapiid", ConditionOperator.Equal, api.customApiId);

                var responseParamsResponse = await serviceClient.RetrieveMultipleAsync(responseParamsQuery);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                string result = "Custom API Metadata:\n";
                result += JsonSerializer.Serialize(api, options);
                
                result += "\n\nRequest Parameters:\n";
                if (requestParamsResponse.Entities.Count > 0)
                {
                    var requestParams = requestParamsResponse.Entities.Select(e => new
                    {
                        uniqueName = e.GetAttributeValue<string>("uniquename"),
                        displayName = e.GetAttributeValue<string>("displayname"),
                        description = e.GetAttributeValue<string>("description"),
                        type = e.GetAttributeValue<OptionSetValue>("type")?.Value,
                        typeName = e.FormattedValues.Contains("type") ? e.FormattedValues["type"] : null,
                        isOptional = e.GetAttributeValue<bool?>("isoptional")
                    });
                    result += JsonSerializer.Serialize(requestParams, options);
                }
                else
                {
                    result += "No request parameters defined.";
                }
                
                result += "\n\nResponse Properties:\n";
                if (responseParamsResponse.Entities.Count > 0)
                {
                    var responseParams = responseParamsResponse.Entities.Select(e => new
                    {
                        uniqueName = e.GetAttributeValue<string>("uniquename"),
                        displayName = e.GetAttributeValue<string>("displayname"),
                        description = e.GetAttributeValue<string>("description"),
                        type = e.GetAttributeValue<OptionSetValue>("type")?.Value,
                        typeName = e.FormattedValues.Contains("type") ? e.FormattedValues["type"] : null
                    });
                    result += JsonSerializer.Serialize(responseParams, options);
                }
                else
                {
                    result += "No response properties defined.";
                }

                result += "\n\nTo execute this custom API via Web API:";
                
                if (api.isFunction == true)
                {
                    result += $"\nGET {{organizationUrl}}/api/data/v9.2/{uniqueName}(/* parameters */)";
                }
                else
                {
                    if (api.bindingType == 1) // Entity binding
                    {
                        result += $"\nPOST {{organizationUrl}}/api/data/v9.2/{{entitySetName}}({{recordId}})/{uniqueName}";
                    }
                    else if (api.bindingType == 2) // Entity Collection binding
                    {
                        result += $"\nPOST {{organizationUrl}}/api/data/v9.2/{{entitySetName}}/{uniqueName}";
                    }
                    else // Global (0)
                    {
                        result += $"\nPOST {{organizationUrl}}/api/data/v9.2/{uniqueName}";
                    }
                    result += "\nContent-Type: application/json\n\n";
                    result += "Body: { /* request parameters as JSON */ }";
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving custom API metadata.");
                return $"Error: {ex.Message}";
            }
        }
    }
}
