# SWLOR.Game.Server Deployment Documentation

This document provides comprehensive information about deploying and running the SWLOR.Game.Server project, including Docker configuration, environment setup, and production considerations.

## Overview

SWLOR.Game.Server is designed to run in a containerized environment using Docker, with support for different deployment environments (Development, Test, Production). The deployment system includes monitoring with Grafana and uses Redis for caching and data storage.

## Directory Structure

```
Docker/
├── docker-compose.yml          # Main Docker Compose configuration
├── Dockerfile                  # Container build instructions
├── swlor.env                   # Environment variables
├── settings.tml                # Application configuration
└── grafana-provisioning/       # Grafana dashboards and datasources
    ├── dashboards/
    └── datasources/
```

## Docker Configuration

### 1. Docker Compose Setup

The main deployment configuration is in `docker-compose.yml`:

```yaml
version: '3.8'
services:
  swlor-server:
    build: .
    environment:
      - SWLOR_ENVIRONMENT=development
      - SWLOR_APP_LOG_DIRECTORY=/app/logs
      - NWNX_REDIS_HOST=redis:6379
    volumes:
      - ./logs:/app/logs
    depends_on:
      - redis
      - grafana

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    volumes:
      - ./grafana-provisioning:/etc/grafana/provisioning
```

### 2. Environment Configuration

Environment variables are configured in `swlor.env`:

```bash
# Server Environment
SWLOR_ENVIRONMENT=development  # development, test, production

# Logging
SWLOR_APP_LOG_DIRECTORY=/app/logs

# Redis Configuration
NWNX_REDIS_HOST=redis:6379

# Database Configuration
DB_CONNECTION_STRING=your_connection_string

# Discord Integration
DISCORD_BOT_TOKEN=your_discord_token
DISCORD_GUILD_ID=your_guild_id
```

### 3. Application Settings

The application uses `ApplicationSettings.cs` to manage configuration:

```csharp
public class ApplicationSettings
{
    public string LogDirectory { get; }
    public string RedisIPAddress { get; }
    public ServerEnvironmentType ServerEnvironment { get; }
    
    // Environment detection
    private ApplicationSettings()
    {
        LogDirectory = Environment.GetEnvironmentVariable("SWLOR_APP_LOG_DIRECTORY");
        RedisIPAddress = Environment.GetEnvironmentVariable("NWNX_REDIS_HOST");
        
        var environment = Environment.GetEnvironmentVariable("SWLOR_ENVIRONMENT");
        ServerEnvironment = ParseEnvironment(environment);
    }
}
```

## Deployment Environments

### 1. Development Environment

**Purpose**: Local development and testing

**Configuration**:
```bash
SWLOR_ENVIRONMENT=development
SWLOR_APP_LOG_DIRECTORY=./logs
NWNX_REDIS_HOST=localhost:6379
```

**Features**:
- Detailed logging enabled
- Debug information available
- Hot reloading support
- Test data available

### 2. Test Environment

**Purpose**: Staging and testing before production

**Configuration**:
```bash
SWLOR_ENVIRONMENT=test
SWLOR_APP_LOG_DIRECTORY=/app/logs
NWNX_REDIS_HOST=redis:6379
```

**Features**:
- Production-like configuration
- Test data and scenarios
- Performance monitoring
- Integration testing

### 3. Production Environment

**Purpose**: Live game server

**Configuration**:
```bash
SWLOR_ENVIRONMENT=production
SWLOR_APP_LOG_DIRECTORY=/app/logs
NWNX_REDIS_HOST=redis:6379
```

**Features**:
- Optimized performance
- Minimal logging
- Security hardening
- High availability setup

## Monitoring and Logging

### 1. Grafana Dashboards

The deployment includes pre-configured Grafana dashboards for monitoring:

**Location**: `Docker/grafana-provisioning/dashboards/`

**Available Dashboards**:
- Server Performance
- Player Activity
- Combat Statistics
- Error Tracking
- Database Performance

### 2. Logging Configuration

Logging is handled by Serilog with multiple sinks:

```csharp
// Logging setup in the application
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/swlor-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

**Log Groups**:
- `LogGroup.Attack` - Combat-related logs
- `LogGroup.Ability` - Ability usage logs
- `LogGroup.Quest` - Quest progression logs
- `LogGroup.Error` - Error and exception logs

## Build and Deployment Process

### 1. Local Development

```bash
# Build the project
dotnet build

# Run with Docker Compose
docker-compose up --build

# Run in development mode
docker-compose -f docker-compose.dev.yml up
```

### 2. Production Deployment

```bash
# Build production image
docker build -t swlor-game-server:latest .

# Deploy with production configuration
docker-compose -f docker-compose.prod.yml up -d

# Monitor deployment
docker-compose logs -f swlor-server
```

### 3. CI/CD Pipeline

The project includes a post-build target in `SWLOR.Game.Server.csproj`:

```xml
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="cd $(SolutionDir)Build&#xD;&#xA;SWLOR.CLI.exe -o" />
</Target>
```

## Dependencies

### 1. External Dependencies

**NuGet Packages**:
- `NWN.Native` - Neverwinter Nights engine integration
- `StackExchange.Redis` - Redis client
- `Serilog` - Logging framework
- `Discord.Net` - Discord bot integration
- `Newtonsoft.Json` - JSON serialization

### 2. System Requirements

**Minimum Requirements**:
- .NET 7.0 Runtime
- Docker and Docker Compose
- 4GB RAM
- 10GB disk space

**Recommended Requirements**:
- .NET 7.0 SDK
- 8GB RAM
- 20GB disk space
- SSD storage

## Security Considerations

### 1. Environment Variables

**Sensitive Data**:
- Database connection strings
- API keys and tokens
- Discord bot tokens
- Redis passwords

**Best Practices**:
- Never commit sensitive data to version control
- Use Docker secrets for production
- Rotate credentials regularly
- Use environment-specific configurations

### 2. Network Security

**Port Configuration**:
- Redis: 6379 (internal only)
- Grafana: 3000 (monitoring)
- Game Server: Custom ports as needed

**Firewall Rules**:
- Restrict external access to monitoring ports
- Use VPN for administrative access
- Implement rate limiting

## Troubleshooting

### 1. Common Issues

**Redis Connection Issues**:
```bash
# Check Redis container
docker-compose logs redis

# Test Redis connection
docker exec -it swlor_redis_1 redis-cli ping
```

**Log File Issues**:
```bash
# Check log directory permissions
ls -la logs/

# View recent logs
docker-compose logs swlor-server --tail=100
```

**Performance Issues**:
```bash
# Monitor resource usage
docker stats

# Check Grafana dashboards
# Access http://localhost:3000
```

### 2. Debug Mode

Enable debug logging by setting environment variables:

```bash
SWLOR_ENVIRONMENT=development
SWLOR_DEBUG=true
```

### 3. Health Checks

The application includes health check endpoints:

```csharp
// Health check implementation
public static bool IsHealthy()
{
    try
    {
        // Check Redis connection
        // Check database connection
        // Check core systems
        return true;
    }
    catch
    {
        return false;
    }
}
```

## Scaling Considerations

### 1. Horizontal Scaling

**Load Balancing**:
- Use multiple server instances
- Implement session management
- Configure Redis clustering

**Database Scaling**:
- Read replicas for queries
- Connection pooling
- Query optimization

### 2. Vertical Scaling

**Resource Allocation**:
- Increase container memory limits
- Optimize garbage collection
- Use performance profiling

**Monitoring**:
- Set up alerts for resource usage
- Monitor response times
- Track error rates

## Backup and Recovery

### 1. Data Backup

**Redis Data**:
```bash
# Create Redis backup
docker exec swlor_redis_1 redis-cli BGSAVE

# Copy backup files
docker cp swlor_redis_1:/data/dump.rdb ./backups/
```

**Database Backup**:
- Implement automated database backups
- Test restore procedures
- Store backups securely

### 2. Disaster Recovery

**Recovery Procedures**:
1. Stop all services
2. Restore from backup
3. Verify data integrity
4. Restart services
5. Test functionality

**Documentation**:
- Maintain recovery runbooks
- Test procedures regularly
- Update documentation after changes

---

*This documentation should be updated when deployment procedures change or new environments are added.* 