# API Contract: Media Search & Lookup

This document describes the backend endpoints that let the client **search** for media and **look up** the details of one item. Both endpoints are read-only (`GET`). They proxy an external media provider. For movies, that provider is TMDB.

| Endpoint                           | Purpose                                              |
| ---------------------------------- | ---------------------------------------------------- |
| `GET /api/search/{type}?q={query}` | Search for media of a given type by title            |
| `GET /api/lookup/{type}/{id}`      | Get the details of one media item by its provider id |

- **Base URL (local dev):** `http://localhost:5251`
- **Auth:** none required from the client. The backend adds the TMDB token itself.
- **Content type:** responses are `application/json` on success and `application/problem+json` on error.
- **Field naming:** JSON properties are `camelCase`.
- **OpenAPI:** in the Development environment, the generated spec is served at `GET /openapi/v1.json`.

Source: `backend/Features/Search`, `backend/Features/Lookup`, `backend/Providers`, `backend/Shared`.

---

## Supported media types

The `{type}` path segment selects a **media provider**. Matching is case-insensitive and ignores surrounding whitespace, so `movie`, `Movie` and `MOVIE` all work.

| `type`  | Provider | `id` format                                      |
| ------- | -------- | ------------------------------------------------ |
| `movie` | TMDB     | A positive integer (TMDB movie id), e.g. `27205` |

Any other value returns `400 Unknown media type`. The error message lists the supported types. To add a new type (book, TV, game), implement `IMediaProvider` and register it. The resolver picks it up automatically, so the endpoints do not change.

---

## 1. Search media

```
GET /api/search/{type}?q={query}
```

Returns up to **10** matches for `query`, in the order the provider ranks them by relevance. Only the first page of provider results is used.

### Inputs

| Name   | In    | Type   | Required | Rules                                                                                                                           |
| ------ | ----- | ------ | -------- | ------------------------------------------------------------------------------------------------------------------------------- |
| `type` | path  | string | yes      | A supported media type (see above)                                                                                              |
| `q`    | query | string | yes      | Must not be empty or whitespace-only. The value is trimmed, and the trimmed length must be **≤ 200** characters. URL-encode it. |

### Success: `200 OK`

```jsonc
{
  "results": [
    {
      "externalId": "27205", // string: provider's id, pass it to the lookup endpoint
      "mediaType": "movie", // string: always matches the requested type
      "title": "Inception", // string: never null
      "releaseDate": "2010-07-15", // string "YYYY-MM-DD" or null if unknown
      "posterUrl": "https://image.tmdb.org/t/p/w500/<poster>.jpg", // full URL, or null if no poster
    },
  ],
}
```

**No matches is not an error.** The endpoint returns `200` with `{ "results": [] }`.

### Errors

| Status | `title`                | When                                                                             |
| ------ | ---------------------- | -------------------------------------------------------------------------------- |
| `400`  | `Invalid request`      | `q` is missing, empty or whitespace (`"Query parameter 'q' must not be empty."`) |
| `400`  | `Invalid request`      | Trimmed `q` is longer than 200 characters                                        |
| `400`  | `Unknown media type`   | `type` has no registered provider                                                |
| `502`  | `Provider unavailable` | TMDB failed, timed out or returned an unexpected body                            |

The query is validated **before** the type. A request with an unsupported type and an empty `q` therefore reports the `q` error first.

### Examples

```bash
curl "http://localhost:5251/api/search/movie?q=inception"
```

```bash
# Empty query → 400
curl -i "http://localhost:5251/api/search/movie?q=%20%20"
```

```json
{
  "title": "Invalid request",
  "status": 400,
  "detail": "Query parameter 'q' must not be empty.",
  "traceId": "0HN7...:00000001"
}
```

```bash
# Unsupported type → 400
curl -i "http://localhost:5251/api/search/book?q=dune"
```

```json
{
  "title": "Unknown media type",
  "status": 400,
  "detail": "Media type 'book' is not supported. Supported: movie.",
  "traceId": "0HN7...:00000002"
}
```

---

## 2. Look up one media item

```
GET /api/lookup/{type}/{id}
```

Returns the full details for one item. The usual flow is to call **search**, then pass a result's `externalId` here.

### Inputs

| Name   | In   | Type   | Required | Rules                                                                                                                                                                                |
| ------ | ---- | ------ | -------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `type` | path | string | yes      | A supported media type                                                                                                                                                               |
| `id`   | path | string | yes      | Must be valid for that provider. For `movie`, it must be a **positive integer** containing digits only: no sign, decimal point or letters. Leading zeros are accepted (`007` → `7`). |

### Success: `200 OK` (movie)

```jsonc
{
  "externalId": "27205", // string
  "title": "Inception", // string: never null
  "releaseDate": "2010-07-15", // "YYYY-MM-DD" or null
  "description": "Cobb, a skilled thief who ...", // string or null (blank overviews become null)
  "posterUrl": "https://image.tmdb.org/t/p/w500/<poster>.jpg", // full URL or null
  "runtime": 148, // minutes (int) or null; TMDB's "0 = unknown" becomes null
}
```

The fields shared by all media types are `externalId`, `title`, `releaseDate`, `description` and `posterUrl`. Each media type can add its own fields. For movies, the extra field is `runtime`.

### Errors

| Status | `title`                | When                                                                                                                           |
| ------ | ---------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| `400`  | `Invalid request`      | `id` is malformed for the provider, e.g. `abc`, `-5`, `0`, `1.5`, or too large for an int (`"'abc' is not a valid movie id."`) |
| `400`  | `Unknown media type`   | `type` has no registered provider                                                                                              |
| `404`  | `Media not found`      | The id is well-formed but the provider has no such item (`"No movie found with id '999999999'."`)                              |
| `502`  | `Provider unavailable` | TMDB failed, timed out or returned an unexpected body                                                                          |

A malformed id returns **400**, not 404. TMDB itself answers 404 for both a malformed id and a missing one, so the backend validates the format first to tell these two cases apart.

### Examples

```bash
curl "http://localhost:5251/api/lookup/movie/27205"
```

```bash
# Malformed id → 400
curl -i "http://localhost:5251/api/lookup/movie/abc"
```

```json
{
  "title": "Invalid request",
  "status": 400,
  "detail": "'abc' is not a valid movie id.",
  "traceId": "0HN7...:00000003"
}
```

```bash
# Well-formed but non-existent id → 404
curl -i "http://localhost:5251/api/lookup/movie/999999999"
```

```json
{
  "title": "Media not found",
  "status": 404,
  "detail": "No movie found with id '999999999'.",
  "traceId": "0HN7...:00000004"
}
```

---

## Error format (both endpoints)

Every error that the API reports on purpose is an [RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457) object with content type `application/problem+json`:

| Field     | Type   | Meaning                                                      |
| --------- | ------ | ------------------------------------------------------------ |
| `status`  | int    | Same value as the HTTP status code                           |
| `title`   | string | Short, stable name of the problem. Clients can branch on it. |
| `detail`  | string | Human-readable explanation that is safe to show to users     |
| `traceId` | string | Request id. Search the backend logs for it when debugging.   |

| Status | Exception (backend)                                    | Meaning for the client                                                                                                      |
| ------ | ------------------------------------------------------ | --------------------------------------------------------------------------------------------------------------------------- |
| `400`  | `InvalidRequestException`, `UnknownMediaTypeException` | Fix the request. Retrying unchanged will not help.                                                                          |
| `404`  | `MediaNotFoundException`                               | The item does not exist                                                                                                     |
| `502`  | `ProviderUnavailableException`                         | Upstream problem. Show a "try again later" message. Provider details are logged on the server but never sent to the client. |
| `500`  | any other exception                                    | Unexpected backend bug, returned as a generic Problem Details body                                                          |

All error mapping happens in one place, `Shared/Errors/ApiExceptionHandler.cs`. Services throw an `ApiException` subclass, and the handler converts it into the response.

---

## Configuration these endpoints depend on

Both endpoints need TMDB settings under the `Tmdb` section. The backend fails at startup if any setting is missing or invalid.

| Key                    | Where it's set                  | Notes                                                          |
| ---------------------- | ------------------------------- | -------------------------------------------------------------- |
| `Tmdb:BaseUrl`         | `appsettings.json`              | `https://api.themoviedb.org/3/` (must end with `/`)            |
| `Tmdb:ImageBaseUrl`    | `appsettings.json`              | `https://image.tmdb.org/t/p/w500`, used to build `posterUrl`   |
| `Tmdb:ReadAccessToken` | **user-secrets / env var only** | TMDB v4 read access token. Never commit it.                    |
| `Tmdb:TimeoutSeconds`  | optional                        | Default `10`, allowed range 1–120. Exceeding it gives a `502`. |

```bash
cd backend
dotnet user-secrets set "Tmdb:ReadAccessToken" "<your-token>"
# or: export Tmdb__ReadAccessToken="<your-token>"
```

---

## Known limitations

- `movie` is the only supported type for now.
- Search returns at most 10 results and has no pagination.
- Lookup responses do **not** include a `mediaType` field. The frontend type `MovieLookupResult` (`frontend/src/types/movieLookupResult.ts`) declares one, so on the client that value is `undefined`.
- The example values above (poster paths, trace ids, overview text) are abbreviated.
