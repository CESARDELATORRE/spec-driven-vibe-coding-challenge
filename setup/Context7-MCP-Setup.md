# Context7 MCP Integration Setup Guide

## Overview

Context7 is a Model Context Protocol (MCP) server that provides access to developer documentation through AI assistants. This guide covers configuration options, remote vs local setup, and installation alternatives.

## Configuration Options

### 1. Docker-based MCP Server (Recommended)

The current configuration in `.vscode/mcp.json` includes Context7 along with other MCP servers (GitHub and Perplexity):

```json
{
    "inputs": [
        {
            "type": "promptString",
            "id": "perplexity-key",
            "description": "Perplexity API Key",
            "password": true
        },
        {
            "type": "promptString",
            "id": "github_token_new",
            "description": "GitHub Personal Access Token",
            "password": true
        }
    ],
    "servers": {
        "github": {
            "command": "docker",
            "args": ["run", "-i", "--rm", "--pull=always", "-e", "GITHUB_PERSONAL_ACCESS_TOKEN", "ghcr.io/github/github-mcp-server"],
            "env": {
                "GITHUB_PERSONAL_ACCESS_TOKEN": "${input:github_token_new}"
            }
        },
        "perplexity-ask": {
            "command": "docker",
            "args": ["run", "-i", "--rm", "-e", "PERPLEXITY_API_KEY", "mcp/perplexity-ask"],
            "env": {
                "PERPLEXITY_API_KEY": "${input:perplexity-key}"
            }
        },
        "context7": {
            "command": "docker",
            "args": ["run", "--rm", "-i", "node:18-alpine", "sh", "-c", "npm install -g @upstash/context7-mcp@1.0.14 && context7-mcp"],
            "transport": {
                "type": "stdio"
            }
        }
    }
}
```

**Benefits:**
- No need for Node.js/npm installation on host system
- Isolated environment for MCP server
- Consistent behavior across different systems
- Direct integration with VS Code
- Standard MCP protocol compliance

**Prerequisites:**
- Docker installed and running on your system

### 2. Local MCP Server with npx (Alternative)

For systems with Node.js/npm installed, you can alternatively use npx by replacing the Context7 server configuration:

```json
{
    "servers": {
        "context7": {
            "command": "npx",
            "args": ["-y", "@upstash/context7-mcp@1.0.14"],
            "transport": {
                "type": "stdio"
            }
        }
    }
}
```
  }
}
```
**Benefits:**
- Faster startup (no Docker overhead)
- Works offline after initial package download
- Direct Node.js execution

**Prerequisites:**
- Node.js and npm/npx installed on your system

### 3. HTTP Transport (Alternative Setup)

For HTTP-based communication with Docker:

```json
{
  "mcpServers": {
    "context7": {
      "command": "docker",
      "args": ["run", "--rm", "-p", "3000:3000", "node:18-alpine", "sh", "-c", "npm install -g @upstash/context7-mcp@1.0.14 && context7-mcp --transport http --port 3000"],
      "transport": {
        "type": "http",
        "host": "localhost",
        "port": 3000
      }
    }
  }
}
```

### 4. Remote Context7 Service

**Current Status**: Context7 does not currently offer a hosted remote MCP server that can be directly accessed via HTTP from VS Code's MCP configuration. The Context7 service primarily operates as a local MCP server package.

**Why Remote HTTP is Not Available:**
- MCP servers typically run locally for security and performance
- Context7's @upstash/context7-mcp package is designed for local execution
- No publicly documented remote Context7 HTTP endpoints for MCP integration

## Installation Alternatives

### Option 1: Using Docker (Recommended)
```bash
# Verify Docker is installed and running
docker --version
docker info

# Test Context7 MCP server with Docker
docker run --rm -i node:18-alpine sh -c "npm install -g @upstash/context7-mcp@1.0.14 && context7-mcp --help"
```

### Option 2: Using npx (Alternative)
If you have Node.js/npm installed:

```bash
# Verify npx is available
npx --version

# Test Context7 installation
npx -y @upstash/context7-mcp@1.0.14 --help
```
### Option 3: Global npm Installation
If npx is not available but npm is:

```bash
# Install globally
npm install -g @upstash/context7-mcp@1.0.14

# Update mcp.json to use global installation
{
  "mcpServers": {
    "context7": {
      "command": "context7-mcp",
      "transport": {
        "type": "stdio"
      }
    }
  }
}
```

### Option 4: Local Project Installation
```bash
# In your project directory
npm install @upstash/context7-mcp@1.0.14

# Update mcp.json
{
  "mcpServers": {
    "context7": {
      "command": "node",
      "args": ["./node_modules/@upstash/context7-mcp/dist/index.js"],
      "transport": {
        "type": "stdio"
      }
    }
  }
}
```

### Option 5: When Docker and Node.js are Not Available

If neither Docker nor Node.js is installed:

1. **Install Docker (Recommended):**
   ```bash
   # Ubuntu/Debian
   curl -fsSL https://get.docker.com -o get-docker.sh
   sudo sh get-docker.sh

   # macOS with Homebrew
   brew install --cask docker

   # Windows - Download Docker Desktop from docker.com
   ```

2. **Install Node.js (Alternative):**
   ```bash
   # Ubuntu/Debian
   curl -fsSL https://deb.nodesource.com/setup_lts.x | sudo -E bash -
   sudo apt-get install -y nodejs

   # macOS with Homebrew
   brew install node

   # Windows - Download from nodejs.org
   ```

2. **Alternative: Pre-built Docker Image**
   ```dockerfile
   # Dockerfile for Context7 MCP (if you want to build your own image)
   FROM node:18-alpine
   RUN npm install -g @upstash/context7-mcp@1.0.14
   EXPOSE 3000
   CMD ["context7-mcp"]
   ```

## Current Best Practices

1. **Use Docker**: Prefer Docker-based installation for consistent behavior and isolated environment
2. **Use Specific Version**: Pin to version `1.0.14` (latest stable) instead of `@latest`
3. **Transport Type**: Use `stdio` for VS Code integration (fastest and most reliable)
4. **Local Installation**: Prefer local MCP server over remote for security and performance
5. **Configuration Format**: Use the `mcpServers` object format as per MCP specification

## Available Tools

When properly configured, Context7 provides these MCP tools:
- `resolve-library-id`: Find referenced libraries
- `get-library-docs`: Retrieve library documentation

## Troubleshooting

### Common Issues:

1. **Docker not found**: Install Docker or use alternative installation methods above
2. **Docker permission errors**: Ensure your user is in the docker group or use sudo
3. **Package not found**: Ensure internet connection for package download
4. **VS Code not detecting**: Restart VS Code after mcp.json changes
5. **Container startup slow**: First run may be slower due to image download and npm install

### Testing Installation:

```bash
# Test Docker-based Context7 MCP server directly
docker run --rm -i node:18-alpine sh -c "npm install -g @upstash/context7-mcp@1.0.14 && context7-mcp --help"

# Test HTTP mode with Docker
docker run --rm -p 3000:3000 node:18-alpine sh -c "npm install -g @upstash/context7-mcp@1.0.14 && context7-mcp --transport http --port 3000"

# Test npx fallback (if Node.js is available)
npx -y @upstash/context7-mcp@1.0.14 --help
```

## Migration from Previous Configuration

If you have an older configuration, update it to use the new Docker-based format:

**Old npx format:**
```json
{
  "context7": {
    "command": "npx",
    "args": ["-y", "@upstash/context7-mcp@latest"]
  }
}
```

**New Docker format (recommended):**
```json
{
  "mcpServers": {
    "context7": {
      "command": "docker",
      "args": ["run", "--rm", "-i", "node:18-alpine", "sh", "-c", "npm install -g @upstash/context7-mcp@1.0.14 && context7-mcp"],
      "transport": {
        "type": "stdio"
      }
    }
  }
}
```

**Alternative npx format:**
```json
{
  "mcpServers": {
    "context7": {
      "command": "npx",
      "args": ["-y", "@upstash/context7-mcp@1.0.14"],
      "transport": {
        "type": "stdio"
      }
    }
  }
}
```