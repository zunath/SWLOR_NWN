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

The main deployment configuration is defined in `Docker/docker-compose.yml`. This file configures:

- **Redis**: Caching and data storage using `redislabs/redismod:latest`
- **Redis Commander**: Web interface for Redis management
- **SWLOR Server**: Main game server using `zunath/nwn-dotnet:8193.37.3-1` image
- **InfluxDB**: Time-series database for metrics storage
- **Grafana**: Monitoring and visualization dashboard

The configuration includes proper networking, volume mounts, and environment variable setup.

### 2. Environment Configuration

Environment variables are configured in `Docker/swlor.env`. Key variables include:

- `SWLOR_ENVIRONMENT`: Controls server environment (development, test, production)
- `SWLOR_APP_LOG_DIRECTORY`: Log file location
- `NWNX_REDIS_HOST`: Redis connection string
- Database and Discord integration settings

### 3. Application Settings

The application uses `ApplicationSettings.cs` to manage configuration. This class:

- Reads environment variables for configuration
- Detects server environment automatically
- Provides centralized access to application settings
- Handles environment-specific behavior

## Deployment Environments

### 1. Development Environment

**Purpose**: Local development and testing

**Configuration**:
- Environment: `development`
- Log directory: `./logs`
- Redis host: `localhost:6379`

**Features**:
- Detailed logging enabled
- Debug information available
- Debug symbols and stack traces
- Test data available

### 2. Test Environment

**Purpose**: Staging and testing before production

**Configuration**:
- Environment: `test`
- Log directory: `/app/logs`
- Redis host: `redis:6379`

**Features**:
- Production-like configuration
- Test data and scenarios
- Performance monitoring
- Integration testing

### 3. Production Environment

**Purpose**: Live game server

**Configuration**:
- Environment: `production`
- Log directory: `/app/logs`
- Redis host: `redis:6379`

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

Logging is handled by Serilog with multiple sinks. The configuration is defined in the application code and supports:

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

# Deploy with production config
docker-compose -f docker-compose.prod.yml up -d
```

### 3. Environment-Specific Configurations

Environment-specific configurations can be created by extending the base `docker-compose.yml` file. See the Docker directory for examples of development and production configurations.

## Service Components

### 1. Redis

**Purpose**: Caching and data storage

**Configuration**: Defined in `Docker/docker-compose.yml`

**Usage**: The application connects to Redis using the connection string specified in environment variables.

### 2. InfluxDB

**Purpose**: Time-series data storage for metrics

**Configuration**: Defined in `Docker/docker-compose.yml`

**Features**:
- Database: `nwn`
- Admin user and password configured
- UDP support enabled for metrics collection

### 3. Grafana

**Purpose**: Monitoring and visualization

**Configuration**: Defined in `Docker/docker-compose.yml`

**Access**: Web interface available on port 3000 with admin credentials

## Security Considerations

### 1. Network Security

**Port Configuration**:
- `5121/udp` - NWN server port
- `3000` - Grafana web interface
- `8081` - Redis Commander
- `8086` - InfluxDB HTTP API
- `8089` - InfluxDB UDP

**Firewall Rules**: Configure firewall to allow only necessary ports and restrict external access to monitoring interfaces.

### 2. Data Security

**Encryption**:
- Use TLS for external connections
- Encrypt sensitive data in Redis
- Secure database connections

**Access Control**:
- Use strong passwords for admin accounts
- Implement role-based access control
- Regular security audits

### 3. Container Security

**Best Practices**:
- Use minimal base images
- Run containers as non-root users
- Regularly update container images
- Scan images for vulnerabilities

## Performance Optimization

### 1. Resource Allocation

Configure resource limits and reservations in the Docker Compose file:

**Memory**: Set appropriate memory limits for each service
**CPU**: Configure CPU limits and reservations
**Storage**: Use appropriate volume configurations

### 2. Caching Strategy

**Redis Caching**: The application uses Redis for caching frequently accessed data
**Application Caching**: In-memory caching for session data and temporary storage

### 3. Database Optimization

**Connection Pooling**: Configure appropriate connection pool sizes
**Query Optimization**: Use indexed queries and optimize database operations

## Backup and Recovery

### 1. Data Backup

**Automated Backups**: Implement automated backup scripts for Redis and application data
**Database Backup**: Regular backups of application data and configuration

### 2. Recovery Procedures

**Redis Recovery**: Restore Redis data from backup files
**Application Recovery**: Restore application data and configuration from backups

## Troubleshooting

### 1. Common Issues

**Container Won't Start**: Check container logs and resource usage
**Connection Issues**: Verify network connectivity and service dependencies
**Performance Issues**: Monitor resource usage and application logs

### 2. Debugging

**Enable Debug Logging**: Set debug environment variables for detailed logging
**Access Container Shell**: Use Docker exec to access running containers
**Check Application Status**: Monitor application processes and logs

### 3. Monitoring

**Health Checks**: Configure health checks for all services
**Log Monitoring**: Monitor application and system logs for errors

## Scaling

### 1. Horizontal Scaling

**Multiple Instances**: Deploy multiple server instances with load balancing
**Load Balancing**: Use reverse proxy or load balancer for traffic distribution

### 2. Vertical Scaling

**Resource Scaling**: Increase memory and CPU allocation for services
**Database Scaling**: Optimize database performance and connection pooling

## Maintenance

### 1. Regular Maintenance

**Updates**: Regularly update container images and application code
**Cleanup**: Remove unused containers, images, and volumes

### 2. Monitoring

**Metrics Collection**: Monitor CPU, memory, network, and disk usage
**Alerting**: Set up alerts for resource usage and application errors

---

*This documentation should be updated when deployment configuration changes or new deployment processes are added.* 