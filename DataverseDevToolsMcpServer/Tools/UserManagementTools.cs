using DataverseDevToolsMcpServer.Models;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DataverseDevToolsMcpServer.Tools
{
    [McpServerToolType]
    public class UserManagementTools
    {
        private readonly ILogger<UserManagementTools> _logger;
        public UserManagementTools(ILogger<UserManagementTools> logger)
        {
            this._logger = logger;
        }

        [McpServerTool, Description("Get details of the current logged in user")]
        public static async Task<string> GetCurrentUser(ServiceClient serviceClient)
        {
            var response = (WhoAmIResponse)await serviceClient.ExecuteAsync(new WhoAmIRequest());
            Guid userId = response.UserId;
            Entity userEntity = await serviceClient.RetrieveAsync("systemuser", userId, new ColumnSet("fullname", "domainname", "businessunitid"));

            User userObj = new User
            {
                userId = userEntity.Id,
                fullName = userEntity.GetAttributeValue<string>("fullname"),
                domainName = userEntity.GetAttributeValue<string>("domainname"),
                businessUnitId = (Guid)(userEntity.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Id),
                businessUnitName = userEntity.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Name
            };
            string result = JsonSerializer.Serialize(userObj);
            return $"Current logged in user is {result}";
        }

        [McpServerTool, Description("Get user details by name")]
        public async Task<string> GetUserByName(ServiceClient serviceClient,
            [Description("FullName of the user")] string userName)
        {
            var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("systemuser")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("fullname", "domainname", "businessunitid"),
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                {
                    Conditions =
                    {
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("fullname", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, userName)
                    }
                }
            };
            var users = await serviceClient.RetrieveMultipleAsync(query);
            if (users.Entities.Count == 0)
            {
                return $"No user found with name {userName}. Do you want to search for User with the keyword {userName}?";
            }
            var user = users.Entities.First();

            User userObj = new User
            {
                userId = user.Id,
                fullName = user.GetAttributeValue<string>("fullname"),
                domainName = user.GetAttributeValue<string>("domainname"),
                businessUnitId = (Guid)(user.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Id),
                businessUnitName = user.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Name
            };

            string result = JsonSerializer.Serialize(userObj);

            return $"User Details:\n {result}";
        }

        [McpServerTool, Description("Get user details by user Id (Guid)")]
        public async Task<string> GetUserById(ServiceClient serviceClient,
            [Description("User Id (Guid) of the user")] string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format: {userId}";
            }
            var user = await serviceClient.RetrieveAsync("systemuser", userGuid, new Microsoft.Xrm.Sdk.Query.ColumnSet("fullname", "domainname", "businessunitid"));
            if (user == null)
            {
                return $"No user found with Id {userId}";
            }
            User userObj = new User
            {
                userId = user.Id,
                fullName = user.GetAttributeValue<string>("fullname"),
                domainName = user.GetAttributeValue<string>("domainname"),
                businessUnitId = (Guid)(user.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Id),
                businessUnitName = user.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Name
            };
            string result = JsonSerializer.Serialize(userObj);
            return $"User Details:\n {result}";
        }

        [McpServerTool, Description("Search for users where fullname contains the keyword")]
        public async Task<string> SearchUsersByKeyword(ServiceClient serviceClient,
            [Description("Keyword to search in fullname of users")] string keyword,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Pagination Cookie")] string pagingCookie = null,
            [Description("Number of records to be returned per page")] int numOfRecordsPerPage = 10)
        {
            string result = string.Empty;
            try
            {
                var query = new QueryExpression("systemuser")
                {
                    ColumnSet = new ColumnSet("fullname", "domainname", "businessunitid"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                    {
                        new ConditionExpression("fullname", ConditionOperator.Like, $"%{keyword}%")
                    }
                    }
                };

                query.PageInfo = new PagingInfo
                {
                    Count = numOfRecordsPerPage,
                    PageNumber = pageNumber,
                    PagingCookie = pagingCookie
                };

                var users = await serviceClient.RetrieveMultipleAsync(query);
                if (users.Entities.Count == 0)
                {
                    return $"No user found with keyword {keyword}";
                }
                List<User> userList = new List<User>();
                foreach (var user in users.Entities)
                {
                    User userObj = new User
                    {
                        userId = user.Id,
                        fullName = user.GetAttributeValue<string>("fullname"),
                        domainName = user.GetAttributeValue<string>("domainname"),
                        businessUnitId = (Guid)(user.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Id),
                        businessUnitName = user.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Name
                    };
                    userList.Add(userObj);
                }
                result += string.Join("\n", JsonSerializer.Serialize(userList));
                result += string.Join("\n", "Since there are multiple results, the user should be prompted to pick a User Id from the results to proceed with subsequent action.");

                if (users.MoreRecords)
                {
                    var nextPageNumber = pageNumber + 1;
                    var nextPagingCookie = users.PagingCookie;
                    result += $"\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber} and Paging Cookie: {nextPagingCookie}";
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching users by keyword.");
                return $"An error occurred: {ex.Message}";

            }
        }

        [McpServerTool, Description("Get the queues where the user is a member of")]
        public async Task<string> GetUserQueues(ServiceClient serviceClient,
            [Description("User Id (Guid) of the user")] string userId,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Pagination Cookie")] string pagingCookie = null,
            [Description("Number of records to be returned per page")] int numOfRecordsPerPage = 10)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format: {userId}";
            }

            string result = string.Empty;
            try
            {
                var query = new QueryExpression("queue")
                {
                    ColumnSet = new ColumnSet("name", "queueid", "description"),
                    LinkEntities =
                {
                    new LinkEntity
                    {
                        LinkFromEntityName = "queue",
                        LinkFromAttributeName = "queueid",
                        LinkToEntityName = "queuemembership",
                        LinkToAttributeName = "queueid",
                        JoinOperator = JoinOperator.Inner,
                        LinkEntities =
                        {
                            new LinkEntity
                            {
                                LinkFromEntityName = "queuemembership",
                                LinkFromAttributeName = "systemuserid",
                                LinkToEntityName = "systemuser",
                                LinkToAttributeName = "systemuserid",
                                JoinOperator = JoinOperator.Inner,
                                LinkCriteria = new FilterExpression
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("systemuserid", ConditionOperator.Equal, userGuid)
                                    }
                                }
                            }
                        }
                    }
                }
                };

                query.PageInfo = new PagingInfo
                {
                    Count = numOfRecordsPerPage,
                    PageNumber = pageNumber,
                    PagingCookie = pagingCookie
                };

                var queues = await serviceClient.RetrieveMultipleAsync(query);
                if (queues.Entities.Count == 0)
                {
                    return $"No queues found for user with Id {userId}";
                }
                List<Queue> queueList = new List<Queue>();
                foreach (var queue in queues.Entities)
                {
                    Queue queueObj = new Queue
                    {
                        queueId = queue.Id,
                        name = queue.GetAttributeValue<string>("name"),
                        description = queue.GetAttributeValue<string>("description")
                    };
                    queueList.Add(queueObj);
                }

                result += string.Join("\n", JsonSerializer.Serialize(queueList));

                if (queues.MoreRecords)
                {
                    var nextPageNumber = pageNumber + 1;
                    var nextPagingCookie = queues.PagingCookie;
                    result += $"\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber} and Paging Cookie: {nextPagingCookie}";
                }
                return $"Queues for User Id {userId}:\n {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching user queues.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get the teams where the user is a member of")]
        public async Task<string> GetUserTeams(ServiceClient serviceClient,
            [Description("User Id (Guid) of the user")] string userId,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Pagination Cookie")] string pagingCookie = null,
            [Description("Number of records to be returned per page")] int numOfRecordsPerPage = 10)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format: {userId}";
            }
            string result = string.Empty;
            try
            {
                var query = new QueryExpression("team")
                {
                    ColumnSet = new ColumnSet("name", "teamid"),
                    LinkEntities =
                {
                    new LinkEntity
                    {
                        LinkFromEntityName = "team",
                        LinkFromAttributeName = "teamid",
                        LinkToEntityName = "teammembership",
                        LinkToAttributeName = "teamid",
                        JoinOperator = JoinOperator.Inner,
                        LinkEntities =
                        {
                            new LinkEntity
                            {
                                LinkFromEntityName = "teammembership",
                                LinkFromAttributeName = "systemuserid",
                                LinkToEntityName = "systemuser",
                                LinkToAttributeName = "systemuserid",
                                JoinOperator = JoinOperator.Inner,
                                LinkCriteria = new FilterExpression
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("systemuserid", ConditionOperator.Equal, userGuid)
                                    }
                                }
                            }
                        }
                    }
                }
                };
                query.PageInfo = new PagingInfo
                {
                    Count = numOfRecordsPerPage,
                    PageNumber = pageNumber,
                    PagingCookie = pagingCookie
                };
                var teams = await serviceClient.RetrieveMultipleAsync(query);
                if (teams.Entities.Count == 0)
                {
                    return $"No teams found for user with Id {userId}";
                }
                List<Team> teamList = new List<Team>();
                foreach (var team in teams.Entities)
                {
                    Team teamObj = new Team
                    {
                        teamId = team.Id,
                        name = team.GetAttributeValue<string>("name")
                    };
                    teamList.Add(teamObj);
                }
                result += string.Join("\n", JsonSerializer.Serialize(teamList));
                if (teams.MoreRecords)
                {
                    var nextPageNumber = pageNumber + 1;
                    var nextPagingCookie = teams.PagingCookie;
                    result += $"\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber} and Paging Cookie: {nextPagingCookie}";

                }
                return $"Teams for User Id {userId}:\n {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching user teams.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get the security roles assigned to a user by user id (Guid)")]
        public async Task<string> GetUserRoles(ServiceClient serviceClient,
            [Description("User Id (Guid) of the user")] string userId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format: {userId}";
            }
            try
            {
                var query = new QueryExpression("role")
                {
                    ColumnSet = new ColumnSet("name", "roleid", "businessunitid"),
                    LinkEntities =
                {
                    new LinkEntity
                    {
                        LinkFromEntityName = "role",
                        LinkFromAttributeName = "roleid",
                        LinkToEntityName = "systemuserroles",
                        LinkToAttributeName = "roleid",
                        JoinOperator = JoinOperator.Inner,
                        LinkEntities =
                        {
                            new LinkEntity
                            {
                                LinkFromEntityName = "systemuserroles",
                                LinkFromAttributeName = "systemuserid",
                                LinkToEntityName = "systemuser",
                                LinkToAttributeName = "systemuserid",
                                JoinOperator = JoinOperator.Inner,
                                LinkCriteria = new FilterExpression
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("systemuserid", ConditionOperator.Equal, userGuid)
                                    }
                                }
                            }
                        }
                    }
                }
                };
                var roles = await serviceClient.RetrieveMultipleAsync(query);
                if (roles.Entities.Count == 0)
                {
                    return $"No security roles found for user with Id {userId}";
                }
                List<Role> roleList = new List<Role>();
                foreach (var role in roles.Entities)
                {
                    Role roleObj = new Role
                    {
                        roleId = role.Id,
                        name = role.GetAttributeValue<string>("name"),
                        businessUnitId = (Guid)(role.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Id),
                        businessUnitName = role.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Name
                    };
                    roleList.Add(roleObj);
                }
                string result = JsonSerializer.Serialize(roleList);
                return $"Security Roles for User Id {userId}:\n {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching user security roles.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get the Business Unit details by name")]
        public async Task<string> GetBUByName(ServiceClient serviceClient,
            [Description("Name of the Business Unit")] string businessUnitName)
        {
            var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("businessunit")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("name", "businessunitid", "parentbusinessunitid", "description"),
                Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                {
                    Conditions =
                    {
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("name", ConditionOperator.Equal, businessUnitName)
                    }
                }
            };
            var businessUnits = await serviceClient.RetrieveMultipleAsync(query);
            if (businessUnits.Entities.Count == 0)
            {
                return $"No Business Unit found with name {businessUnitName}. Do you want to search for Business Unit with the keyword {businessUnitName}?";
            }
            var businessUnit = businessUnits.Entities.First();
            BusinessUnit buObj = new BusinessUnit
            {
                businessUnitId = businessUnit.Id,
                name = businessUnit.GetAttributeValue<string>("name"),
                parentBusinessUnitId = (Guid?)(businessUnit.GetAttributeValue<EntityReference>("parentbusinessunitid")?.Id),
                parentBusinessUnitName = businessUnit.GetAttributeValue<EntityReference>("parentbusinessunitid")?.Name,
                description = businessUnit.GetAttributeValue<string>("description")
            };
            string result = JsonSerializer.Serialize(buObj);
            return $"Business Unit Details:\n {result}";
        }

        [McpServerTool, Description("Search Business Unit details by keyword")]
        public async Task<string> SearchBUByKeyword(ServiceClient serviceClient,
            [Description("Keyword to search in name of Business Units")] string keyword,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Pagination Cookie")] string pagingCookie = null,
            [Description("Number of records to be returned per page")] int numOfRecordsPerPage = 10)
        {
            string result = string.Empty;
            try
            {
                var query = new QueryExpression("businessunit")
                {
                    ColumnSet = new ColumnSet("name", "businessunitid", "parentbusinessunitid", "description"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Like, $"%{keyword}%")
                    }
                    }
                };
                query.PageInfo = new PagingInfo
                {
                    Count = numOfRecordsPerPage,
                    PageNumber = pageNumber,
                    PagingCookie = pagingCookie
                };
                var businessUnits = await serviceClient.RetrieveMultipleAsync(query);
                if (businessUnits.Entities.Count == 0)
                {
                    return $"No Business Unit found with keyword {keyword}";
                }
                List<BusinessUnit> buList = new List<BusinessUnit>();
                foreach (var bu in businessUnits.Entities)
                {
                    BusinessUnit buObj = new BusinessUnit
                    {
                        businessUnitId = bu.Id,
                        name = bu.GetAttributeValue<string>("name"),
                        parentBusinessUnitId = (Guid?)(bu.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("parentbusinessunitid")?.Id),
                        parentBusinessUnitName = bu.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("parentbusinessunitid")?.Name,
                        description = bu.GetAttributeValue<string>("description")
                    };
                    buList.Add(buObj);
                }
                result += string.Join("\n", JsonSerializer.Serialize(buList));
                result += string.Join("\n", "Since there are multiple results, the user should be prompted to pick a Business Unit Id from the results to proceed with subsequent action.");
                if (businessUnits.MoreRecords)
                {
                    var nextPageNumber = pageNumber + 1;
                    var nextPagingCookie = businessUnits.PagingCookie;
                    result += $"\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber} and Paging Cookie: {nextPagingCookie}";
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching business units by keyword.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get the Root Business Unit details")]
        public async Task<string> GetRootBU(ServiceClient serviceClient)
        {
            try
            {
                var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("businessunit")
                {
                    ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("name", "businessunitid", "parentbusinessunitid", "description"),
                    Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                    {
                        Conditions =
                    {
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("parentbusinessunitid", ConditionOperator.Null)
                    }
                    }
                };
                var businessUnits = await serviceClient.RetrieveMultipleAsync(query);
                if (businessUnits.Entities.Count == 0)
                {
                    return $"No Root Business Unit found.";
                }
                var businessUnit = businessUnits.Entities.First();
                BusinessUnit buObj = new BusinessUnit
                {
                    businessUnitId = businessUnit.Id,
                    name = businessUnit.GetAttributeValue<string>("name"),
                    parentBusinessUnitId = (Guid?)(businessUnit.GetAttributeValue<EntityReference>("parentbusinessunitid")?.Id),
                    parentBusinessUnitName = businessUnit.GetAttributeValue<EntityReference>("parentbusinessunitid")?.Name,
                    description = businessUnit.GetAttributeValue<string>("description")

                };
                string result = JsonSerializer.Serialize(buObj);
                return $"Root Business Unit Details:\n {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching root business unit.");
                return $"An error occurred: {ex.Message}";

            }
        }

        [McpServerTool, Description(
        @"Get security role details by name. 
        If a business unit id is not available, get the role details for Root Business Unit. 
        GetRootBusinessUnit() MCP Tool can be used to get Root Business Unit details")]
        public async Task<string> GetRoleByNameAndBU(ServiceClient serviceClient,
            [Description("Name of the Security Role")] string roleName,
            [Description("Business Unit Id (Guid). Default value is the Root Business Unit ID (Guid)")] string businessUnitId)
        {
            try
            {
                var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("role")
                {
                    ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("name", "roleid", "businessunitid"),
                    Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                    {
                        Conditions =
                    {
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("name", ConditionOperator.Equal, roleName),
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("businessunitid", ConditionOperator.Equal, new Guid(businessUnitId))
                    }
                    }
                };
                var roles = await serviceClient.RetrieveMultipleAsync(query);
                if (roles.Entities.Count == 0)
                {
                    return $"No Security Role found with name {roleName}. Do you want to search for Security Role with the keyword {roleName}?";
                }
                var role = roles.Entities.First();
                Role roleObj = new Role
                {
                    roleId = role.Id,
                    name = role.GetAttributeValue<string>("name"),
                    businessUnitId = (Guid)(role.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Id),
                    businessUnitName = role.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Name
                };
                string result = JsonSerializer.Serialize(roleObj);
                return $"Security Role Details:\n {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching security role by name.");
                return $"An error occurred: {ex.Message}";

            }
        }

        [McpServerTool, Description(
        @"Search security role details by keyword. 
        If a business unit id is not available, get the role details for Root Business Unit. 
        GetRootBusinessUnit() MCP Tool can be used to get Root Business Unit details")]
        public async Task<string> SearchRolesByKeywordAndBU(ServiceClient serviceClient,
            [Description("Keyword to search in name of Security Roles")] string keyword,
            [Description("Business Unit Id (Guid). Default value is the Root Business Unit ID (Guid)")] string businessUnitId,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Pagination Cookie")] string pagingCookie = null,
            [Description("Number of records to be returned per page")] int numOfRecordsPerPage = 10)
        {
            string result = string.Empty;
            try
            {
                var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("role")
                {
                    ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("name", "roleid", "businessunitid"),
                    Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
                    {
                        Conditions =
                    {
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("name", ConditionOperator.Like, $"%{keyword}%"),
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("businessunitid", ConditionOperator.Equal, new Guid(businessUnitId))
                    }
                    }
                };
                query.PageInfo = new PagingInfo
                {
                    Count = numOfRecordsPerPage,
                    PageNumber = pageNumber,
                    PagingCookie = pagingCookie
                };
                var roles = await serviceClient.RetrieveMultipleAsync(query);

                if (roles.Entities.Count == 0)
                {
                    return $"No Security Role found with keyword {keyword}";
                }
                List<Role> roleList = new List<Role>();
                foreach (var role in roles.Entities)
                {

                    Role roleObj = new Role
                    {
                        roleId = role.Id,
                        name = role.GetAttributeValue<string>("name"),
                        businessUnitId = (Guid)(role.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Id),
                        businessUnitName = role.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Name
                    };
                    roleList.Add(roleObj);
                }
                result += string.Join("\n", JsonSerializer.Serialize(roleList));
                result += string.Join("\n", "Since there are multiple results, the user should be prompted to pick a Role Id from the results to proceed with subsequent action.");
                if (roles.MoreRecords)
                {
                    var nextPageNumber = pageNumber + 1;
                    var nextPagingCookie = roles.PagingCookie;
                    result += $"\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber} and Paging Cookie: {nextPagingCookie}";
                }
                return result;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching security roles by keyword.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description(@"Assign security role to user based on the user's business unit")]
        public async Task<string> AssignRoleToUser(ServiceClient serviceClient,
            [Description("Security Role Id based on the business unit of the user")] string roleId,
            [Description("User Id (Guid)")] string userId)
        {
            if (!Guid.TryParse(roleId, out Guid roleGuid))
            {
                return $"Invalid GUID format for Role Id: {roleId}";
            }
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format for User Id: {userId}";
            }
            try
            {
                var associateRequest = new AssociateRequest
                {
                    Target = new EntityReference("systemuser", userGuid),
                    RelatedEntities = new EntityReferenceCollection
                    {
                        new EntityReference("role", roleGuid)
                    },
                    Relationship = new Relationship("systemuserroles_association")
                };
                await serviceClient.ExecuteAsync(associateRequest);
                return $"Security Role with Id {roleId} has been assigned to User with Id {userId} successfully.";

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while assigning security role to user.");
                return $"An error occurred: {ex.Message}";
            }


        }

        [McpServerTool, Description(@"Remove security role from a user based on the user's business unit")]
        public async Task<string> RemoveRoleFromUser(ServiceClient serviceClient,
            [Description("Security Role Id based on the business unit of the user")] string roleId,
            [Description("User Id (Guid)")] string userId)
        {
            if (!Guid.TryParse(roleId, out Guid roleGuid))
            {
                return $"Invalid GUID format for Role Id: {roleId}";
            }
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format for User Id: {userId}";
            }
            try
            {
                var disassociateRequest = new DisassociateRequest
                {
                    Target = new EntityReference("systemuser", userGuid),
                    RelatedEntities = new EntityReferenceCollection
                    {
                        new EntityReference("role", roleGuid)
                    },
                    Relationship = new Relationship("systemuserroles_association")
                };
                await serviceClient.ExecuteAsync(disassociateRequest);
                return $"Security Role with Id {roleId} has been removed from User with Id {userId} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing security role from user.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Change Business Unit of the user")]
        public async Task<string> ChangeUserBU(ServiceClient serviceClient,
            [Description("User Id (Guid) of the user")] string userId,
            [Description("Business Unit Id (Guid) to which the user should be moved")] string businessUnitId)
        {
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format for User Id: {userId}";
            }
            if (!Guid.TryParse(businessUnitId, out Guid businessUnitGuid))
            {
                return $"Invalid GUID format for Business Unit Id: {businessUnitId}";
            }
            try
            {
                var userEntity = new Entity("systemuser", userGuid);
                userEntity["businessunitid"] = new EntityReference("businessunit", businessUnitGuid);

                var updateRequest = new UpdateRequest
                {
                    Target = userEntity
                };
                await serviceClient.ExecuteAsync(updateRequest);
                return $"User with Id {userId} has been moved to Business Unit with Id {businessUnitId} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing user's business unit.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get the queue details by name")]
        public async Task<string> GetQueueByName(ServiceClient serviceClient,
            [Description("Name of the Queue")] string queueName)
        {
            try
            {
                var query = new QueryExpression("queue")
                {
                    ColumnSet = new ColumnSet("name", "queueid", "description"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, queueName)
                    }
                    }
                };
                var queues = await serviceClient.RetrieveMultipleAsync(query);
                if (queues.Entities.Count == 0)
                {
                    return $"No Queue found with name {queueName}. Do you want to search for Queue with the keyword {queueName}?";
                }
                var queue = queues.Entities.First();
                Queue queueObj = new Queue
                {
                    queueId = queue.Id,
                    name = queue.GetAttributeValue<string>("name"),
                    description = queue.GetAttributeValue<string>("description")
                };
                string result = JsonSerializer.Serialize(queueObj);
                return $"Queue Details:\n {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching queue by name.");
                return $"An error occurred: {ex.Message}";

            }
        }

        [McpServerTool, Description("Search queue details by keyword")]
        public async Task<string> SearchQueuesByKeyword(ServiceClient serviceClient,
            [Description("Keyword to search in name of Queues")] string keyword,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Pagination Cookie")] string pagingCookie = null,
            [Description("Number of records to be returned per page")] int numOfRecordsPerPage = 10)
        {
            string result = string.Empty;
            try
            {
                var query = new QueryExpression("queue")
                {
                    ColumnSet = new ColumnSet("name", "queueid", "description"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Like, $"%{keyword}%")
                    }
                    }
                };
                query.PageInfo = new PagingInfo
                {
                    Count = numOfRecordsPerPage,
                    PageNumber = pageNumber,
                    PagingCookie = pagingCookie
                };
                var queues = await serviceClient.RetrieveMultipleAsync(query);
                if (queues.Entities.Count == 0)
                {
                    return $"No Queue found with keyword {keyword}";
                }
                List<Queue> queueList = new List<Queue>();
                foreach (var queue in queues.Entities)
                {
                    Queue queueObj = new Queue
                    {
                        queueId = queue.Id,
                        name = queue.GetAttributeValue<string>("name"),
                        description = queue.GetAttributeValue<string>("description")
                    };
                    queueList.Add(queueObj);
                }
                result += string.Join("\n", JsonSerializer.Serialize(queueList));
                result += string.Join("\n", "Since there are multiple results, the user should be prompted to pick a Queue Id from the results to proceed with subsequent action.");
                if (queues.MoreRecords)
                {
                    var nextPageNumber = pageNumber + 1;
                    var nextPagingCookie = queues.PagingCookie;
                    result += $"\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber} and Paging Cookie: {nextPagingCookie}";
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching queues by keyword.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Add user as a member to a queue")]
        public async Task<string> AddUserToQueue(ServiceClient serviceClient,
            [Description("Queue Id (Guid)")] string queueId,
            [Description("User Id (Guid)")] string userId)
        {
            if (!Guid.TryParse(queueId, out Guid queueGuid))
            {
                return $"Invalid GUID format for Queue Id: {queueId}";
            }
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format for User Id: {userId}";
            }
            try
            {
                // Add user to queue using AssociateRequest
                var associateRequest = new AssociateRequest
                {
                    Target = new EntityReference("queue", queueGuid),
                    RelatedEntities = new EntityReferenceCollection
                    {
                        new EntityReference("systemuser", userGuid)
                    },
                    Relationship = new Relationship("queuemembership_association")
                };

                await serviceClient.ExecuteAsync(associateRequest);

                return $"User with Id {userId} has been added to Queue with Id {queueId} successfully.";

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding user to queue.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Remove user from a queue")]
        public async Task<string> RemoveUserFromQueue(ServiceClient serviceClient,
            [Description("Queue Id (Guid)")] string queueId,
            [Description("User Id (Guid)")] string userId)
        {
            if (!Guid.TryParse(queueId, out Guid queueGuid))
            {
                return $"Invalid GUID format for Queue Id: {queueId}";
            }
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format for User Id: {userId}";
            }
            try
            {
                // Remove user from queuemembership using dissociate request
                var disassociateRequest = new DisassociateRequest
                {
                    Target = new EntityReference("queue", queueGuid),
                    RelatedEntities = new EntityReferenceCollection
                    {
                        new EntityReference("systemuser", userGuid)
                    },
                    Relationship = new Relationship("queuemembership_association")
                };
                await serviceClient.ExecuteAsync(disassociateRequest);


                return $"User with Id {userId} has been removed from Queue with Id {queueId} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing user from queue.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Get team details by name")]
        public async Task<string> GetTeamByName(ServiceClient serviceClient,
            [Description("Name of the Team")] string teamName)
        {
            try
            {
                var query = new QueryExpression("team")
                {
                    ColumnSet = new ColumnSet("name", "teamid", "description", "businessunitid", "teamtype"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Equal, teamName)
                    }
                    }
                };
                var teams = await serviceClient.RetrieveMultipleAsync(query);
                if (teams.Entities.Count == 0)
                {
                    return $"No Team found with name {teamName}. Do you want to search for Team with the keyword {teamName}?";
                }
                var team = teams.Entities.First();
                Team teamObj = new Team
                {
                    teamId = team.Id,
                    name = team.GetAttributeValue<string>("name"),
                    description = team.GetAttributeValue<string>("description"),
                    businessUnitId = (Guid)(team.GetAttributeValue<EntityReference>("businessunitid")?.Id),
                    businessUnitName = team.GetAttributeValue<EntityReference>("businessunitid")?.Name,
                    teamType = team.GetAttributeValue<OptionSetValue>("teamtype")?.Value ?? 0
                };
                string result = JsonSerializer.Serialize(teamObj);
                return $"Team Details:\n {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching team by name.");
                return $"An error occurred: {ex.Message}";
            }
        }
        [McpServerTool, Description("Search team details by keyword")]
        public async Task<string> SearchTeamsByKeyword(ServiceClient serviceClient,
            [Description("Keyword to search in name of Teams")] string keyword,
            [Description("Page Number")] int pageNumber = 1,
            [Description("Pagination Cookie")] string pagingCookie = null,
            [Description("Number of records to be returned per page")] int numOfRecordsPerPage = 10)
        {
            string result = string.Empty;
            try
            {
                var query = new QueryExpression("team")
                {
                    ColumnSet = new ColumnSet("name", "teamid", "description", "businessunitid", "teamtype"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                    {
                        new ConditionExpression("name", ConditionOperator.Like, $"%{keyword}%")
                    }
                    }
                };
                query.PageInfo = new PagingInfo
                {
                    Count = numOfRecordsPerPage,
                    PageNumber = pageNumber,
                    PagingCookie = pagingCookie
                };
                var teams = await serviceClient.RetrieveMultipleAsync(query);
                if (teams.Entities.Count == 0)
                {
                    return $"No Team found with keyword {keyword}";
                }
                List<Team> teamList = new List<Team>();
                foreach (var team in teams.Entities)
                {
                    Team teamObj = new Team
                    {
                        teamId = team.Id,
                        name = team.GetAttributeValue<string>("name"),
                        description = team.GetAttributeValue<string>("description"),
                        businessUnitId = (Guid)(team.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Id),
                        businessUnitName = team.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Name,
                        teamType = team.GetAttributeValue<OptionSetValue>("teamtype")?.Value ?? 0
                    };
                    teamList.Add(teamObj);
                }
                result += string.Join("\n", JsonSerializer.Serialize(teamList));
                result += string.Join("\n", "Since there are multiple results, the user should be prompted to pick a Team Id from the results to proceed with subsequent action.");
                if (teams.MoreRecords)
                {
                    var nextPageNumber = pageNumber + 1;
                    var nextPagingCookie = teams.PagingCookie;
                    result += $"\nMore records are available. To fetch the next page, use Page Number: {nextPageNumber} and Paging Cookie: {nextPagingCookie}";
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while searching teams by keyword.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Add user to a team")]
        public async Task<string> AddUserToTeam(ServiceClient serviceClient,
            [Description("Team Id (Guid)")] string teamId,
            [Description("User Id (Guid)")] string userId)
        {
            if (!Guid.TryParse(teamId, out Guid teamGuid))
            {
                return $"Invalid GUID format for Team Id: {teamId}";
            }
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format for User Id: {userId}";
            }
            try
            {
                Guid[] memberIds = new Guid[] { userGuid };

                AddMembersTeamRequest request = new()
                {
                    MemberIds = memberIds,
                    TeamId = teamGuid
                };

                await serviceClient.ExecuteAsync(request);
                return $"User with Id {userId} has been added to Team with Id {teamId} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding user to team.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Remove user from a team")]
        public async Task<string> RemoveUserFromTeam(ServiceClient serviceClient,
            [Description("Team Id (Guid)")] string teamId,
            [Description("User Id (Guid)")] string userId)
        {
            if (!Guid.TryParse(teamId, out Guid teamGuid))
            {
                return $"Invalid GUID format for Team Id: {teamId}";
            }
            if (!Guid.TryParse(userId, out Guid userGuid))
            {
                return $"Invalid GUID format for User Id: {userId}";
            }
            try
            {
                Guid[] memberIds = new Guid[] { userGuid };
                RemoveMembersTeamRequest request = new()
                {
                    MemberIds = memberIds,
                    TeamId = teamGuid
                };
                await serviceClient.ExecuteAsync(request);
                return $"User with Id {userId} has been removed from Team with Id {teamId} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing user from team.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description(@"Assign security role to a Team based on the Team's business unit")]
        public async Task<string> AssignRoleToTeam(ServiceClient serviceClient,
            [Description("Security Role Id based on the business unit of the Team")] string roleId,
            [Description("Team Id (Guid)")] string teamId)
        {
            if (!Guid.TryParse(roleId, out Guid roleGuid))
            {
                return $"Invalid GUID format for Role Id: {roleId}";
            }
            if (!Guid.TryParse(teamId, out Guid teamGuid))
            {
                return $"Invalid GUID format for Team Id: {teamId}";
            }
            try
            {
                var associateRequest = new AssociateRequest
                {
                    Target = new EntityReference("team", teamGuid),
                    RelatedEntities = new EntityReferenceCollection
                    {
                        new EntityReference("role", roleGuid)
                    },
                    Relationship = new Relationship("teamroles_association")
                };
                await serviceClient.ExecuteAsync(associateRequest);
                return $"Security Role with Id {roleId} has been assigned to Team with Id {teamId} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while assigning security role to team.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description(@"Remove security role from a Team based on the team's business unit")]
        public async Task<string> RemoveRoleFromTeam(ServiceClient serviceClient,
            [Description("Security Role Id based on the business unit of the Team")] string roleId,
            [Description("Team Id (Guid)")] string teamId)
        {
            if (!Guid.TryParse(roleId, out Guid roleGuid))
            {
                return $"Invalid GUID format for Role Id: {roleId}";
            }
            if (!Guid.TryParse(teamId, out Guid teamGuid))
            {
                return $"Invalid GUID format for Team Id: {teamId}";
            }
            try
            {
                var disassociateRequest = new DisassociateRequest
                {
                    Target = new EntityReference("team", teamGuid),
                    RelatedEntities = new EntityReferenceCollection
                    {
                        new EntityReference("role", roleGuid)
                    },
                    Relationship = new Relationship("teamroles_association")
                };
                await serviceClient.ExecuteAsync(disassociateRequest);
                return $"Security Role with Id {roleId} has been removed from Team with Id {teamId} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing security role from team.");
                return $"An error occurred: {ex.Message}";
            }
        }

        [McpServerTool, Description("Change Business Unit of the Team")]
        public async Task<string> ChangeTeamBU(ServiceClient serviceClient,
            [Description("Team Id (Guid) of the Team")] string teamId,
            [Description("Business Unit Id (Guid) to which the Team should be moved")] string businessUnitId)
        {
            if (!Guid.TryParse(teamId, out Guid teamGuid))
            {
                return $"Invalid GUID format for Team Id: {teamId}";
            }
            if (!Guid.TryParse(businessUnitId, out Guid businessUnitGuid))
            {
                return $"Invalid GUID format for Business Unit Id: {businessUnitId}";
            }
            try
            {
                var teamEntity = new Entity("team", teamGuid);
                teamEntity["businessunitid"] = new EntityReference("businessunit", businessUnitGuid);
                var updateRequest = new UpdateRequest
                {
                    Target = teamEntity
                };
                await serviceClient.ExecuteAsync(updateRequest);
                return $"Team with Id {teamId} has been moved to Business Unit with Id {businessUnitId} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while changing team's business unit.");
                return $"An error occurred: {ex.Message}";
            }

        }

        [McpServerTool, Description("Get the security roles assigned to a team by team id (Guid)")]
        public async Task<string> GetRolesByTeamId(ServiceClient serviceClient,
            [Description("Team Id (Guid)")] string teamId)
        {
            if (!Guid.TryParse(teamId, out Guid teamGuid))
            {
                return $"Invalid GUID format for Team Id: {teamId}";
            }
            try
            {
                var query = new QueryExpression("role")
                {
                    ColumnSet = new ColumnSet("name", "roleid", "businessunitid"),
                    LinkEntities =
                {
                    new LinkEntity
                    {
                        LinkFromEntityName = "role",
                        LinkFromAttributeName = "roleid",
                        LinkToEntityName = "teamroles",
                        LinkToAttributeName = "roleid",
                        JoinOperator = JoinOperator.Inner,
                        LinkEntities =
                        {
                            new LinkEntity
                            {
                                LinkFromEntityName = "teamroles",
                                LinkFromAttributeName = "teamid",
                                LinkToEntityName = "team",
                                LinkToAttributeName = "teamid",
                                JoinOperator = JoinOperator.Inner,
                                LinkCriteria = new FilterExpression
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression("teamid", ConditionOperator.Equal, teamGuid)
                                    }
                                }
                            }
                        }
                    }
                }
                };
                var roles = await serviceClient.RetrieveMultipleAsync(query);
                if (roles.Entities.Count == 0)
                {
                    return $"No Security Role found assigned to Team with Id {teamId}";
                }
                List<Role> roleList = new List<Role>();
                foreach (var role in roles.Entities)
                {
                    Role roleObj = new Role
                    {
                        roleId = role.Id,
                        name = role.GetAttributeValue<string>("name"),
                        businessUnitId = (Guid)(role.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Id),
                        businessUnitName = role.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("businessunitid")?.Name
                    };
                    roleList.Add(roleObj);
                }
                string result = JsonSerializer.Serialize(roleList);
                return $"Security Roles assigned to Team with Id {teamId}:\n {result}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching security roles by team id.");
                return $"An error occurred: {ex.Message}";
            }
        }
    }
}
