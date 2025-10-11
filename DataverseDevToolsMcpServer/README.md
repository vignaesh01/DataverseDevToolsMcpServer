# DataverseDevTools MCP Server

A Model Context Protocol (MCP) server exposing ready-to-use Dataverse tools for user and security administration, data operations, metadata exploration, and troubleshooting. Stay in your flow: as a Dynamics 365/Dataverse developer, you don’t need to switch to XrmToolBox, make.powerapps.com, or admin.powerplatform.com for common tasks—run them right inside Visual Studio/VS Code or any MCP‑enabled client.

## Prerequisites

- .NET 8.0 SDK or later
- A Dataverse environment URL (e.g., https://org.crm.dynamics.com)
- Permissions in Dataverse to perform the requested operations
- For plugin trace logs, ensure Plugin Trace Log is enabled in the environment

## Install (global .NET tool)

- Install:
  - dotnet tool install -g DataverseDevToolsMcpServer
- Update:
  - dotnet tool update -g DataverseDevToolsMcpServer
- Uninstall:
  - dotnet tool uninstall -g DataverseDevToolsMcpServer
- Verify:
  - dotnet tool list -g

Note: The executable command name is the tool command published with the package. If unsure, check with dotnet tool list -g.

## Setup


## Run

- Start the server:
  - dataversedevtoolsmcpserver --environmentUrl https://yourorg.crm.dynamics.com
- The server communicates over stdio and is discoverable by MCP-compatible clients (for example, GitHub Copilot Chat). Pass --environmentUrl every time you start it.

Authentication uses OAuth interactive login and will prompt if needed.

## Set up in MCP clients

Use the server from your favorite MCP-enabled client. Below are quick setups for VS Code, Visual Studio, and Claude on Windows.

### VS Code (GitHub Copilot Chat)

Prerequisites:
- Latest VS Code
- GitHub Copilot and GitHub Copilot Chat extensions installed
- MCP support enabled in Copilot Chat (Preview/Experimental in some builds).VS Code MCP setting should be set as "chat.mcp.discovery.enabled": true. Refer: https://code.visualstudio.com/docs/copilot/customization/mcp-servers

Configure the server via `mcp.json`:

- In VS Code: open View > Command Palette and run "MCP: Open User Configuration" to open/edit your `mcp.json`.

- Windows path (alternative): `%APPDATA%/Code/User/mcp.json` (for example: `C:/Users/<you>/AppData/Roaming/Code/User/mcp.json`).


Using the Global Tool (Recommended):

```json
{
  "servers": {
    "DataverseDevToolsMCPServer": {
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

Sample `mcp.json` if you have cloned the GitHub Repository (For Explorers):

```json
{
  "servers": {
    "DataverseDevToolsMCPServer": {
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



Usage: Open Copilot Chat in VS Code and ask to use the Dataverse tools; the client will discover the server automatically.

### Visual Studio (GitHub Copilot Chat)

Prerequisites:
- Visual Studio 2022 17.10 or later
- GitHub Copilot and Copilot Chat installed
- MCP features enabled (Preview/Experimental), if required by your VS version

Configuration options (depending on your VS build):
The following walkthrough requires version 17.14.9 or later.
- Create a new file: <SOLUTIONDIR>\.mcp.json or %USERPROFILE%\.mcp.json. We recommend that you use Visual Studio to edit this file so that its JSON schema is automatically applied.
- Paste the following contents into the .mcp.json file:

```json
{
  "servers": {
    "DataverseDevToolsMCPServer": {
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

- In Visual Studio, select the Ask arrow in the GitHub Copilot Chat window, and then select Agent.
- Select the tools that you want to use by clicking on toolset wrench icon.
- After configuration, open Copilot Chat in Visual Studio and the Dataverse server should appear as an available toolset.

### Claude (Desktop or Web with MCP support)

Prerequisites:
- Claude app/build with MCP support enabled (Labs/Settings)

Add a new local MCP server via Claude’s Settings > Integrations/Servers (labels vary by build):
- Command: `dataversedevtoolsmcpserver`
- Args: `--environmentUrl https://yourorg.crm.dynamics.com`

If running from source instead of the global tool, use:
- Command: `dotnet`
- Args: `run --project C:/Projects/DataverseDevToolsMcpServer/DataverseDevToolsMcpServer/DataverseDevToolsMcpServer.csproj --environmentUrl https://yourorg.crm.dynamics.com`

Once added, start a new chat and invoke Dataverse operations in natural language; Claude will call the server tools under the hood.

## Tools overview

Below are the tools exposed by the server, grouped by category. Each item links to its source.

### User, Team & Queue Management

| Tool Name | Description |
| --- | --- |
| GetCurrentUserInfo | Get details of the current logged-in user |
| GetUserByName | Get user details by full name |
| GetUserById | Get user details by user ID |
| SearchUsersByKeyword | Search users where fullname contains a keyword (with paging) |
| GetUserQueues | List queues for a user (with paging) |
| GetUserTeams | List teams for a user (with paging) |
| GetUserSecurityRoles | List security roles for a user |
| GetBusinessUnitByName | Get BU details by name |
| SearchBusinessUnitsByKeyword | Search BUs by keyword (with paging) |
| GetRootBusinessUnit | Get the root BU |
| GetSecurityRoleByNameAndBusinessUnit | Get a role by name in a specific BU |
| SearchSecurityRolesByKeywordAndBusinessUnit | Search roles by keyword in a BU (with paging) |
| AssignSecurityRoleToUser | Assign role to user |
| RemoveSecurityRoleFromUser | Remove role from user |
| ChangeUserBusinessUnit | Change user’s BU |
| GetQueueByName | Get queue by name |
| SearchQueuesByKeyword | Search queues by keyword (with paging) |
| AddUserToQueue | Add user to queue |
| RemoveUserFromQueue | Remove user from queue |
| GetTeamByName | Get team by name |
| SearchTeamsByKeyword | Search teams by keyword (with paging) |
| AddUserToTeam | Add user to team |
| RemoveUserFromTeam | Remove user from team |
| AssignSecurityRoleToTeam | Assign role to team |
| RemoveSecurityRoleFromTeam | Remove role from team |
| ChangeTeamBusinessUnit | Change team’s BU |
| GetSecurityRolesByTeamId | List roles assigned to a team |

### Security Management

| Tool Name | Description |
| --- | --- |
| GetEntityPrivilegesBySecurityRoleId | Privileges a role has on a specific entity |
| GetAllPrivilegesBySecurityRoleId | All privileges for a role |
| ListSecurityRolesByEntityPrivilegeId | Roles having a specific privilege ID |

### Data Management

| Tool Name | Description |
| --- | --- |
| ExecuteFetchXmlQuery | Run a FetchXML query (supports paging-cookie) |
| ExecuteWebApiRequest | Execute raw Dataverse Web API requests |
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
| FindGlobalOptionSetLogicalNameUsingKeyword | Find global OptionSets |
| GetGlobalOptionSetValues | Values of a global OptionSet |
| ListAllGlobalOptionSets | List all global OptionSets |
| GetEntityPrivileges | Privileges defined on an entity |

### Troubleshooting

| Tool Name | Description |
| --- | --- |
| GetPluginTraceLogsByPluginName | Plugin trace logs by type name (with paging) |
| GetPluginTraceLogsByCorrelationId | Plugin trace logs by correlation ID (with paging) |

## Sample prompts

Use natural-language prompts in your MCP client; the client will map them to tool calls.

### User Management

- “What is my current Dataverse user and business unit?”
- “Find the user ‘Jane Doe’ and show her queues and teams.”
- “Search users whose name contains ‘Doe’, page 1 with 10 per page.”
- “List security roles for user ID 00000000-0000-0000-0000-000000000000.”
- “Add user 00000000-0000-0000-0000-000000000000 to team 00000000-0000-0000-0000-000000000001.”
- “Move user 00000000-0000-0000-0000-000000000000 to business unit 00000000-0000-0000-0000-000000000002.”

### Security Management

- “For role 00000000-0000-0000-0000-000000000010, list its privileges on the ‘account’ table.”
- “Show all privileges for role 00000000-0000-0000-0000-000000000010.”
- “Which roles (root BU) include privilege ID 00000000-0000-0000-0000-000000000099 and at what depth?”

### Data Management

- “Execute this FetchXML and give me the first page of 10 records: <paste FetchXML>.”
- “Create a new account using Web API with this JSON payload.”
- “PATCH contact 00000000-0000-0000-0000-0000000000AA in entity set contacts with this JSON.”
- “Upsert a record in entity set new_widgets using alternate keys (new_key='ACME') with this payload.”
- “Delete record 00000000-0000-0000-0000-0000000000BB from entity set incidents.”

### Entity Metadata

- “Find tables containing the keyword ‘invoice’.”
- “Get full metadata for the ‘account’ table.”
- “List OptionSet values for field ‘statuscode’ on entity ‘incident’.”
- “List all global OptionSets.”
- “Show the privileges defined on the ‘contact’ entity.”

### Troubleshooting

- “Get plugin trace logs for type name containing ‘Contoso.Plugins.AccountPreCreate’ (page 1).”
- “Get plugin trace logs for correlation ID ‘aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee’.”

## Notes

- Many search operations support paging via Page Number, Number of records per page, and Paging Cookie. When more records are available, the tool returns the next page token (paging cookie).
- For Web API tools:
  - Use entity set names (plural schema names).
  - For lookups, use <SchemaName>@odata.bind with the URL of the related record.
- Source files for tools:
  - User Management: [DataverseDevToolsMcpServer/Tools/UserManagementTools.cs](DataverseDevToolsMcpServer/Tools/UserManagementTools.cs)
  - Security Management: [DataverseDevToolsMcpServer/Tools/SecurityManagement.cs](DataverseDevToolsMcpServer/Tools/SecurityManagement.cs)
  - Data Management: [DataverseDevToolsMcpServer/Tools/DataManagementTools.cs](DataverseDevToolsMcpServer/Tools/DataManagementTools.cs)
  - Entity Metadata: [DataverseDevToolsMcpServer/Tools/EntityMetadataTools.cs](DataverseDevToolsMcpServer/Tools/EntityMetadataTools.cs)
  - Troubleshooting: [DataverseDevToolsMcpServer/Tools/TroubleshootingTools.cs](DataverseDevToolsMcpServer/Tools/TroubleshootingTools.cs)