# Office Planner

## Deployment (Docker)
- Register a new OIDC client app, setting the direct URI to `/signin-oidc`
- Create `appsettings.json`
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database": "Warning"
    }
  },
  "OIDC": {
    "Authority": TODO,
    "ClientId": TODO,
    "ClientSecret": TODO
  },
  "DbConnectionString": "Host=postgres;Database=OfficePlanner;Username=docker;Password=docker",
  "AdminPassword": TODO
}
```
- Create `compose.yml`
```yaml
services:
  officeplanner:
    image: ghcr.io/trolldemorted/officeplanner/officeplanner:nightly
    restart: unless-stopped
    volumes:
      - "./appsettings.json:/app/appsettings.json:ro"
      - "./data/officeplanner:/root/.aspnet"
    ports:
      - "8080:8080"
  postgres:
    image: postgres
    restart: unless-stopped
    environment:
      POSTGRES_USER: docker
      POSTGRES_PASSWORD: docker
    volumes:
      - ./data/postgres:/var/lib/postgresql/data
    shm_size: 512MB
```
- `docker compose up -d`
