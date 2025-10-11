using DataverseDevToolsMcpServer.Helpers;
using DataverseDevToolsMcpServer.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
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
    public class SecurityManagementTools
    {
        private readonly ILogger<SecurityManagementTools> _logger;
        public SecurityManagementTools(ILogger<SecurityManagementTools> logger)
        {
            _logger = logger;
        }


        [McpServerTool, Description("Get all the privileges/permissions a security role has on an entity using the security role id (Guid)")]
        public async Task<string> GetEntityPrivByRoleId(ServiceClient serviceClient,
           [Description("Role Id (Guid) of the security role")] string roleId,
           [Description("Entity/Table logical name")] string entityLogicalName)
        {
            try
            {
                string result = string.Empty;

                if (!Guid.TryParse(roleId, out Guid roleGuid))
                {
                    return $"Invalid GUID format for Role Id: {roleId}";
                }

                // Get entity privileges (all CRUD + others)
                var retrieveEntity = new RetrieveEntityRequest
                {
                    LogicalName = entityLogicalName,
                    EntityFilters = EntityFilters.Privileges
                };
                var entityResponse = (RetrieveEntityResponse)await serviceClient.ExecuteAsync(retrieveEntity);
                var entityPrivileges = entityResponse.EntityMetadata.Privileges;

                // Get all roleprivileges for this role
                var rolePrivQuery = new QueryExpression("roleprivileges")
                {
                    ColumnSet = new ColumnSet("privilegeid", "privilegedepthmask"),
                    Criteria = new FilterExpression
                    {
                        Conditions = { new ConditionExpression("roleid", ConditionOperator.Equal, roleId) }
                    }
                };
                //var rolePrivileges = await serviceClient.RetrieveMultipleAsync(rolePrivQuery);
                var rolePrivileges = await DataManagementHelper.RetrieveAllRecordsAsync(serviceClient, rolePrivQuery);
                if (rolePrivileges.Entities.Count == 0)
                {
                    return $"No privileges found for Role Id: {roleId}";
                }

                // Join entity privileges with role privileges to get the depth mask for each privilege
                var rolePrivilegesForEntity = from ep in entityPrivileges
                                              join rp in rolePrivileges.Entities
                                              on ep.PrivilegeId equals rp.GetAttributeValue<Guid>("privilegeid")
                                              select new
                                              {
                                                  PrivilegeName = ep.Name,
                                                  PrivilegeType = ep.PrivilegeType,
                                                  DepthMask = rp.GetAttributeValue<int>("privilegedepthmask"),
                                                  PrivilegeDepthInfo = SecurityManagementHelper.PrivilegeDepthToString(rp.GetAttributeValue<int>("privilegedepthmask"))
                                              };
                result += string.Join(Environment.NewLine, "The role has the following privileges:");
                result += string.Join(Environment.NewLine, JsonSerializer.Serialize(rolePrivilegesForEntity));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving entity privileges for role {RoleId} on entity {EntityLogicalName}", roleId, entityLogicalName);
                return $"Error: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get all the privileges/permissions a security role has using the security role id (guid)")]
        public async Task<string> GetAllPrivByRoleId(ServiceClient serviceClient,
           [Description("Role Id (Guid) of the security role")] string roleId)
        {
            try
            {
                string result = string.Empty;
                if (!Guid.TryParse(roleId, out Guid roleGuid))
                {
                    return $"Invalid GUID format for Role Id: {roleId}";
                }
                // Get all roleprivileges for this role
                var rolePrivQuery = new QueryExpression("roleprivileges")
                {
                    ColumnSet = new ColumnSet("privilegeid", "privilegedepthmask"),
                    Criteria = new FilterExpression
                    {
                        Conditions = { new ConditionExpression("roleid", ConditionOperator.Equal, roleId) }
                    }
                };
                //var rolePrivileges = await serviceClient.RetrieveMultipleAsync(rolePrivQuery);
                var rolePrivileges = await DataManagementHelper.RetrieveAllRecordsAsync(serviceClient, rolePrivQuery);

                if (rolePrivileges.Entities.Count == 0)
                {
                    return $"No privileges found for Role Id: {roleId}";
                }

                var rolePrivs = rolePrivileges.Entities;
                // Retrieve the related privileges (bulk query)
                var privilegeIds = rolePrivs.Select(rp => rp.GetAttributeValue<Guid>("privilegeid")).Distinct().ToList();

                var privQuery = new QueryExpression("privilege")
                {
                    ColumnSet = new ColumnSet("name", "accessright"),
                    Criteria = new FilterExpression
                    {
                        Conditions = { new ConditionExpression("privilegeid", ConditionOperator.In, privilegeIds) }
                    }
                };

                //var privileges = serviceClient.RetrieveMultiple(privQuery).Entities.ToDictionary(p => p.Id);
                var privilegesCollection = await DataManagementHelper.RetrieveAllRecordsAsync(serviceClient, privQuery);
                var privileges = privilegesCollection.Entities.ToDictionary(p => p.Id);

                // Inside your method, build the list:
                List <PrivilegeDetail> privilegeDetails = new();

                foreach (var rp in rolePrivs)
                {
                    Guid privilegeId = rp.GetAttributeValue<Guid>("privilegeid");
                    int depth = rp.GetAttributeValue<int>("privilegedepthmask");

                    if (privileges.TryGetValue(privilegeId, out Entity priv))
                    {
                        string name = priv.GetAttributeValue<string>("name");
                        int access = priv.GetAttributeValue<int>("accessright");
                        string accessRightStr = SecurityManagementHelper.AccessRightToString(access);
                        string depthStr = SecurityManagementHelper.PrivilegeDepthToString(depth);

                        privilegeDetails.Add(new PrivilegeDetail
                        {
                            name = name,
                            access = access,
                            accessRightStr = accessRightStr,
                            depthStr = depthStr                  
                        });
                    }
                }
  
                result += string.Join(Environment.NewLine, "The role has the following privileges:");
                result += string.Join(Environment.NewLine, JsonSerializer.Serialize(privilegeDetails));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all privileges for role {RoleId}", roleId);
                return $"Error: {ex.Message}";
            }

        }

        [McpServerTool, Description("List all the security roles having a specific privilege on an entity/table using privilege id (guid)")]
        public async Task<string> ListRolesByPrivId(ServiceClient serviceClient,
           [Description("Privilege Id (Guid) of the entity privilege")] string privilegeId)
        {
            try
            {
                string result = string.Empty;
                if (!Guid.TryParse(privilegeId, out Guid privGuid))
                {
                    return $"Invalid GUID format for Privilege Id: {privilegeId}";
                }

                //query the security role id & name of the roles having this privilege. filter the role for Business unit where parentbusinessunit is null (top level business unit)
                string fetchXml = $@"<fetch>
                                      <entity name=""roleprivileges"">
                                        <attribute name=""privilegedepthmask"" />
                                        <attribute name=""privilegeid"" />
                                        <attribute name=""roleid"" />
                                        <attribute name=""roleprivilegeid"" />
                                        <filter>
                                          <condition attribute=""privilegeid"" operator=""eq"" value=""886b280c-6396-4d56-a0a3-2c1b0a50ceb0"" />
                                        </filter>
                                        <link-entity name=""role"" from=""roleid"" to=""roleid"" link-type=""inner"" alias=""rol"">
                                          <attribute name=""name"" />
                                          <attribute name=""parentrootroleid"" />
                                          <attribute name=""roleid"" />
                                          <filter>
                                            <condition attribute=""parentroleid"" operator=""null"" />
                                          </filter>
                                        </link-entity>
                                        <link-entity name=""privilege"" from=""privilegeid"" to=""privilegeid"" link-type=""inner"" alias=""prv"">
                                          <attribute name=""accessright"" />
                                        </link-entity>
                                      </entity>
                                    </fetch>";
                FetchExpression fetchExpression = new FetchExpression(fetchXml);
                var fetchResult = await serviceClient.RetrieveMultipleAsync(fetchExpression);
                
                if(fetchResult==null || fetchResult.Entities==null || fetchResult.Entities.Count == 0)
                {
                    return $"No roles found with privilege Id: {privilegeId}";
                }
                
                var rolesWithPrivilege = fetchResult.Entities.Select(e => new
                {
                    roleId = e.GetAttributeValue<Guid>("roleid"),
                    roleName = (string)e.GetAttributeValue<AliasedValue>("rol.name")?.Value,
                    privilegeDepthMask = e.GetAttributeValue<int>("privilegedepthmask"),
                    privilegeDepthInfo = SecurityManagementHelper.PrivilegeDepthToString(e.GetAttributeValue<int>("privilegedepthmask")),
                    accessRight = (int)e.GetAttributeValue<AliasedValue>("prv.accessright")?.Value,
                    accessRightStr = SecurityManagementHelper.AccessRightToString((int)(e.GetAttributeValue<AliasedValue>("prv.accessright")?.Value ?? 0))
                });
                result += string.Join(Environment.NewLine, "The following roles have this privilege:");
                result += string.Join(Environment.NewLine, JsonSerializer.Serialize(rolesWithPrivilege));
                

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing security roles for privilege {PrivilegeId}", privilegeId);
                return $"Error: {ex.Message}";

            }
        }

    }
}
