# Trinetra Deployment Guide
## Complete Setup & Deployment Instructions

**Version:** 1.0  
**Environments:** Local Development, Staging, Production

---

## PREREQUISITES

### System Requirements
```
CPU:                4+ cores
RAM:                8GB minimum (production: 32GB)
Disk:               50GB SSD (scalable)
OS:                 Ubuntu 20.04+ or equivalent
```

### Required Software
```
Docker:             24.0+
Docker Compose:     2.0+
.NET SDK:           8.0+
Node.js:            18+ OR Angular 17+
Git:                2.30+
PostgreSQL CLI:     psql 14+
Terraform:          1.0+ (production)
```

---

## LOCAL DEVELOPMENT SETUP

### Step 1: Clone Repository
```bash
git clone https://github.com/company/trinetra.git
cd trinetra
```

### Step 2: Configure Environment
```bash
# Copy example .env file
cp .env.example .env

# Edit .env with local values
nano .env

# Minimum required variables:
DATABASE_URL=postgresql://postgres:password@localhost:5432/trinetra_dev
REDIS_URL=redis://localhost:6379
RABBITMQ_URL=amqp://guest:guest@localhost:5672/
CLAUDE_API_KEY=sk-ant-your-key-here
```

### Step 3: Start Services
```bash
# Start all Docker containers
docker-compose up -d

# Wait for services to be ready (30-60 sec)
docker-compose ps

# Check logs if any service fails
docker-compose logs -f postgres
```

### Step 4: Initialize Database
```bash
# Run migrations
dotnet ef database update

# Seed initial data
psql -U postgres -d trinetra_dev < migrations/seed_data.sql
```

### Step 5: Start Backend
```bash
# Restore packages
dotnet restore

# Run backend
dotnet run --project src/Trinetra.API
# API will be available at http://localhost:5000
```

### Step 6: Start Frontend
```bash
# In another terminal
cd src/frontend

# Install dependencies
npm install

# Start development server
npm start
# Frontend will be available at http://localhost:3000
```

### Step 7: Verify Setup
```bash
# Check API health
curl http://localhost:5000/health

# Check frontend
open http://localhost:3000

# View logs
docker-compose logs -f elasticsearch
```

---

## DOCKER COMPOSE CONFIGURATION

### Services Configuration
```yaml
services:
  postgres:
    image: postgres:15-alpine
    environment:
      POSTGRES_DB: trinetra_dev
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"
  
  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
  
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:8.0.0
    environment:
      discovery.type: single-node
      xpack.security.enabled: false
    ports:
      - "9200:9200"
  
  rabbitmq:
    image: rabbitmq:3-management
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    ports:
      - "5672:5672"
      - "15672:15672"
```

---

## KUBERNETES DEPLOYMENT (PRODUCTION)

### Prerequisites
```
Kubernetes cluster (GKE, EKS, AKS)
Helm 3+
kubectl configured
```

### Deploy with Helm
```bash
# Create namespace
kubectl create namespace trinetra

# Install Trinetra
helm install trinetra ./helm/trinetra \
  --namespace trinetra \
  --values helm/values-prod.yaml

# Verify deployment
kubectl get pods -n trinetra
kubectl logs -f deployment/trinetra-api -n trinetra
```

### Scale Replicas
```bash
# Scale API servers
kubectl scale deployment trinetra-api --replicas=5 -n trinetra

# Scale agent workers
kubectl scale deployment trinetra-agent-worker --replicas=3 -n trinetra
```

---

## CONFIGURATION MANAGEMENT

### Environment Variables
```
API_PORT=5000
DATABASE_URL=postgresql://user:pass@host:5432/trinetra
REDIS_URL=redis://host:6379
RABBITMQ_URL=amqp://guest:guest@host:5672/
CLAUDE_API_KEY=sk-ant-xxxx
LOG_LEVEL=Information
```

### Database Configuration
```
Max connections: 100
Shared buffers: 256MB (dev) / 4GB (prod)
Effective cache: 1GB (dev) / 16GB (prod)
Connection timeout: 30 seconds
```

---

## BACKUP & RECOVERY

### Database Backup
```bash
# Full backup
pg_dump -Fc trinetra_prod > backup_$(date +%Y%m%d).dump

# Restore
pg_restore -d trinetra_prod backup_20260826.dump
```

---

## MONITORING SETUP

### Prometheus
```yaml
scrape_configs:
  - job_name: 'trinetra-api'
    static_configs:
      - targets: ['localhost:9090']
```

### Grafana Dashboards
```
Import dashboard: Trinetra Overview (ID: 12345)
Data source: Prometheus
Refresh interval: 30 seconds
```

---

## TROUBLESHOOTING

### Common Issues

**Database Connection Failed**
```bash
# Check PostgreSQL
docker-compose ps postgres
docker-compose logs postgres

# Test connection
psql -U postgres -d trinetra_dev
```

**Redis Connection Failed**
```bash
# Check Redis
redis-cli ping
# Should return PONG
```

**API Not Starting**
```bash
# Check logs
docker-compose logs -f api
dotnet run --project src/Trinetra.API
```

---

**See monitoring guide for detailed observability setup.**
