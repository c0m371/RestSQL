# RestSQL [![Nuget](https://img.shields.io/nuget/v/Comet1.RestSQL)](https://www.nuget.org/packages/Comet1.RestSQL) [![Docker](https://img.shields.io/docker/v/cometone/restsql)](https://hub.docker.com/r/cometone/restsql)

RestSQL is a lightweight .NET tool that turns SQL queries (defined in YAML) into ready-to-run REST endpoints. Works standalone or as a library, supports transactions, nested JSON output, and multiple database providers.

## Getting Started

There are two ways to use RestSQL:

### 1. Standalone API (Using RestSQL.Api)

#### Build from source

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

#### Alternatively, use docker 
[![Docker](https://img.shields.io/docker/v/cometone/restsql)](https://hub.docker.com/r/cometone/restsql)


Set the `RestSQL:ConfigFolder` key using the environment variable name `RestSQL__ConfigFolder` (double underscore maps to a colon in ASP.NET configuration).

Example Docker run (mount local configs and set env):

```sh
docker run --rm -p 7017:80 \
  -v $(pwd)/my-yaml-configs:/app/config \
  -e RestSQL__ConfigFolder=/app/config \
  cometone/restsql-api:latest
```

Running inside containers: connection string tips

When your API runs inside Docker, the database host in connection strings must be reachable from the container. A few common patterns:

- Docker Desktop (Windows/macOS): use `host.docker.internal` to reach services running on the host machine. Example Postgres connection string:

  `Host=host.docker.internal;Port=5432;Database=mydb;Username=user;Password=pass`

- Linux Docker (host.docker.internal): older Docker engines on Linux don't provide `host.docker.internal` by default. You can add it at container startup with `--add-host` (Docker 20.10+ with host-gateway):

  ```sh
  docker run --add-host=host.docker.internal:host-gateway \
    -e RestSQL__ConfigFolder=/app/config \
    -v $(pwd)/my-yaml-configs:/app/config \
    cometone/restsql-api:latest
  ```

### 2. Library Usage (Adding to Existing Project)

Add RestSQL to your ASP.NET Core project:

```csharp
// Program.cs
builder.Services.AddRestSQL();

// Configure middleware
app.UseRestSQL("path/to/config/folder");
```

Either link the project via the source code, or use Nuget 
![NuGet Version](https://img.shields.io/nuget/v/Comet1.RestSQL)

## Configuration

RestSQL uses YAML files for configuration. You need two main configuration sections:

1. connections - Database connection definitions
2. endpoints - Endpoint definitions

You can split these across files as you see fit, they will be merged.
A single file is also fine. 

### Database Connections

Define your database connections under connections:

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

Define REST endpoints:

```yaml
endpoints:
  # Get all posts with tags
  - path: /api/posts
    method: GET
    statusCode: 200
    sqlQueries:
      posts:
        connectionName: blog
        sql: >
          select id post_id, title, description, creation_date, username
          from posts;
      tags: &tagsQuery
        connectionName: blog
        sql: >
          select *
          from tags;
    outputStructure:
      type: Object
      isArray: true
      queryName: posts
      fields: &postFields
        - { type: Long, name: id, columnName: post_id }
        - { type: String, name: title, columnName: title }
        - { type: String, name: description, columnName: description }
        - { type: String, name: username, columnName: username }
        - { type: String, name: creationDate, columnName: creation_date }
        - type: string
          isArray: true
          name: tags
          queryName: tags
          columnName: tag
          linkColumn: post_id
```

## Example Blog API

Let's look at a complete blog post API example:

```yaml
connections:
  blog:
    type: PostgreSQL
    connectionString: "Host=localhost;Database=restsql_blog;Username=restsql_blog;Password=restsql_blog"

endpoints:
  # Get all posts with tags
  - path: /api/posts
    method: GET
    statusCode: 200
    sqlQueries:
      posts:
        connectionName: blog
        sql: >
          select id post_id, title, description, creation_date, username
          from posts;
      tags: &tagsQuery
        connectionName: blog
        sql: >
          select *
          from tags;
    outputStructure:
      type: Object
      isArray: true
      queryName: posts
      fields: &postFields
        - { type: Long, name: id, columnName: post_id }
        - { type: String, name: title, columnName: title }
        - { type: String, name: description, columnName: description }
        - { type: String, name: username, columnName: username }
        - { type: String, name: creationDate, columnName: creation_date }
        - type: string
          isArray: true
          name: tags
          queryName: tags
          columnName: tag
          linkColumn: post_id
      
  # Get specific post
  - path: /api/posts/{id}
    method: GET
    statusCode: 200
    statusCodeOnEmptyResult: 404
    sqlQueries:
      posts:
        connectionName: blog
        sql: >
          select id post_id, title, description, creation_date, username
          from posts
          where id = :id::int;
      tags: &tagsQuery
        connectionName: blog
        sql: >
          select *
          from tags;
    outputStructure:
      type: Object
      isArray: true
      queryName: posts
      fields: *postFields

  # Create new post, and return the created post
  - path: /api/posts
    method: POST
    statusCode: 200
    writeOperations:
      - connectionName: blog
        sql: >
          insert into posts (title, description, creation_date, username)
          values (:title, :description, current_timestamp, :username)
          returning id;
        bodyType: Object
        outputCaptures:
          - columnName: id
            parameterName: post_id
    sqlQueries:
      posts:
        connectionName: blog
        sql: >
          select id post_id, title, description, creation_date, username
          from posts
          where id = @post_id;
      tags: *tagsQuery
    outputStructure:
      type: Object
      queryName: posts
      fields: *postFields
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
  "username": "bob_devs"
}

Response:
{
  "id": 2,
  "title": "PostgreSQL vs MySQL",
  "description": "A performance comparison",
  "username": "bob_devs",
  "creationDate": "06/11/2025 10:30:41",
  "tags": []
}
```

Get specific post:
```sh
GET /api/posts/2

Response:
[
  {
    "id": 2,
    "title": "PostgreSQL vs MySQL: A Performance Review",
    "description": "Comparing the speed and features of two popular databases.",
    "username": "alice_codes",
    "creationDate": "22/10/2025 22:00:00",
    "tags": [
      "PostgreSQL",
      "Database"
    ]
  }
]
```

## Advanced Features

### Output Structure Transformation

You can define complex nested JSON structures.
Queries can be linked through a linkColumn.

```yaml
    sqlQueries:
      posts:
        connectionName: blog
        sql: >
          select id post_id, title, description, creation_date, username
          from posts;
      tags:
        connectionName: blog
        sql: >
          select *
          from tags;
    outputStructure:
      type: Object
      isArray: true
      queryName: posts
      fields:
        - { type: Long, name: id, columnName: post_id }
        - { type: String, name: title, columnName: title }
        - { type: String, name: description, columnName: description }
        - { type: String, name: username, columnName: username }
        - { type: String, name: creationDate, columnName: creation_date }
        - type: string
          isArray: true
          name: tags
          queryName: tags
          columnName: tag
          linkColumn: post_id
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
2. Install .NET 9.0 SDK
3. Run tests:

```sh
dotnet test
```

Note that docker should be running to be able to run the integration tests with test containers.

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
