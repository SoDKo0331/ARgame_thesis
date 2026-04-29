# Nomad Adventure Backend

Production-minded Node.js backend for the Unity project. It provides guest login, tourism spot data, reward claiming, and user reward history on top of PostgreSQL with Prisma.

## Stack

- Node.js 22+
- Express
- Prisma
- PostgreSQL
- Zod validation

## Folder Structure

```text
backend/
  prisma/
    migrations/
      202603190001_init/
        migration.sql
    migration_lock.toml
    schema.prisma
    seed.js
  postman/
    Nomad Adventure Backend.postman_collection.json
  src/
    config/
      env.js
    controllers/
      auth.controller.js
      spot.controller.js
      user.controller.js
    lib/
      prisma.js
    middleware/
      errorHandler.js
      notFound.js
    routes/
      auth.routes.js
      spot.routes.js
      user.routes.js
    utils/
      apiError.js
      asyncHandler.js
      serializers.js
    app.js
    server.js
  .env.example
  package.json
  README.md
```

## Local Setup

1. Create a PostgreSQL database.

Example:

```sql
CREATE DATABASE nomad_adventure;
```

2. Go into the backend folder.

```bash
cd /Users/dmtmobiledev/development/Test/Nomad\ Adventure/backend
```

3. Install dependencies.

```bash
npm install
```

4. Create a `.env` file from the example.

```bash
cp .env.example .env
```

5. Update `DATABASE_URL` in `.env`.

Example:

```env
DATABASE_URL="postgresql://your_db_user@localhost:5432/nomad_adventure?schema=public"
```

If you installed PostgreSQL locally with Homebrew on macOS, `your_db_user` is often your macOS username.

6. Generate the Prisma client.

```bash
npm run prisma:generate
```

7. Apply the checked-in migration.

```bash
npm run prisma:deploy
```

The nearby-spot migration `20260421000140_add_postgis_nearby_support` requires the
`postgis` extension to be installed for the PostgreSQL server you are using. If
`CREATE EXTENSION postgis` fails, install the matching PostGIS package for that
PostgreSQL instance and rerun `npm run prisma:deploy`.

For local iteration, you can also create a new development migration:

```bash
npm run prisma:migrate -- --name init
```

If you only want a quick local prototype without migration files, you can use this instead:

```bash
npm run prisma:push
```

8. Seed the database.

```bash
npm run prisma:seed
```

9. Start the API server.

```bash
npm run dev
```

The server will run at:

```text
http://localhost:4000
```

## API Endpoints

### `POST /auth/guest-login`

Guest login creates or reuses a user based on `deviceId`.

Request:

```json
{
  "deviceId": "ios-device-001",
  "displayName": "Guest UB"
}
```

Response:

```json
{
  "user": {
    "id": "cm9example1",
    "deviceId": "ios-device-001",
    "displayName": "Guest UB",
    "lastLoginAt": "2026-03-19T08:00:00.000Z",
    "createdAt": "2026-03-19T08:00:00.000Z",
    "updatedAt": "2026-03-19T08:00:00.000Z"
  },
  "isNewUser": true
}
```

### `GET /spots`

Returns active tourism spots and their reward metadata.

Response:

```json
{
  "spots": [
    {
      "id": "cm9spot1",
      "name": "Sukhbaatar Square",
      "description": "The civic heart of Ulaanbaatar and a natural starting point for city exploration.",
      "latitude": 47.918467,
      "longitude": 106.917701,
      "radiusMeters": 120,
      "isActive": true,
      "createdAt": "2026-03-19T08:00:00.000Z",
      "updatedAt": "2026-03-19T08:00:00.000Z",
      "reward": {
        "id": "cm9reward1",
        "name": "Blue Sky Silk Scarf",
        "description": "A symbolic scarf inspired by Mongolian hospitality and heritage.",
        "imageUrl": null,
        "createdAt": "2026-03-19T08:00:00.000Z",
        "updatedAt": "2026-03-19T08:00:00.000Z"
      }
    }
  ]
}
```

### `GET /spots/:id`

Returns one tourism spot and its reward.

Response:

```json
{
  "spot": {
    "id": "cm9spot1",
    "name": "Sukhbaatar Square",
    "description": "The civic heart of Ulaanbaatar and a natural starting point for city exploration.",
    "latitude": 47.918467,
    "longitude": 106.917701,
    "radiusMeters": 120,
    "isActive": true,
    "createdAt": "2026-03-19T08:00:00.000Z",
    "updatedAt": "2026-03-19T08:00:00.000Z",
    "reward": {
      "id": "cm9reward1",
      "name": "Blue Sky Silk Scarf",
      "description": "A symbolic scarf inspired by Mongolian hospitality and heritage.",
      "imageUrl": null,
      "createdAt": "2026-03-19T08:00:00.000Z",
      "updatedAt": "2026-03-19T08:00:00.000Z"
    }
  }
}
```

### `POST /spots/:id/claim`

Claims the reward for a user at a tourism spot. Repeating the same request for the same `userId` and `spotId` is idempotent.

Request:

```json
{
  "userId": "cm9user1"
}
```

Response:

```json
{
  "claim": {
    "id": "cm9claim1",
    "userId": "cm9user1",
    "claimedAt": "2026-03-19T08:30:00.000Z",
    "reward": {
      "id": "cm9reward1",
      "name": "Blue Sky Silk Scarf",
      "description": "A symbolic scarf inspired by Mongolian hospitality and heritage.",
      "imageUrl": null,
      "createdAt": "2026-03-19T08:00:00.000Z",
      "updatedAt": "2026-03-19T08:00:00.000Z"
    },
    "tourismSpot": {
      "id": "cm9spot1",
      "name": "Sukhbaatar Square",
      "description": "The civic heart of Ulaanbaatar and a natural starting point for city exploration.",
      "latitude": 47.918467,
      "longitude": 106.917701,
      "radiusMeters": 120,
      "isActive": true,
      "createdAt": "2026-03-19T08:00:00.000Z",
      "updatedAt": "2026-03-19T08:00:00.000Z",
      "reward": {
        "id": "cm9reward1",
        "name": "Blue Sky Silk Scarf",
        "description": "A symbolic scarf inspired by Mongolian hospitality and heritage.",
        "imageUrl": null,
        "createdAt": "2026-03-19T08:00:00.000Z",
        "updatedAt": "2026-03-19T08:00:00.000Z"
      }
    }
  },
  "alreadyClaimed": false
}
```

Repeat claim response:

```json
{
  "claim": {
    "id": "cm9claim1",
    "userId": "cm9user1",
    "claimedAt": "2026-03-19T08:30:00.000Z",
    "reward": {
      "id": "cm9reward1",
      "name": "Blue Sky Silk Scarf",
      "description": "A symbolic scarf inspired by Mongolian hospitality and heritage.",
      "imageUrl": null,
      "createdAt": "2026-03-19T08:00:00.000Z",
      "updatedAt": "2026-03-19T08:00:00.000Z"
    },
    "tourismSpot": {
      "id": "cm9spot1",
      "name": "Sukhbaatar Square",
      "description": "The civic heart of Ulaanbaatar and a natural starting point for city exploration.",
      "latitude": 47.918467,
      "longitude": 106.917701,
      "radiusMeters": 120,
      "isActive": true,
      "createdAt": "2026-03-19T08:00:00.000Z",
      "updatedAt": "2026-03-19T08:00:00.000Z",
      "reward": {
        "id": "cm9reward1",
        "name": "Blue Sky Silk Scarf",
        "description": "A symbolic scarf inspired by Mongolian hospitality and heritage.",
        "imageUrl": null,
        "createdAt": "2026-03-19T08:00:00.000Z",
        "updatedAt": "2026-03-19T08:00:00.000Z"
      }
    }
  },
  "alreadyClaimed": true
}
```

### `GET /users/:id/rewards`

Returns all claimed rewards for a user.

Response:

```json
{
  "userId": "cm9user1",
  "rewards": [
    {
      "id": "cm9claim1",
      "userId": "cm9user1",
      "claimedAt": "2026-03-19T08:30:00.000Z",
      "reward": {
        "id": "cm9reward1",
        "name": "Blue Sky Silk Scarf",
        "description": "A symbolic scarf inspired by Mongolian hospitality and heritage.",
        "imageUrl": null,
        "createdAt": "2026-03-19T08:00:00.000Z",
        "updatedAt": "2026-03-19T08:00:00.000Z"
      },
      "tourismSpot": {
        "id": "cm9spot1",
        "name": "Sukhbaatar Square",
        "description": "The civic heart of Ulaanbaatar and a natural starting point for city exploration.",
        "latitude": 47.918467,
        "longitude": 106.917701,
        "radiusMeters": 120,
        "isActive": true,
        "createdAt": "2026-03-19T08:00:00.000Z",
        "updatedAt": "2026-03-19T08:00:00.000Z",
        "reward": {
          "id": "cm9reward1",
          "name": "Blue Sky Silk Scarf",
          "description": "A symbolic scarf inspired by Mongolian hospitality and heritage.",
          "imageUrl": null,
          "createdAt": "2026-03-19T08:00:00.000Z",
          "updatedAt": "2026-03-19T08:00:00.000Z"
        }
      }
    }
  ]
}
```

## Error Handling

All errors return JSON in this shape:

```json
{
  "error": {
    "message": "Validation failed",
    "details": {}
  }
}
```

Common status codes:

- `200` success
- `201` created
- `400` bad request / validation error
- `404` not found
- `500` server error

## Postman And Curl

Postman collection:

- [Nomad Adventure Backend.postman_collection.json](/Users/dmtmobiledev/development/Test/Nomad Adventure/backend/postman/Nomad Adventure Backend.postman_collection.json)

Quick curl examples:

Guest login:

```bash
curl -sS -X POST http://localhost:4000/auth/guest-login \
  -H 'Content-Type: application/json' \
  -d '{"deviceId":"ios-device-001","displayName":"Guest UB"}'
```

Get spots:

```bash
curl -sS http://localhost:4000/spots
```

Get one spot:

```bash
curl -sS http://localhost:4000/spots/<spot-id>
```

Claim reward:

```bash
curl -sS -X POST http://localhost:4000/spots/<spot-id>/claim \
  -H 'Content-Type: application/json' \
  -d '{"userId":"<user-id>"}'
```

Get user rewards:

```bash
curl -sS http://localhost:4000/users/<user-id>/rewards
```

## Unity Integration Proposal

Unity should treat the backend as the source of truth for users, spots, and reward claims.

### Suggested Unity Flow

1. App launch:
   - call `POST /auth/guest-login`
   - store returned `user.id`
2. Main map scene:
   - call `GET /spots`
   - match backend spots against local GPS proximity checks, or replace local static spot data later
3. When player enters a spot:
   - use the backend spot `id` for the active location
4. When chest is opened:
   - call `POST /spots/:id/claim` with the logged-in `userId`
5. Inventory / profile screen:
   - call `GET /users/:id/rewards`

### Suggested Unity Requests

Guest login:

```json
{
  "deviceId": "ios-unique-device-id",
  "displayName": "Guest Player"
}
```

Claim reward:

```json
{
  "userId": "cm9user1"
}
```

### Suggested Unity Usage of Responses

- Save `user.id` locally after guest login.
- Save the `/spots` response in memory for spot IDs, names, coordinates, radius, and reward data.
- On claim success, show `claim.reward.name` and `claim.reward.description` in the AR reward popup.
- If `/spots/:id/claim` returns `alreadyClaimed: true`, treat it as a successful repeat claim and keep the reward UI consistent.

## Notes

- Unity files were left untouched.
- The backend is ready for Prisma migrations and local PostgreSQL use.
- If you want next, the Unity project can be updated with API client stubs that call these endpoints.
