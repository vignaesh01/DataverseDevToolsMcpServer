# DataverseDevTools MCP Server

A Model Context Protocol (MCP) server exposing ready-to-use Dataverse tools for user and security administration, data operations, metadata exploration, and troubleshooting. Stay in your flow: as a Dynamics 365/Dataverse developer, you don’t need to switch to XrmToolBox, make.powerapps.com, or admin.powerplatform.com for common tasks—run them right inside Visual Studio/VS Code or any MCP‑enabled client.

## Prerequisites

- .NET 8.0 SDK or later
- A Dataverse environment URL (e.g., https://org.crm.dynamics.com)
- Permissions in Dataverse to perform the requested operations
- For plugin trace logs, ensure Plugin Trace Log is enabled in the environment

## Install as global .NET tool

```
dotnet tool install --global vignaesh01.dataversedevtoolsmcpserver
```

Note: The executable command name is the tool command published with the package. If unsure, check with dotnet tool list -g.


## Setup in MCP clients

Use the server from your favorite MCP-enabled client. Below are quick setups for VS Code, Visual Studio, and Claude on Windows.

### VS Code (GitHub Copilot Chat)

**Prerequisites:**
- Latest VS Code
- GitHub Copilot and GitHub Copilot Chat extensions installed

**Step-by-Step instructions with Screenshots:**

- Go to : [Setup in VS Code](setup_vs_code/setup_vs_code.md)


**mcp.json - using the Global Tool (Recommended)**


```json
{
  "servers": {
    "dvmcp": {
      "type": "stdio",
      "command": "dataversedevtoolsmcpserver",
      "args": [
        "--environmentUrl",
        "https://yourorg.crm.dynamics.com"
      ]
    }
  }
}
```

**mcp.json - Using Client Credentials Authentication (Service Principal)**

For automated scenarios or when interactive login is not possible, you can use client credentials authentication with an Azure AD application:

```json
{
  "servers": {
    "dvmcp": {
      "type": "stdio",
      "command": "dataversedevtoolsmcpserver",
      "args": [
        "--environmentUrl",
        "https://yourorg.crm.dynamics.com",
        "--tenantId",
        "your-tenant-id",
        "--clientId",
        "your-client-id",
        "--clientSecret",
        "your-client-secret"
      ]
    }
  }
}
```

**Note:** To use client credentials authentication, you need to:
1. Register an application in Azure Active Directory
2. Create a client secret for the application
3. Add the application user in Dataverse with appropriate security roles
4. Use the tenant ID, client ID (application ID), and client secret in the configuration

**mcp.json - For Corporate Networks behind a proxy**
- Use Authenticated/Unauthenticated proxy address as appropriate:

```json
{
  "servers": {
    "dvmcp": {
      "type": "stdio",
      "command": "dataversedevtoolsmcpserver",
      "args": [
        "--environmentUrl",
        "https://yourorg.crm.dynamics.com"
      ],
      "env":{
          "HTTP_PROXY": "http://<username@domain.com>:<password>@<proxy.domain.com>:8080",
          "HTTPS_PROXY": "http://<username@domain.com>:<password>@<proxy.domain.com>:8080"
        }
    }
  }
}
```

**Sample `mcp.json` if you have cloned the GitHub Repository (For Explorers):**

```json
{
  "servers": {
    "dvmcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:/Projects/DataverseDevToolsMcpServer/DataverseDevToolsMcpServer/DataverseDevToolsMcpServer.csproj",
        "--environmentUrl",
        "https://yourorg.crm.dynamics.com"
      ]
    }
  }
}
```

**Sample `mcp.json` with Client Credentials (For Explorers using cloned repository):**

```json
{
  "servers": {
    "dvmcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:/Projects/DataverseDevToolsMcpServer/DataverseDevToolsMcpServer/DataverseDevToolsMcpServer.csproj",
        "--environmentUrl",
        "https://yourorg.crm.dynamics.com",
        "--tenantId",
        "your-tenant-id",
        "--clientId",
        "your-client-id",
        "--clientSecret",
        "your-client-secret"
      ]
    }
  }
}
```

- Start the MCP Server.

Usage: Open Copilot Chat in Agent mode and ask to use the Dataverse tools or dvmcp tools; the client will discover the server automatically.

### Visual Studio (GitHub Copilot Chat)

**Prerequisites:**
- Visual Studio 2022 17.10 or later
- GitHub Copilot and Copilot Chat installed
- MCP features enabled (Preview/Experimental), if required by your VS version. 
- Refer : https://learn.microsoft.com/en-us/visualstudio/ide/mcp-servers?view=vs-2022#configuration-example-with-a-github-mcp-server

**Step-by-Step instructions with Screenshots:**

- Go to : [Setup in Visual Studio](setup_vs/setup_vs.md)

### Claude Desktop

**Prerequisites:**
- If you have not already done so, download and install Claude desktop from here.
- Launch Claude desktop and navigate to File -> Settings
- Select Developer & Edit Config
- You will be launched into Claude directory:
- Open claude_desktop_config.json in Visual Studio Code and paste the following configuration in the JSON file.

```json
{
  "mcpServers": {
    "dvmcp": {
      "type": "stdio",
      "command": "dataversedevtoolsmcpserver",
      "args": [
        "--environmentUrl",
        "https://yourorg.crm.dynamics.com"
      ]
    }
  }
}
```

**With Client Credentials:**

```json
{
  "mcpServers": {
    "dvmcp": {
      "type": "stdio",
      "command": "dataversedevtoolsmcpserver",
      "args": [
        "--environmentUrl",
        "https://yourorg.crm.dynamics.com",
        "--tenantId",
        "your-tenant-id",
        "--clientId",
        "your-client-id",
        "--clientSecret",
        "your-client-secret"
      ]
    }
  }
}
```
- Save this file and go back to Claude. Exit Claude – don't just close the window, but use the menu to exit.


## Sample prompts

Use natural-language prompts in your MCP client; the client will map them to tool calls.

### User Management

- "What is my current Dataverse user and business unit?"
- "Find the user ‘Jane Doe’ and show her queues and teams."
- "List security roles for the user Jane Doe."
- "Assign Basic User security role to the user Jane Doe."
- "Remove Basic User security role from the user Jane Doe."
- "Move user Jane Doe to business unit Sales."
- "Add the user Jane Doe to team Super Squad."
- "Remove the user Jane Doe from team Super Squad."
- "Add the user Jane Doe to the  Super Squad queue."
- "Remove the user Jane Doe from Super Squad queue."

### Team Management
- "Assign Basic User security role to the Super Squad Team."
- "Remove Basic User security role from the Super Squad Team."
- "Change Business Unit of Super Squad team to Service business unit"

### Security Management

- "What privileges does Basic User security role has on the ‘account’ table?"
- "Show all privileges for role Basic User."
- "Which security roles have read privilege on account table and at what depth?"
- "Compare the privieleges between Basic User and Support user security role."

### Data Management

- "Execute this FetchXML query: <paste FetchXML>."
- "Execute the Dataverse Web API: <paste Web API Details>."
- "Generate a FetchXML query to get all the Opportunities where account type is premium."
- "Generate a Web API Odata query to get all the Opportunities where account type is premium."
- "Create a new account using Web API with this JSON payload."
- "Update contact record of Jane Doe with this JSON payload."
- "Upsert contact record of Jane Doe using alternate keys (new_key='ACME') with this payload."
- "Delete record contact record of Jane Doe."

### Entity Metadata

- "Find tables containing the keyword ‘invoice’."
- "Get full metadata for the ‘account’ table."
- "What is the logical name of the client type field in account table?"
- "List OptionSet values for field ‘statuscode’ on entity ‘incident’."
- "Show the privileges defined on the ‘contact’ entity."
- "List all global OptionSets."
- "Get option values of the global optionset Rating."


### Custom Actions & Custom APIs

- "Find custom actions with keyword 'qualify'."
- "Get metadata for custom action 'new_MyCustomAction'."
- "Find custom APIs containing 'calculate' in the name."
- "Get full metadata with parameters for custom API 'contoso_CalculateTotal'."
- "Show me how to call the custom API 'new_ProcessOrder' via Web API."
### Troubleshooting

- "Get plugin trace logs for the plugin ‘Contoso.Plugins.AccountPreCreate’."
- "Get plugin trace logs for correlation ID ‘aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee’."

## Tools overview

Below are the tools exposed by the server, grouped by category. Each item links to its source.

### User, Team & Queue Management

| Tool Name | Description |
| --- | --- |
| GetCurrentUser | Get details of the current logged-in user |
| GetUserByName | Get user details by full name |
| GetUserById | Get user details by user ID |
| SearchUsersByKeyword | Search users where fullname contains a keyword (with paging) |
| GetUserQueues | List queues for a user (with paging) |
| GetUserTeams | List teams for a user (with paging) |
| GetUserRoles | List security roles for a user |
| GetBUByName | Get BU details by name |
| SearchBUByKeyword | Search BUs by keyword (with paging) |
| GetRootBU | Get the root BU |
| GetRoleByNameAndBU | Get a role by name in a specific BU |
| SearchRolesByKeywordAndBU | Search roles by keyword in a BU (with paging) |
| AssignRoleToUser | Assign role to user |
| RemoveRoleFromUser | Remove role from user |
| ChangeUserBU | Change user’s BU |
| GetQueueByName | Get queue by name |
| SearchQueuesByKeyword | Search queues by keyword (with paging) |
| AddUserToQueue | Add user to queue |
| RemoveUserFromQueue | Remove user from queue |
| GetTeamByName | Get team by name |
| SearchTeamsByKeyword | Search teams by keyword (with paging) |
| AddUserToTeam | Add user to team |
| RemoveUserFromTeam | Remove user from team |
| AssignRoleToTeam | Assign role to team |
| RemoveRoleFromTeam | Remove role from team |
| ChangeTeamBU | Change team’s BU |
| GetRolesByTeamId | List roles assigned to a team |

### Security Management

| Tool Name | Description |
| --- | --- |
| GetEntityPrivByRoleId | Privileges a role has on a specific entity |
| GetAllPrivByRoleId | All privileges for a role |
| ListRolesByPrivId | Roles having a specific privilege ID |

### Data Management

| Tool Name | Description |
| --- | --- |
| ExecuteFetchXml | Run a FetchXML query (supports paging-cookie) |
| ExecuteWebApi | Execute raw Dataverse Web API requests |
| CreateRecord | Create a record (Web API) |
| UpdateRecord | Update by ID (Web API) |
| UpsertRecord | Upsert using alternate keys (Web API) |
| DeleteRecord | Delete by ID (Web API) |

### Entity Metadata

| Tool Name | Description |
| --- | --- |
| FindEntityLogicalNameUsingKeyword | Find entities by keyword |
| ListAllEntities | List all entities |
| GetEntityMetadataDetails | Full metadata (entity, attributes, relationships) |
| GetOptionSetValuesForEntityField | OptionSet values for a field |
| FindGlobalOptionSet | Find global OptionSets |
| GetGlobalOptionSetValues | Values of a global OptionSet |
| ListAllGlobalOptionSets | List all global OptionSets |
| GetEntityPrivileges | Privileges defined on an entity |

### Custom Actions & Custom APIs

| Tool Name | Description |
| --- | --- |
| FindCustomActionUsingKeyword | Find custom actions by keyword |
| GetCustomActionMetadata | Get custom action metadata with Web API usage info |
| FindCustomApiUsingKeyword | Find custom APIs by keyword |
| GetCustomApiMetadata | Get custom API metadata with request/response parameters and Web API usage info |

### Troubleshooting

| Tool Name | Description |
| --- | --- |
| GetPluginTracesByName | Plugin trace logs by type name (with paging) |
| GetPluginTracesByCorrId | Plugin trace logs by correlation ID (with paging) |

## Manage .Net Tool
- Update:
  - dotnet tool update -g vignaesh01.dataversedevtoolsmcpserver
- Uninstall:
  - dotnet tool uninstall -g vignaesh01.dataversedevtoolsmcpserver
- Verify:
  - dotnet tool list -g

## Notes

- Many search operations support paging via Page Number, Number of records per page, and Paging Cookie. When more records are available, the tool returns the next page token (paging cookie).
- For Web API tools:
  - Use entity set names (plural schema names).
  - For lookups, use <SchemaName>@odata.bind with the URL of the related record.
- Source files for tools:
  - User Management: [DataverseDevToolsMcpServer/Tools/UserManagementTools.cs](DataverseDevToolsMcpServer/Tools/UserManagementTools.cs)
  - Security Management: [DataverseDevToolsMcpServer/Tools/SecurityManagementTools.cs](DataverseDevToolsMcpServer/Tools/SecurityManagementTools.cs)
  - Data Management: [DataverseDevToolsMcpServer/Tools/DataManagementTools.cs](DataverseDevToolsMcpServer/Tools/DataManagementTools.cs)
  - Entity Metadata: [DataverseDevToolsMcpServer/Tools/EntityMetadataTools.cs](DataverseDevToolsMcpServer/Tools/EntityMetadataTools.cs)
  - Custom Actions & Custom APIs: [DataverseDevToolsMcpServer/Tools/CustomActionApiTools.cs](DataverseDevToolsMcpServer/Tools/CustomActionApiTools.cs)
  - Troubleshooting: [DataverseDevToolsMcpServer/Tools/TroubleshootingTools.cs](DataverseDevToolsMcpServer/Tools/TroubleshootingTools.cs)
