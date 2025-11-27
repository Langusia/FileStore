# Deployment Guide

## Production Deployment Checklist

### Infrastructure Requirements

1. **Database**
   - SQL Server 2019 or later
   - Minimum 10GB storage for metadata
   - Regular backups configured
   - Connection pooling enabled

2. **SMB Storage**
   - Hot storage: High-performance SSD/NVMe
   - Cold storage: High-capacity HDD or archive storage
   - Redundancy: RAID or distributed filesystem
   - Backup strategy: Snapshots and/or replication
   - ACL/permissions configured

3. **Application Server**
   - .NET 8.0 Runtime
   - Minimum 4GB RAM
   - Network connectivity to SQL Server and SMB shares

### SMB Mount Configuration

#### Linux (mount SMB share)
```bash
# Install cifs-utils
sudo apt-get install cifs-utils

# Create mount points
sudo mkdir -p /mnt/storage/hot
sudo mkdir -p /mnt/storage/cold

# Create credentials file
sudo nano /etc/smbcredentials
```

Add to `/etc/smbcredentials`:
```
username=storage_user
password=YourSecurePassword
```

```bash
sudo chmod 600 /etc/smbcredentials

# Add to /etc/fstab for persistent mounts
//fileserver/hot /mnt/storage/hot cifs credentials=/etc/smbcredentials,uid=www-data,gid=www-data,file_mode=0660,dir_mode=0770 0 0
//fileserver/cold /mnt/storage/cold cifs credentials=/etc/smbcredentials,uid=www-data,gid=www-data,file_mode=0660,dir_mode=0770 0 0

# Mount all
sudo mount -a

# Verify
df -h | grep storage
```

#### Windows (map network drive)
```powershell
# Map drives with persistent credentials
net use H: \\fileserver\hot /persistent:yes
net use C: \\fileserver\cold /persistent:yes
```

Update `appsettings.Production.json`:
```json
{
  "Storage": {
    "HotRootPath": "H:\\",
    "ColdRootPath": "C:\\"
  }
}
```

### Database Deployment

1. **Create database and user**:
```sql
CREATE DATABASE FileStore;
GO

CREATE LOGIN filestore_app WITH PASSWORD = 'StrongPassword123!';
GO

USE FileStore;
GO

CREATE USER filestore_app FOR LOGIN filestore_app;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO filestore_app;
GO
```

2. **Run schema script**:
```bash
sqlcmd -S production-sql-server -U sa -P AdminPassword -d FileStore -i database/schema.sql
```

3. **Verify tables**:
```sql
USE FileStore;
SELECT * FROM INFORMATION_SCHEMA.TABLES;
```

### Application Configuration

1. **Create production appsettings**:

`appsettings.Production.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "FileStore": "Information"
    }
  },
  "Database": {
    "ConnectionString": "Server=production-sql-server;Database=FileStore;User Id=filestore_app;Password=StrongPassword123!;TrustServerCertificate=False;Encrypt=True"
  },
  "Storage": {
    "Backend": "SMB",
    "HotRootPath": "/mnt/storage/hot",
    "ColdRootPath": "/mnt/storage/cold",
    "Shard": {
      "Levels": 3,
      "CharsPerShard": 2
    },
    "MaxFileSizeMb": 100,
    "EnableRangeReads": true,
    "AllowedContentTypes": [
      "application/pdf",
      "image/jpeg",
      "image/png",
      "image/gif",
      "application/vnd.ms-excel",
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      "application/msword",
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    ]
  },
  "Tiering": {
    "Enabled": true,
    "IntervalMinutes": 60,
    "ColdAfterDays": 365,
    "BatchSize": 100,
    "BucketsExcludedFromCold": [
      "audit-logs",
      "compliance-documents"
    ]
  }
}
```

2. **Build for production**:
```bash
dotnet publish src/FileStore.API/FileStore.API.csproj \
  -c Release \
  -o /var/www/filestore \
  --runtime linux-x64 \
  --self-contained false
```

3. **Set environment**:
```bash
export ASPNETCORE_ENVIRONMENT=Production
```

### Systemd Service (Linux)

Create `/etc/systemd/system/filestore.service`:

```ini
[Unit]
Description=FileStore API Service
After=network.target

[Service]
Type=notify
User=www-data
Group=www-data
WorkingDirectory=/var/www/filestore
ExecStart=/usr/bin/dotnet /var/www/filestore/FileStore.API.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=filestore
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl daemon-reload
sudo systemctl enable filestore
sudo systemctl start filestore
sudo systemctl status filestore
```

View logs:
```bash
sudo journalctl -u filestore -f
```

### Nginx Reverse Proxy

`/etc/nginx/sites-available/filestore`:

```nginx
upstream filestore_backend {
    server localhost:5000;
}

server {
    listen 80;
    server_name filestore.yourdomain.com;

    # Redirect to HTTPS
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name filestore.yourdomain.com;

    ssl_certificate /etc/ssl/certs/filestore.crt;
    ssl_certificate_key /etc/ssl/private/filestore.key;

    client_max_body_size 100M;

    location / {
        proxy_pass http://filestore_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # Timeout settings for large file uploads
        proxy_connect_timeout 600;
        proxy_send_timeout 600;
        proxy_read_timeout 600;
        send_timeout 600;
    }
}
```

Enable and restart:
```bash
sudo ln -s /etc/nginx/sites-available/filestore /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### Health Checks

Add health check endpoint in `Program.cs`:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
```

Test:
```bash
curl https://filestore.yourdomain.com/health
```

### Monitoring

1. **Application Insights** (recommended for Azure)
2. **Prometheus + Grafana**
3. **ELK Stack** (Elasticsearch, Logstash, Kibana)

Add Serilog for structured logging:

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.File
```

### Security Hardening

1. **Enable HTTPS only**
2. **Use strong passwords** for database and SMB
3. **Restrict network access** with firewall rules
4. **Regular security updates**
5. **Enable audit logging**
6. **Implement authentication/authorization** (add JWT or similar)

### Backup Strategy

1. **Database**: Daily automated backups with point-in-time recovery
2. **SMB Storage**:
   - Snapshots every 6 hours
   - Daily full backups
   - Monthly archive backups
3. **Test restore procedures** quarterly

### Performance Tuning

1. **Database**:
   - Index tuning based on query patterns
   - Connection pooling
   - Query optimization

2. **Storage**:
   - Monitor I/O performance
   - Adjust sharding if needed (only for new files)
   - Consider SSD for hot tier

3. **Application**:
   - Enable response compression
   - Configure appropriate timeouts
   - Monitor memory usage

### Migration from Database BLOB Storage

Script to migrate existing files:

```sql
-- Example migration query
SELECT
    DocumentId,
    FileData,
    ContentType,
    FileName,
    Channel,
    Operation,
    EntityId
FROM LegacyDocuments
WHERE MigratedToFileStore = 0
ORDER BY CreatedDate
OFFSET 0 ROWS FETCH NEXT 100 ROWS ONLY;
```

Implement migration tool to:
1. Read from legacy table
2. Upload to FileStore API
3. Store ObjectId in legacy table
4. Mark as migrated
5. Verify integrity
6. Archive old BLOB data

### Troubleshooting

1. **Connection issues**:
   - Verify SMB mounts: `mount | grep storage`
   - Test SQL connection: `sqlcmd -S server -U user -P pass`

2. **Permission issues**:
   - Check file ownership: `ls -la /mnt/storage/hot`
   - Verify process user: `ps aux | grep FileStore`

3. **Performance issues**:
   - Check disk I/O: `iostat -x 1`
   - Monitor CPU/memory: `htop`
   - Review logs: `journalctl -u filestore`

4. **Storage issues**:
   - Check disk space: `df -h`
   - Verify write permissions
   - Test file creation manually

### Rollback Plan

1. Keep previous deployment artifacts
2. Document configuration changes
3. Database backup before schema changes
4. Ability to switch back to legacy system
5. Communication plan for downtime
