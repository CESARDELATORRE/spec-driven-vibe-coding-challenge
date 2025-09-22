# Data Model - kb-mcp-server

Scope is intentionally minimal; no complex relational structures. Entities are implicit in service/tool responses.

## 1. Entities
### KnowledgeBaseInfo
| Field | Type | Description |
|-------|------|-------------|
| IsAvailable | bool | Indicates if initialization succeeded |
| ContentLength | int | Character length of loaded content |
| Description | string | Human-readable description |
| FileSizeBytes | long | Size on disk of source file |
| LastModified | DateTime | Last write time of file |

### Tool Payloads (Protocol Boundary)
`get_kb_info` returns object:
```
{
  "status": "ok" | "unavailable",
  "info": {
    "fileSizeBytes": number,
    "contentLength": number,
    "isAvailable": boolean,
    "description": string,
    "lastModified": string (ISO-8601)
  }
}
```

`get_kb_content` returns object:
```
{
  "status": "ok" | "empty" | "error",
  "contentLength": number?,
  "content": string?,
  "error": string? // only when status = error
}
```

## 2. Relationships
Single knowledge base instance; no collections beyond the raw content string.

## 3. Validation Rules
| Field | Rule | Rationale |
|-------|------|-----------|
| ContentLength | >= 0 | Non-negative invariant |
| status (info) | Matches availability | Ensures protocol contract clarity |
| status (content) | Maps to length or exception state | Prevents ambiguous empty/error responses |

## 4. State Transitions
| State | Trigger | Next State |
|-------|---------|-----------|
| Uninitialized | Start | Initialized (on successful `InitializeAsync`) |
| Uninitialized | Init failure | Uninitialized (error logged) |
| Initialized | File mutation (not tracked) | Initialized (stale until restart) |

## 5. Edge Cases
| Scenario | Handling |
|----------|----------|
| File missing | Initialization returns false; info.status = unavailable |
| Empty file | contentLength = 0; content.status = empty |
| Exception during read | status = error with message |
| Tool invoked before init | Returns empty content (warning logged) |

## 6. Serialization Considerations
Anonymous objects created in tool methods; MCP host handles JSON serialization. Field names intentionally snake_case where exposed (`fileSizeBytes` kept camelCase for internal consistency— acceptable for prototype; future harmonization optional).

## 7. Versioning
Payloads currently unversioned; introduce `schemaVersion` field if additive growth or backward-incompatible changes considered.

---
Aligned with implementation plan v1.2 and current codebase (September 2025).
