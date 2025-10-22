using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Tools
{
    [McpServerToolType]
    public class EntityMetadataTools
    {
        private readonly ILogger<EntityMetadataTools> _logger;
        public EntityMetadataTools(ILogger<EntityMetadataTools> logger)
        {
            _logger = logger;
        }

        [McpServerTool, Description("Find Entity/Table Logical Name using keyword.")]
        public async Task<string> FindEntityLogicalNameUsingKeyword(ServiceClient serviceClient,
            [Description("Keyword to search for entity/table")] string keyword)
        {
            try
            {
                string result = string.Empty;

                // Get all entities metadata
                var allEntitiesRequest = new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = true
                };

                var allEntitiesResponse =
                    (RetrieveAllEntitiesResponse)await serviceClient.ExecuteAsync(allEntitiesRequest);

                var matchingEntities = allEntitiesResponse.EntityMetadata
                    .Where(e =>
                        (!string.IsNullOrEmpty(e.LogicalName) &&
                         e.LogicalName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                     || (e.DisplayName?.UserLocalizedLabel != null &&
                         e.DisplayName.UserLocalizedLabel.Label.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                         || (e.Description?.UserLocalizedLabel != null &&
                         e.Description.UserLocalizedLabel.Label.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                if (matchingEntities.Count == 0)
                {
                    string negResult = string.Join("\n", "No matching entities found. Let the LLM Agent find the entity/table from the below full list of entities/tables: ");
                    var entities = allEntitiesResponse.EntityMetadata;
                    // Serialize the list of entities to JSON for easier reading
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = false,
                        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                    };
                    string jsonEntities = JsonSerializer.Serialize(entities.Select(e => new
                    {
                        e.LogicalName,
                        DisplayName = e.DisplayName?.UserLocalizedLabel?.Label ?? "N/A",
                        Description = e.Description?.UserLocalizedLabel?.Label ?? "N/A"
                    }), options);

                    negResult += string.Join("\n", $"{jsonEntities}");
                    return negResult;
                }

                result += string.Join("\n", $"Found {matchingEntities.Count} matching entities");
                if (matchingEntities.Count > 1)
                {
                    result += string.Join("\n", "Since there are multiple results, the user should be prompted to pick a logical name from the results to proceed with subsequent action.");
                }

                foreach (var entity in matchingEntities)
                {
                    var displayName = entity.DisplayName?.UserLocalizedLabel?.Label ?? "N/A";
                    result += string.Join("\n", $"Logical Name: {entity.LogicalName}, Display Name: {displayName}");
                }

                //result += string.Join("\n", JsonSerializer.Serialize(matchingEntities));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching entity metadata.");
                return $"Error: {ex.Message}";
            }

        }

        [McpServerTool, Description("Get list of all entities/tables in the environment")]
        public async Task<string> ListAllEntities(ServiceClient serviceClient)
        {
            try
            {
                // Get all entities metadata
                var allEntitiesRequest = new RetrieveAllEntitiesRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    RetrieveAsIfPublished = true
                };
                var allEntitiesResponse =
                    (RetrieveAllEntitiesResponse)await serviceClient.ExecuteAsync(allEntitiesRequest);
                var entities = allEntitiesResponse.EntityMetadata;
                if (entities == null || entities.Length == 0)
                {
                    return "No entities found in the environment.";
                }
                // Serialize the list of entities to JSON for easier reading
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                string jsonEntities = JsonSerializer.Serialize(entities.Select(e => new
                {
                    e.LogicalName,
                    DisplayName = e.DisplayName?.UserLocalizedLabel?.Label ?? "N/A",
                    Description = e.Description?.UserLocalizedLabel?.Label ?? "N/A"
                }), options);
                return jsonEntities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all entities.");
                return $"Error: {ex.Message}";
            }
        }

        // Commented out - replaced by GetEntityMetadata, GetFields, and GetRelationships methods for better pagination support
        //[McpServerTool, Description("Get Entity/Table Metadata Details like Entity/Table properties, attributes/fields/columns, relationships using logical name of the entity/table")]
        //public async Task<string> GetEntityMetadataDetails(ServiceClient serviceClient,
        //    [Description("Entity/Table Logical Name")] string entityLogicalName)
        //{
        //    try
        //    {
        //        // Prepare the request to retrieve entity metadata
        //        var request = new RetrieveEntityRequest
        //        {
        //            EntityFilters = (EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships),
        //            LogicalName = entityLogicalName,
        //            RetrieveAsIfPublished = true
        //        };
        //        // Execute the request
        //        var response = (RetrieveEntityResponse)await serviceClient.ExecuteAsync(request);
        //        // Get the entity metadata from the response
        //        var entityMetadata = response.EntityMetadata;
        //        if (entityMetadata == null)
        //        {
        //            return $"No metadata found for entity: {entityLogicalName}";
        //        }
        //        // Serialize the metadata to JSON for easier reading
        //        var options = new JsonSerializerOptions
        //        {
        //            WriteIndented = false,
        //            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        //        };

        //        string jsonMetadata = JsonSerializer.Serialize(entityMetadata, options);

        //        return jsonMetadata;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error retrieving entity metadata.");
        //        return $"Error: {ex.Message}";
        //    }

        //}

        [McpServerTool, Description("Get Entity/Table metadata information using logical name of the entity/table")]
        public async Task<string> GetEntityMetadata(ServiceClient serviceClient,
            [Description("Entity/Table Logical Name")] string entityLogicalName)
        {
            try
            {
                // Prepare the request to retrieve entity metadata
                var request = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.Entity,
                    LogicalName = entityLogicalName,
                    RetrieveAsIfPublished = true
                };
                // Execute the request
                var response = (RetrieveEntityResponse)await serviceClient.ExecuteAsync(request);
                // Get the entity metadata from the response
                var entityMetadata = response.EntityMetadata;
                if (entityMetadata == null)
                {
                    return $"No metadata found for entity: {entityLogicalName}";
                }
                // Serialize the metadata to JSON for easier reading
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                string jsonMetadata = JsonSerializer.Serialize(entityMetadata, options);

                return jsonMetadata;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity metadata.");
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get attributes/fields/columns metadata information for an entity/table in a paginated fashion. IMPORTANT: Maximum 50 records per page. Do not exceed this limit.")]
        public async Task<string> GetFields(ServiceClient serviceClient,
            [Description("Entity/Table Logical Name")] string entityLogicalName,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Number of records to be returned per page. Maximum allowed is 50. Do not set this value higher than 50.")] int numOfRecordsPerPage = 50)
        {
            try
            {
                // Enforce maximum limit of 50 records per page
                bool exceededLimit = false;
                if (numOfRecordsPerPage > 50)
                {
                    exceededLimit = true;
                    numOfRecordsPerPage = 50;
                }

                // Prepare the request to retrieve entity metadata with attributes
                var request = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.Attributes,
                    LogicalName = entityLogicalName,
                    RetrieveAsIfPublished = true
                };
                // Execute the request
                var response = (RetrieveEntityResponse)await serviceClient.ExecuteAsync(request);
                // Get the entity metadata from the response
                var entityMetadata = response.EntityMetadata;
                if (entityMetadata == null)
                {
                    return $"No metadata found for entity: {entityLogicalName}";
                }

                if (entityMetadata.Attributes == null || entityMetadata.Attributes.Length == 0)
                {
                    return $"No attributes found for entity: {entityLogicalName}";
                }

                // Calculate pagination
                int totalRecords = entityMetadata.Attributes.Length;
                int skip = (pageNumber - 1) * numOfRecordsPerPage;
                
                if (skip >= totalRecords)
                {
                    return $"Page {pageNumber} is out of range. Total pages available: {Math.Ceiling((double)totalRecords / numOfRecordsPerPage)}";
                }

                var pagedAttributes = entityMetadata.Attributes
                    .Skip(skip)
                    .Take(numOfRecordsPerPage)
                    .ToArray();

                // Serialize the attributes to JSON for easier reading
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                string jsonAttributes = JsonSerializer.Serialize(pagedAttributes, options);

                // Add warning if limit was exceeded
                if (exceededLimit)
                {
                    jsonAttributes = "WARNING: Number of records per page was set to more than 50, which is not allowed. Returning only 50 records. Please do not set numOfRecordsPerPage to more than 50 in future calls.\n\n" + jsonAttributes;
                }

                // Check if more records are available
                bool hasMoreRecords = (skip + numOfRecordsPerPage) < totalRecords;
                if (hasMoreRecords)
                {
                    int nextPageNumber = pageNumber + 1;
                    jsonAttributes += $"\n\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber}";
                }

                return jsonAttributes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity attributes.");
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get relationships metadata information for an entity/table in a paginated fashion. IMPORTANT: Maximum 50 records per page. Do not exceed this limit.")]
        public async Task<string> GetRelationships(ServiceClient serviceClient,
            [Description("Entity/Table Logical Name")] string entityLogicalName,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Number of records to be returned per page. Maximum allowed is 50. Do not set this value higher than 50.")] int numOfRecordsPerPage = 50)
        {
            try
            {
                // Enforce maximum limit of 50 records per page
                bool exceededLimit = false;
                if (numOfRecordsPerPage > 50)
                {
                    exceededLimit = true;
                    numOfRecordsPerPage = 50;
                }

                // Prepare the request to retrieve entity metadata with relationships
                var request = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.Relationships,
                    LogicalName = entityLogicalName,
                    RetrieveAsIfPublished = true
                };
                // Execute the request
                var response = (RetrieveEntityResponse)await serviceClient.ExecuteAsync(request);
                // Get the entity metadata from the response
                var entityMetadata = response.EntityMetadata;
                if (entityMetadata == null)
                {
                    return $"No metadata found for entity: {entityLogicalName}";
                }

                // Combine all relationship types
                var allRelationships = new List<object>();
                
                if (entityMetadata.OneToManyRelationships != null)
                {
                    allRelationships.AddRange(entityMetadata.OneToManyRelationships.Select(r => new
                    {
                        Type = "OneToMany",
                        Relationship = r
                    }));
                }
                
                if (entityMetadata.ManyToOneRelationships != null)
                {
                    allRelationships.AddRange(entityMetadata.ManyToOneRelationships.Select(r => new
                    {
                        Type = "ManyToOne",
                        Relationship = r
                    }));
                }
                
                if (entityMetadata.ManyToManyRelationships != null)
                {
                    allRelationships.AddRange(entityMetadata.ManyToManyRelationships.Select(r => new
                    {
                        Type = "ManyToMany",
                        Relationship = r
                    }));
                }

                if (allRelationships.Count == 0)
                {
                    return $"No relationships found for entity: {entityLogicalName}";
                }

                // Calculate pagination
                int totalRecords = allRelationships.Count;
                int skip = (pageNumber - 1) * numOfRecordsPerPage;
                
                if (skip >= totalRecords)
                {
                    return $"Page {pageNumber} is out of range. Total pages available: {Math.Ceiling((double)totalRecords / numOfRecordsPerPage)}";
                }

                var pagedRelationships = allRelationships
                    .Skip(skip)
                    .Take(numOfRecordsPerPage)
                    .ToList();

                // Serialize the relationships to JSON for easier reading
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                string jsonRelationships = JsonSerializer.Serialize(pagedRelationships, options);

                // Add warning if limit was exceeded
                if (exceededLimit)
                {
                    jsonRelationships = "WARNING: Number of records per page was set to more than 50, which is not allowed. Returning only 50 records. Please do not set numOfRecordsPerPage to more than 50 in future calls.\n\n" + jsonRelationships;
                }

                // Check if more records are available
                bool hasMoreRecords = (skip + numOfRecordsPerPage) < totalRecords;
                if (hasMoreRecords)
                {
                    int nextPageNumber = pageNumber + 1;
                    jsonRelationships += $"\n\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber}";
                }

                return jsonRelationships;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity relationships.");
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get Optionset Values and labels for an Optionset/Picklist type field/column using entity/table logical name")]
        public async Task<string> GetOptionSetValuesForEntityField(ServiceClient serviceClient,
            [Description("Entity/Table Logical Name")] string entityLogicalName,
            [Description("Field/Column Logical Name")] string fieldLogicalName)
        {
            try
            {
                // Create the request to retrieve the attribute metadata
                RetrieveAttributeRequest retrieveAttributeRequest = new RetrieveAttributeRequest
                {
                    EntityLogicalName = entityLogicalName,
                    LogicalName = fieldLogicalName,
                    RetrieveAsIfPublished = true
                };

                // Execute the request
                RetrieveAttributeResponse retrieveAttributeResponse =
                    (RetrieveAttributeResponse)serviceClient.Execute(retrieveAttributeRequest);
                // Get the attribute metadata
                var attributeMetadata = retrieveAttributeResponse.AttributeMetadata;
                if (attributeMetadata == null)
                {
                    return $"No metadata found for field: {fieldLogicalName} in entity: {entityLogicalName}";
                }
                // Check if the attribute is of type Picklist (OptionSet)
                if (attributeMetadata.AttributeType != AttributeTypeCode.Picklist &&
                    attributeMetadata.AttributeType != AttributeTypeCode.State &&
                    attributeMetadata.AttributeType != AttributeTypeCode.Status)
                {
                    return $"The field: {fieldLogicalName} in entity: {entityLogicalName} is not of type Optionset/Picklist.";
                }
                // Cast to PicklistAttributeMetadata to access OptionSet
                var picklistMetadata = (PicklistAttributeMetadata)attributeMetadata;
                var optionSet = picklistMetadata.OptionSet;
                if (optionSet == null || optionSet.Options == null || optionSet.Options.Count == 0)
                {
                    return $"No options found for the Optionset/Picklist field: {fieldLogicalName} in entity: {entityLogicalName}";
                }

                // Serialize the options to JSON for easier reading
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                string jsonOptions = JsonSerializer.Serialize(optionSet.Options, options);
                return jsonOptions;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving OptionSet values.");
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Find logical name of Global Optionset using keyword")]
        public async Task<string> FindGlobalOptionSet(ServiceClient serviceClient,
            [Description("Keyword to search for Global Optionset")] string keyword)
        {
            try
            {
                string result = string.Empty;
                // Get all global option sets metadata
                var allOptionSetsRequest = new RetrieveAllOptionSetsRequest
                {
                    RetrieveAsIfPublished = true
                };
                var allOptionSetsResponse =
                    (RetrieveAllOptionSetsResponse)await serviceClient.ExecuteAsync(allOptionSetsRequest);
                var matchingOptionSets = allOptionSetsResponse.OptionSetMetadata
                    .Where(o =>
                        (!string.IsNullOrEmpty(o.Name) &&
                         o.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                     || (o.DisplayName?.UserLocalizedLabel != null &&
                         o.DisplayName.UserLocalizedLabel.Label.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (matchingOptionSets.Count == 0)
                {
                    return "No matching global option sets found.";
                }
                result += string.Join("\n", $"Found {matchingOptionSets.Count} matching global option sets");
                if (matchingOptionSets.Count > 1)
                {
                    result += string.Join("\n", "Since there are multiple results, the user should be prompted to pick a logical name from the results to proceed with subsequent action.");
                }
                foreach (var optionSet in matchingOptionSets)
                {
                    var displayName = optionSet.DisplayName?.UserLocalizedLabel?.Label ?? "N/A";
                    result += string.Join("\n", $"Logical Name: {optionSet.Name}, Display Name: {displayName}");
                }
                //result += string.Join("\n", JsonSerializer.Serialize(matchingOptionSets));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching global option set metadata.");
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get Options (values,labels) for a global optionset using logical name")]
        public async Task<string> GetGlobalOptionSetValues(ServiceClient serviceClient,
            [Description("Global Optionset Logical Name")] string globalOptionSetLogicalName)
        {
            try
            {
                // Create the request to retrieve the global option set metadata
                RetrieveOptionSetRequest retrieveOptionSetRequest = new RetrieveOptionSetRequest
                {
                    Name = globalOptionSetLogicalName,
                    RetrieveAsIfPublished = true
                };
                // Execute the request
                RetrieveOptionSetResponse retrieveOptionSetResponse =
                    (RetrieveOptionSetResponse)await serviceClient.ExecuteAsync(retrieveOptionSetRequest);

                // Cast the retrieved option set metadata to an OptionSetMetadata
                OptionSetMetadata optionSetMetadata =
                    (OptionSetMetadata)retrieveOptionSetResponse.OptionSetMetadata;

                if (optionSetMetadata == null)
                {
                    return $"No metadata found for global option set: {globalOptionSetLogicalName}";
                }



                if (optionSetMetadata.Options == null || optionSetMetadata.Options.Count == 0)
                {
                    return $"No options found for the global option set: {globalOptionSetLogicalName}";
                }

                // Serialize the options to JSON for easier reading
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                string jsonOptions = JsonSerializer.Serialize(optionSetMetadata.Options, options);
                return jsonOptions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving global option set values.");
                return $"Error: {ex.Message}";
            }
        }
        [McpServerTool, Description("List all the Global Optionsets in the environment")]
        public async Task<string> ListAllGlobalOptionSets(ServiceClient serviceClient)
        {
            try
            {
                // Get all global option sets metadata
                var allOptionSetsRequest = new RetrieveAllOptionSetsRequest
                {
                    RetrieveAsIfPublished = true
                };
                var allOptionSetsResponse =
                    (RetrieveAllOptionSetsResponse)await serviceClient.ExecuteAsync(allOptionSetsRequest);
                var optionSets = allOptionSetsResponse.OptionSetMetadata;
                if (optionSets == null || optionSets.Length == 0)
                {
                    return "No global option sets found in the environment.";
                }
                // Serialize the list of global option sets to JSON for easier reading
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                string jsonOptionSets = JsonSerializer.Serialize(optionSets.Select(o => new
                {
                    o.Name,
                    DisplayName = o.DisplayName?.UserLocalizedLabel?.Label ?? "N/A",
                    Description = o.Description?.UserLocalizedLabel?.Label ?? "N/A"
                }), options);
                return jsonOptionSets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all global option sets.");
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get Entity/Table privileges/permissions using logical name of the entity/table")]
        public async Task<string> GetEntityPrivileges(ServiceClient serviceClient,
            [Description("Entity/Table Logical Name")] string entityLogicalName)
        {
            try
            {
                // Prepare the request to retrieve entity metadata with privileges
                var request = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.Privileges,
                    LogicalName = entityLogicalName,
                    RetrieveAsIfPublished = true
                };
                // Execute the request
                var response = (RetrieveEntityResponse)await serviceClient.ExecuteAsync(request);
                // Get the entity metadata from the response
                var entityMetadata = response.EntityMetadata;
                if (entityMetadata == null)
                {
                    return $"No metadata found for entity: {entityLogicalName}";
                }
                if (entityMetadata.Privileges == null || entityMetadata.Privileges.Length == 0)
                {
                    return $"No privileges found for entity: {entityLogicalName}";
                }
                // Serialize the privileges to JSON for easier reading
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };
                string jsonPrivileges = JsonSerializer.Serialize(entityMetadata.Privileges, options);
                return jsonPrivileges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity privileges.");
                return $"Error: {ex.Message}";
            }
        }


    }
}
