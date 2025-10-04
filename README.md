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
- Login once through OIDC to create a user
- Visit `/swagger` to promote your user to admin

## Floor Plans
OfficePlanner creates rooms and desks from floor plans automatically when an admin uploads a floor plan svg.
To declare a desk in a room, add the `data-op-desk` and `data-op-room` attributes with the respective names to a `rect`.
The svg is rendered in-tree, so exports from program's like Inkscape might need minor adjustments to remove xml namespace declarations.

## Security
The floor plan svgs are rendered in-tree.
Do not upload floor plans from untrusted sources.

## Development
Create `appsettings.json` as shown above in `./OfficePlanner/`.

If you want to develop OfficePlanner locally, use [`compose.pg.yml`](./compose.pg.yml) to start postgres in Docker.
In `./OfficePlanner/appsettings.json`, set `DbConnectionString` to `"Host=127.0.0.1;Database=OfficePlanner;Username=docker;Password=docker"`.

If you want to develop OfficePlanner in a container, use [`compose.dev.yml`](./compose.dev.yml) to start OfficePlanner and postgres in Docker.
Start it with `sudo docker compose up --build --watch` and it will hot-reload your changes.
**If you don't use sudo and your user does not have privileges to access `./data/`, [docker compose v2.39.4 will freeze and not start your container](https://github.com/docker/compose/issues/13262)**.
