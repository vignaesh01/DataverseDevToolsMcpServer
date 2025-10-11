using DataverseDevToolsMcpServer.Helpers;
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
    public class DataManagementTools
    {
        private readonly ILogger<DataManagementTools> _logger;
        public DataManagementTools(ILogger<DataManagementTools> logger)
        {
            _logger = logger;
        }


        [McpServerTool, Description("Execute/Run a FetchXml query. If paging-cookie attribute is present, its value should have correct XML escaping")]
        public async Task<string> ExecuteFetchXml(
            ServiceClient serviceClient,
            [Description("Fetch XML query to execute/run. If paging-cookie attribute is present, its value should have correct XML escaping")] string fetchXml
            //[Description("Page Number")] int pageNumber=1,
            //[Description("Number of records to be returned per page")] int recordsPerPage=10,
            //[Description("Pagination Cookie")] string pagingCookie = null
            )
        {
            if (serviceClient == null) throw new ArgumentNullException(nameof(serviceClient));
            if (string.IsNullOrWhiteSpace(fetchXml)) throw new ArgumentNullException(nameof(fetchXml));
            // if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0.");
            //if (recordsPerPage < 1) throw new ArgumentOutOfRangeException(nameof(recordsPerPage), "Records per page must be greater than 0.");

            string result = string.Empty;

            try
            {

                // Inject paging attributes into FetchXml
                //var fetchXmlWithPaging = DataManagementHelper.InjectPagingIntoFetchXml(fetchXml, pageNumber, recordsPerPage, pagingCookie);
                string fetchXmlWithPaging = null;
                if (fetchXml.Contains("paging-cookie"))
                {
                    fetchXmlWithPaging = fetchXml; // Use as is if paging-cookie is already present
                }
                else
                {
                    fetchXmlWithPaging = DataManagementHelper.CreateXml(fetchXml, null, 1, 10);
                }

                result += $"\nModified FetchXml with Paging:\n{fetchXmlWithPaging}\n\n";

                var fetchExpression = new FetchExpression(fetchXmlWithPaging);
                var responseCollection = await serviceClient.RetrieveMultipleAsync(fetchExpression);

                if (responseCollection.Entities.Count == 0)
                {
                    return $"No records found for the Fetch XML query {fetchXmlWithPaging}";
                }
                var sb = new StringBuilder();
                //sb.AppendLine($"Retrieved {responseCollection.Entities.Count} records (Page {pageNumber}):");
                sb.AppendLine($"Modified FetchXml with Paging:{fetchXmlWithPaging}");
                sb.AppendLine(JsonSerializer.Serialize(responseCollection.Entities));

                /*foreach (var entity in responseCollection.Entities)
                {
                    sb.AppendLine($"- {entity.LogicalName} ({entity.Id})");
                    foreach (var attr in entity.Attributes)
                    {
                        sb.AppendLine($"    {attr.Key}: {attr.Value}");
                    }
                }*/
                if (responseCollection.MoreRecords)
                {
                    //sb.AppendLine($"More records are available. To fetch the next page, use Page Number: {pageNumber + 1} and Paging Cookie: {responseCollection.PagingCookie}\"");
                    sb.AppendLine($"More records are available. To fetch the next page, increment page by 1 and Paging Cookie (paging-cookie): {System.Security.SecurityElement.Escape(responseCollection.PagingCookie)}  Use the paging-cookie with the correct XML escaping for retrieving next page.");

                }
                result = sb.ToString();



                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing paginated FetchXml query.");
                return $"Error executing paginated FetchXml query:{result} {ex.Message}";

            }

        }

        [McpServerTool, Description("Execute a Dataverse/Dynamics 365 Web API request and return the response.Request url should not contain /api/data/v9.*/")]
        public async Task<string> ExecuteWebApi(
            ServiceClient serviceClient,
            [Description("HTTP Method (GET, POST, PATCH, DELETE)")] string httpMethod,
            [Description(@"Web API request URL (relative to the service root URL).Request url should not contain /api/data/v9.*/. 
            The path and query parameters that you wish to pass onto the Web API")] string requestUrl,
            [Description("Request body in JSON format (for POST and PATCH requests)")] string requestBody = null,
            [Description("Additional headers")] Dictionary<String, List<String>> customHeaders = null,
            [Description("Content Type")] string contentType = "application/json"
            )
        {
            if (serviceClient == null) throw new ArgumentNullException(nameof(serviceClient));
            if (string.IsNullOrWhiteSpace(httpMethod)) throw new ArgumentNullException(nameof(httpMethod));
            if (string.IsNullOrWhiteSpace(requestUrl)) throw new ArgumentNullException(nameof(requestUrl));
            string result = string.Empty;
            try
            {
                var method = new System.Net.Http.HttpMethod(httpMethod.ToUpper());
                requestUrl =DataManagementHelper.CleanRequestUrl(requestUrl);
                var response = await serviceClient.ExecuteWebRequestAsync(method, requestUrl, requestBody, customHeaders, contentType);
                if (response.IsSuccessStatusCode)
                {
                    //append response headers to result
                    result += "Response Headers:\n";
                    foreach (var header in response.Headers)
                    {
                        result += $"{header.Key}: {string.Join(", ", header.Value)}\n";
                    }
                    result += "Response Body:\n";
                    result += await response.Content.ReadAsStringAsync();
                }
                else
                {
                    result += "Response Headers:\n";
                    foreach (var header in response.Headers)
                    {
                        result += $"{header.Key}: {string.Join(", ", header.Value)}\n";
                    }
                    result += $"Error: {response.StatusCode} - {response.ReasonPhrase}\n{await response.Content.ReadAsStringAsync()}";
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing Web API request.");
                return $"Error executing Web API request: {ex.Message}";
            }

        }

        [McpServerTool, Description(@"Create a record in Dataverse. This uses Dataverse/Dynamics 365 Web API.
                        Always reference the schema name from the entity metadata (not the logical name) when building lookup fields in your payload.
                        For lookups, use the format: <SchemaName>@odata.bind")]
        public async Task<string> CreateRecord(
            ServiceClient serviceClient,
            [Description("Entity Set Name from Entity metadata")] string entitySetName,
            [Description(@"Record data in JSON format. 
            Prepare the payload based on field/column type.
            For lookup columns,find the field/column schema name using entity metadata and use the <<field/column's schema name>>@odata.bind in payload")] string recordDataJson,
            [Description("Additional headers")] Dictionary<String, List<String>> customHeaders = null,
            [Description("Content Type")] string contentType = "application/json"
            )
        {
            if (serviceClient == null) throw new ArgumentNullException(nameof(serviceClient));
            if (string.IsNullOrWhiteSpace(entitySetName)) throw new ArgumentNullException(nameof(entitySetName));
            if (string.IsNullOrWhiteSpace(recordDataJson)) throw new ArgumentNullException(nameof(recordDataJson));
            string result = string.Empty;
            try
            {
                //var requestUrl = $"/api/data/v9.2/{entitySetName}";
                var response = await serviceClient.ExecuteWebRequestAsync(new System.Net.Http.HttpMethod("POST"), entitySetName, recordDataJson, customHeaders, contentType);
                if (response.IsSuccessStatusCode)
                {
                    if (response.Headers.Contains("OData-EntityId"))
                    {
                        var entityIdHeader = response.Headers.GetValues("OData-EntityId").FirstOrDefault();
                        result = $"Record created successfully. Entity ID: {entityIdHeader}";
                    }
                    else
                    {
                        result = "Record created successfully, but Entity ID not found in response.";
                    }
                }
                else
                {
                    result = $"Error: {response.StatusCode} - {response.ReasonPhrase}\n{await response.Content.ReadAsStringAsync()}";
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating record.");
                return $"Error creating record: {ex.Message}";
            }

        }

        [McpServerTool, Description(@"Update a record in Dataverse using record Id (GUID). This uses Dataverse/Dynamics 365 Web API.
                        Always reference the schema name from the entity metadata (not the logical name) when building lookup fields in your payload.
                        For lookups, use the format: <SchemaName>@odata.bind")]
        public async Task<string> UpdateRecord(
            ServiceClient serviceClient,
            [Description("Entity Set Name from Entity metadata")] string entitySetName,
            [Description("Record Id")] string recordId,
            [Description(@"Record data in JSON format. 
            Prepare the payload based on field/column type.
            For lookup columns,find the field/column schema name using entity metadata and use the <<field/column's schema name>>@odata.bind in payload")] string recordDataJson,
            [Description("Additional headers")] Dictionary<String, List<String>> customHeaders = null,
            [Description("Content Type")] string contentType = "application/json"
            )
        {
            if (serviceClient == null) throw new ArgumentNullException(nameof(serviceClient));
            if (string.IsNullOrWhiteSpace(entitySetName)) throw new ArgumentNullException(nameof(entitySetName));
            if (string.IsNullOrWhiteSpace(recordDataJson)) throw new ArgumentNullException(nameof(recordDataJson));
            if (string.IsNullOrWhiteSpace(recordId)) throw new ArgumentNullException(nameof(recordId));
            string result = string.Empty;
            entitySetName = $"{entitySetName}({recordId})"; // Append recordId to entitySetName for PATCH request
            try
            {
                //var requestUrl = $"/api/data/v9.2/{entitySetName}";
                var response = await serviceClient.ExecuteWebRequestAsync(new System.Net.Http.HttpMethod("PATCH"), entitySetName, recordDataJson, customHeaders, contentType);
                if (response.IsSuccessStatusCode)
                {

                    result = $"Record updated successfully. Entity ID: {recordId}";

                }
                else
                {
                    result = $"Error: {response.StatusCode} - {response.ReasonPhrase}\n{await response.Content.ReadAsStringAsync()}";
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating record.");
                return $"Error creating record: {ex.Message}";
            }
        }

        [McpServerTool, Description(@"Upsert/Update a record in Dataverse using Alternate Keys. This uses Dataverse/Dynamics 365 Web API.
                        Always reference the schema name from the entity metadata (not the logical name) when building lookup fields in your payload.
                        For lookups, use the format: <SchemaName>@odata.bind")]
        public async Task<string> UpsertRecord(
            ServiceClient serviceClient,
            [Description("Entity Set Name from Entity metadata")] string entitySetName,
            [Description(@"Alternate Keys for the Upsert Request. Sample format
            Sample with single string key : (custom_stringkey1='John%20Doe')
            Samples with multiple keys : 
            Sample 1 : (custom_numberkey1=12345,custom_numberkey2=67890)
            Sample 2 : (custom_stringkey1='John%20Doe',custom_stringkey2='Acme%20Corp')
            \n Ensure the string values are properly URL encoded"
            )] string alternateKeys,
            [Description(@"Record data in JSON format. 
            Prepare the payload based on field/column type.
            For lookup columns,find the field/column schema name using entity metadata and use the <<field/column's schema name>>@odata.bind in payload")] string recordDataJson,
            [Description("Additional headers")] Dictionary<String, List<String>> customHeaders = null,
            [Description("Content Type")] string contentType = "application/json"
            )
        {
            string result = string.Empty;
            if (serviceClient == null) throw new ArgumentNullException(nameof(serviceClient));
            if (string.IsNullOrWhiteSpace(entitySetName)) throw new ArgumentNullException(nameof(entitySetName));
            if (string.IsNullOrWhiteSpace(recordDataJson)) throw new ArgumentNullException(nameof(recordDataJson));
            if (string.IsNullOrWhiteSpace(alternateKeys)) throw new ArgumentNullException(nameof(alternateKeys));
            entitySetName = $"{entitySetName}{alternateKeys}"; // Append alternateKeys to entitySetName for PATCH request

            try
            {
                //var requestUrl = $"/api/data/v9.2/{entitySetName}";
                var response = await serviceClient.ExecuteWebRequestAsync(new System.Net.Http.HttpMethod("PATCH"), entitySetName, recordDataJson, customHeaders, contentType);
                if (response.IsSuccessStatusCode)
                {

                    result = $"Record upserted successfully.";

                }
                else
                {
                    result = $"Error: {response.StatusCode} - {response.ReasonPhrase}\n{await response.Content.ReadAsStringAsync()}";
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating record.");
                return $"Error creating record: {ex.Message}";
            }
        }

        [McpServerTool, Description(@"Delete a record in Dataverse using record Id (GUID).) This uses Dataverse/Dynamics 365 Web API.")]
        public async Task<string> DeleteRecord(
            ServiceClient serviceClient,
            [Description("Entity Set Name from Entity metadata")] string entitySetName,
            [Description("Record Id")] string recordId,
            [Description("Additional headers")] Dictionary<String, List<String>> customHeaders = null,
            [Description("Content Type")] string contentType = "application/json"
            )
        {
            if (serviceClient == null) throw new ArgumentNullException(nameof(serviceClient));
            if (string.IsNullOrWhiteSpace(entitySetName)) throw new ArgumentNullException(nameof(entitySetName));
            if (string.IsNullOrWhiteSpace(recordId)) throw new ArgumentNullException(nameof(recordId));
            string result = string.Empty;
            entitySetName = $"{entitySetName}({recordId})"; // Append recordId to entitySetName for DELETE request
            try
            {
                //var requestUrl = $"/api/data/v9.2/{entitySetName}";
                var response = await serviceClient.ExecuteWebRequestAsync(new System.Net.Http.HttpMethod("DELETE"), entitySetName, null, customHeaders, contentType);
                if (response.IsSuccessStatusCode)
                {
                    result = $"Record deleted successfully. Entity ID: {recordId}";
                }
                else
                {
                    result = $"Error: {response.StatusCode} - {response.ReasonPhrase}\n{await response.Content.ReadAsStringAsync()}";
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting record.");
                return $"Error deleting record: {ex.Message}";
            }

        }

    }
}
