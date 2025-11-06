# RestSQL Documentation

RestSQL is a lightweight, high-performance SQL to REST API generator for .NET. It automatically creates REST endpoints from SQL queries defined in YAML configuration files.

## Getting Started

There are two ways to use RestSQL:

### 1. Standalone API (Using RestSQL.Api)

The simplest way to get started is using the pre-built API project:

1. Clone the repository
2. Configure your `appsettings.json`:

```json
{
  "RestSQL": {
    "ConfigFolder": "path/to/your/yaml/configs"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information"
    },
    "WriteTo": [
      {
        "Name": "Console"
      }
    ]
  }
}
```

3. Run the project:

```sh
cd src/RestSQL.Api
dotnet run
```

This will start a standalone API server with OpenAPI/Swagger documentation available at `/swagger` in development mode.

### 2. Library Usage (Adding to Existing Project)

Add RestSQL to your ASP.NET Core project:

```csharp
// Program.cs
builder.Services.AddRestSQL();

// Configure middleware
app.UseRestSQL("path/to/config/folder");
```

## Configuration

RestSQL uses YAML files for configuration. You need two types of files:

1. `connections.yaml` - Database connection definitions
2. Additional YAML files - Endpoint definitions

### Database Connections

Define your database connections in `connections.yaml`:

```yaml
connections:
  postgres1:
    type: PostgreSQL
    connectionString: "Host=localhost;Database=mydb;Username=user;Password=pass"
  mysql1:
    type: MySql
    connectionString: "Server=localhost;Database=mydb;User=user;Password=pass"
  oracle1:
    type: Oracle
    connectionString: "Data Source=localhost:1521/XEPDB1;User Id=system;Password=pass"
  sqlserver1:
    type: SqlServer
    connectionString: "Server=localhost;Database=mydb;User=sa;Password=pass;TrustServerCertificate=True"
  sqlite1:
    type: Sqlite
    connectionString: "Data Source=local.db"
```

### Endpoint Configuration

Define REST endpoints in YAML files:

```yaml
endpoints:
  - path: /api/posts
    method: GET
    statusCode: 200
    sqlQueries:
      posts: 
        connectionName: postgres1
        sql: |
          SELECT p.*, u.first_name, u.last_name 
          FROM posts p
          JOIN users u ON p.username = u.username
    outputStructure:
      type: object
      isArray: true
      queryName: posts
```

## Example Blog API

Let's look at a complete blog post API example:

```yaml
endpoints:
  # Get all posts with authors and tags
  - path: /api/posts
    method: GET
    statusCode: 200
    sqlQueries:
      posts:
        connectionName: postgres1
        sql: |
          SELECT 
            p.id,
            p.title,
            p.description,
            p.creation_date as "creationDate",
            p.username,
            array_agg(t.tag) as tags
          FROM posts p
          LEFT JOIN tags t ON t.post_id = p.id
          GROUP BY p.id, p.title, p.description, p.creation_date, p.username
          ORDER BY p.creation_date DESC

  # Create new post
  - path: /api/posts
    method: POST
    statusCode: 201
    writeOperations:
      - connectionName: postgres1
        sql: |
          INSERT INTO posts (title, description, creation_date, username)
          VALUES (@title, @description, @creationDate, @username)
          RETURNING id
        bodyType: object
        outputCaptures:
          - columnName: id
            parameterName: id

  # Add tag to post
  - path: /api/posts/{id}/tags
    method: POST 
    statusCode: 201
    writeOperations:
      - connectionName: postgres1
        sql: |
          INSERT INTO tags (post_id, tag)
          VALUES (@id, @tag)
        bodyType: value
        valueParameterName: tag
```

### Making Requests

Get posts:
```sh
GET /api/posts

Response:
[
  {
    "id": 1,
    "title": "The Joys of Async/Await",
    "description": "A deep dive into non-blocking operations in C#.",
    "creationDate": "2025-10-25T15:03:04",
    "username": "alice_codes",
    "tags": ["C#", "Async"]
  }
]
```

Create post:
```sh
POST /api/posts
{
  "title": "PostgreSQL vs MySQL",
  "description": "A performance comparison",
  "creationDate": "2025-10-26T16:05:15",
  "username": "bob_devs"
}

Response:
{
  "id": 2
}
```

Add tag:
```sh
POST /api/posts/2/tags
"Database"
```

## Advanced Features

### Output Structure Transformation

You can define complex nested JSON structures:

```yaml
outputStructure:
  type: object
  isArray: true
  fields:
    - name: post
      type: object
      fields:
        - name: id
          type: number
          columnName: id
        - name: title
          type: string
          columnName: title
    - name: tags
      type: array
      queryName: postTags
```

### Parameter Capture

Capture output from write operations:

```yaml
writeOperations:
  - connectionName: postgres1
    sql: "INSERT INTO posts ... RETURNING id"
    outputCaptures:
      - columnName: id
        parameterName: postId
  - connectionName: postgres1
    sql: "INSERT INTO tags (post_id, tag) VALUES (@postId, @tag)"
```

### Transaction Support

Multiple write operations are automatically wrapped in a transaction:

```yaml
writeOperations:
  - connectionName: postgres1
    sql: "INSERT INTO posts ..."
  - connectionName: postgres1
    sql: "INSERT INTO tags ..."  # Rolled back if posts insert fails
```

### Parameter Binding

- Route parameters: `{id}` in path
- Query parameters: `?search=term`
- Request body: JSON object or value

## Development Setup

1. Clone the repository
2. Install .NET 8.0 SDK
3. Run tests:

```sh
dotnet test
```

4. Start the API:

```sh
cd src/RestSQL.Api
dotnet run
```

For more examples, check out the integration tests in the `tests/RestSQL.IntegrationTests` directory.

## Supported Databases

- PostgreSQL
- SQL Server
- MySQL
- Oracle
- SQLite