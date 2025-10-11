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

Sample `mcp.json` if you have cloned the GitHub Repository (For Explorers):

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

- Start the MCP Server.

Usage: Open Copilot Chat in VS Code and ask to use the Dataverse tools; the client will discover the server automatically.

### Visual Studio (GitHub Copilot Chat)

Prerequisites:
- Visual Studio 2022 17.10 or later
- GitHub Copilot and Copilot Chat installed
- MCP features enabled (Preview/Experimental), if required by your VS version. 
- Refer : https://learn.microsoft.com/en-us/visualstudio/ide/mcp-servers?view=vs-2022#configuration-example-with-a-github-mcp-server

Configuration options (depending on your VS build):
The following walkthrough requires version 17.14.9 or later.
- Create a new file: <SOLUTIONDIR>\.mcp.json or %USERPROFILE%\.mcp.json. We recommend that you use Visual Studio to edit this file so that its JSON schema is automatically applied.
- Paste the following contents into the .mcp.json file:

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

- In Visual Studio, select the Ask arrow in the GitHub Copilot Chat window, and then select Agent.
- Select the tools that you want to use by clicking on toolset wrench icon.
- After configuration, open Copilot Chat in Visual Studio and the Dataverse server should appear as an available toolset.

### Claude Desktop

Prerequisites:
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

- "What privileges does Basic User security role has on the ‘account’ table."
- "Show all privileges for role Basic User."
- "Which security roles have read privilege on account table and at what depth?"
- "Compare the privieleges between Basic User and Support user security role."

### Data Management

- "Execute this FetchXML query: <paste FetchXML>."
- "Execute the Dataverse Web API: <paste Web API Details>"
- "Create a new account using Web API with this JSON payload."
- "Update contact record of Jane Doe with this JSON payload."
- "Upsert contact record of Jane Doe using alternate keys (new_key='ACME') with this payload."
- "Delete record contact record of Jane Doe."

### Entity Metadata

- "Find tables containing the keyword ‘invoice’."
- "Get full metadata for the ‘account’ table."
- "List OptionSet values for field ‘statuscode’ on entity ‘incident’."
- "Show the privileges defined on the ‘contact’ entity."
- "List all global OptionSets."
- "Get option values of the global optionset Rating."

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

### Troubleshooting

| Tool Name | Description |
| --- | --- |
| GetPluginTracesByName | Plugin trace logs by type name (with paging) |
| GetPluginTracesByCorrId | Plugin trace logs by correlation ID (with paging) |



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