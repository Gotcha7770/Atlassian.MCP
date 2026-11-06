# Atlassian MCP Server

Read-only MCP server for Jira and Confluence. Transport is STDIO; the first version provides
read operations only.

The current version supports Jira Data Center 10.x and Confluence Data Center 8.x.

## Requirements

- .NET SDK 10.0 (`global.json`)
- MCP client with STDIO support

## Configuration

Settings are provided through environment variables:

| Variable | Required | Purpose |
| --- | --- | --- |
| `ATLASSIAN_JIRA_URL` | yes | Jira address, for example `https://jira.example.com/jira` |
| `ATLASSIAN_JIRA_PAT` | yes | Jira Personal Access Token |
| `ATLASSIAN_CONFLUENCE_URL` | yes | Confluence address, for example `https://confluence.example.com/wiki` |
| `ATLASSIAN_CONFLUENCE_PAT` | yes | Confluence Personal Access Token |
| `ATLASSIAN_MCP_LANGUAGE` | no | message language: `en` (default) or `ru` |

Addresses may include a context path (`/jira`, `/wiki`). If the configuration is invalid or
incomplete, the server exits with a non-zero code and a message that does not contain token
values.

## Build and test

```powershell
dotnet build
dotnet test
```

## Running

```powershell
dotnet run --project Atlassian.MCP.Server
```

The server communicates over STDIO, so it is launched by an MCP client, not by an interactive
console.

### OpenCode v2

`opencode.json` in the repository root:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "servers": {
      "atlassian": {
        "type": "local",
        "command": ["dotnet", "run", "--project", "Atlassian.MCP.Server"],
        "environment": {
          "ATLASSIAN_JIRA_URL": "https://jira.example.com/jira",
          "ATLASSIAN_JIRA_PAT": "<jira-pat>",
          "ATLASSIAN_CONFLUENCE_URL": "https://confluence.example.com/wiki",
          "ATLASSIAN_CONFLUENCE_PAT": "<confluence-pat>"
        },
        "enabled": true
      }
    }
  }
}
```

## Tools

| Tool | Status |
| --- | --- |
| `jiraGetIssue` | implemented |
| `jiraSearchIssues` | in development |
| `jiraGetProject` | in development |
| `confluenceSearch` | in development |
| `confluenceGetPage` | in development |
| `confluenceGetPageChildren` | in development |

All tools are read-only. The response is limited to 64 KB, long text fields to 16 KB; when a
value is truncated, `truncated` is set and `truncation` is filled. Error messages contain the
system, the HTTP status code and a short reason; tokens and the `Authorization` header never
appear in responses or diagnostics.

## Project structure

- `Atlassian.Mcp.Core` — models, contracts, response limits
- `Atlassian.Mcp.Infrastructure` — Refit clients, HTTP handlers, configuration
- `Atlassian.MCP.Server` — MCP tools and entry point
- `tests/Atlassian.Mcp.Tests` — unit tests
- `tests/Atlassian.Mcp.IntegrationTests` — HTTP layer integration tests
