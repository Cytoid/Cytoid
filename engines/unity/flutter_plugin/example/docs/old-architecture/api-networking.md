# API & Networking

## HTTP Client

**Proyecto26.RestClient** — a Unity wrapper over `UnityEngine.Networking.UnityWebRequest`. Provides promise-based `Get`, `GetArray`, `Post`, `Put` methods with automatic JSON serialization via **Newtonsoft.Json**.

## Authentication

### Flow

1. **Sign Up**: `PUT /session?captcha=...` with `{ uid, email, password }`
2. **Sign In**: `POST /session` with `{ username, password, captcha }` → receives `Session { token, user }`
3. **Auto-login**: `GET /session?captcha=...` with `Authorization: JWT <token>` header → token is refreshed
4. **Token storage**: JWT token stored in `LocalPlayerSettings.LoginToken`, persisted in **LiteDB** (legacy used `SecuredPlayerPrefs`)
5. **Auth headers**: All authenticated requests include `Authorization: JWT <token>` and `Accept-Language`

### Validation Rules

- ID format: `^[a-z0-9-_]{3,16}$`
- Password: minimum 9 characters
- Email: standard format validation

## API Base URLs

| Region | API | Bundles | Website |
|--------|-----|---------|---------|
| International | `https://services.cytoid.io` | `https://artifacts.cytoid.io` | `https://cytoid.io` |
| Mainland China | `https://api.cytoid.cn` | `https://artifacts.cytoid.cn` | `https://cytoid.cn` |
| Debug | Configurable local URL | — | — |

Region auto-detection: `GET /ping` checks `countryCode`, falls back to CN if international is unreachable.

## API Endpoints

### Authentication

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| `POST` | `/session` | `{ username, password, captcha }` | `Session { token, user }` |
| `GET` | `/session?captcha=...` | — (Authorization header) | `Session { token, user }` |
| `PUT` | `/session?captcha=...` | `{ uid, email, password }` | `Session { token, user }` |

### User Profile

| Method | Endpoint | Response |
|--------|----------|----------|
| `GET` | `/profile/{id}` | `Profile` |
| `GET` | `/profile/{id}/details` | `FullProfile` (with levels, records, badges, character, tier) |
| `POST` | `/profile/{id}/character` | — (Body: `{ characterId }`) |

### Level Browsing

| Method | Endpoint | Query Params | Response |
|--------|----------|-------------|----------|
| `GET` | `/levels` | `search, sort, order, date_start, owner, page, limit, featured, qualified` | `OnlineLevel[]` |
| `GET` | `/search/levels` | `search, sort, order, date_start, owner, page, limit` | `OnlineLevel[]` (for relevance sort) |
| `GET` | `/levels/{id}` | — | `OnlineLevel` (full detail) |
| `POST` | `/levels/{id}/resources` | `{ captcha }` | `OnlineLevelResources { package }` |
| `GET` | `/library?granted=true` | — | `LibraryLevel[]` |

### Scores & Rankings

| Method | Endpoint | Response |
|--------|----------|----------|
| `POST` | `/levels/{id}/charts/{chartType}/records` | Score upload step 1: send transfer salt bytes → `{ key }` |
| `PUT` | `/levels/{id}/charts/{chartType}/records` | Score upload step 2: send encrypted payload → `OnlinePlayerStateChange` |
| `GET` | `/levels/{id}/charts/{chartType}/records?limit=10` | Top 10 `RankingEntry[]` |
| `GET` | `/levels/{id}/charts/{chartType}/records` | Full leaderboard `RankingEntry[]` |
| `GET` | `/levels/{id}/charts/{chartType}/user_ranking?limit=6` | Around-user `RankingEntry[]` |

### Level Ratings

| Method | Endpoint | Body | Response |
|--------|----------|------|----------|
| `GET` | `/levels/{id}/ratings` | — | `LevelRating` |
| `POST` | `/levels/{id}/ratings` | `{ rating }` (1–10) | `LevelRating` |

### Leaderboard

| Method | Endpoint | Query Params | Response |
|--------|----------|-------------|----------|
| `GET` | `/leaderboard` | `limit` (50), `user` | `Leaderboard.Entry[]` |

### Tiers / Seasons

| Method | Endpoint | Response |
|--------|----------|----------|
| `GET` | `/seasons/{seasonId}` | `SeasonMeta` |
| `POST` | `/seasons/{seasonId}/tiers/{tierId}/records` | Tier upload step 1: salt |
| `PUT` | `/seasons/{seasonId}/tiers/{tierId}/records` | Tier upload step 2: encrypted |
| `GET` | `/seasons/{seasonId}/tiers/{tierId}/records?limit=10` | Top 10 `TierRankingEntry[]` |
| `GET` | `/seasons/{seasonId}/tiers/{tierId}/user_ranking?limit=6` | Around-user `TierRankingEntry[]` |

### Events & Quests

| Method | Endpoint | Response |
|--------|----------|----------|
| `GET` | `/events` | `EventMeta[]` |
| `GET` | `/epics/adventures/{epicId}` | `AdventureState` |
| `GET` | `/epics/adventures/characters/{questId}` | `SingleQuestState` |

### Collections

| Method | Endpoint | Response |
|--------|----------|----------|
| `GET` | `/collections/{id}` | `CollectionMeta` |

### Characters

| Method | Endpoint | Response |
|--------|----------|----------|
| `GET` | `/characters/all` | `CharacterMeta[]` |
| `GET` | `/characters/{id}/exp` | `ExpData` |

### Misc

| Method | Endpoint | Response |
|--------|----------|----------|
| `GET` | `/announcements` | `Announcement { currentVersion, minSupportedVersion, message }` |
| `GET` | `/training` | `{ levels: OnlineLevel[] }` |
| `GET` | `/ping` | `RegionInfo` |
| `GET` | `/credits` | Credits page (web view) |

## Score Upload Protocol (3-Step Encrypted)

1. `POST` raw salt bytes to record endpoint → server returns `{ key }` (base64 public key)
2. Client computes HMAC-SHA256: `HMAC(publicKey, clientSecret + transferSalt + userId + chartContext)`
3. AES-encrypt the record JSON with derived private key → `PUT` encrypted bytes
4. Mobile uses AES-CBC with prepended IV; desktop uses AES-CTS

## Data Models (Online DTOs)

| Model | File | Key Fields |
|-------|------|------------|
| `OnlineUser` | `Online/OnlineUser.cs` | `{ id, uid, name, avatarURL, avatar: { original, small, large } }` |
| `Session` | `Online/Session.cs` | `{ token, user }` — JWT token + `OnlineUser` |
| `Profile` | `Online/Profile.cs` | `{ user, rating, exp: { currentLevel, totalExp }, grade: { MAX, SSS, ... }, activities: { totalRankedPlays, clearedNotes, maxCombo, averageRankedAccuracy, totalRankedScore, totalPlayTime } }` |
| `FullProfile` | `Online/Profile.cs` | Extends `Profile` with levels, collections, recent records, tier, character, badges, time series |
| `OnlineLevel` | `Online/OnlineLevel.cs` | `{ uid, version, date, title, metadata: { title_localized, artist, charter, illustrator }, bundle: { background, music, music_preview }, owner, charts: [{ type, name, difficulty, notesCount }], rating, plays, downloads, duration, size, description, tags, cover: { original, thumbnail, cover, stripe } }` |
| `OnlineLevelQuery` | `Online/OnlineLevelQuery.cs` | Sort (creation_date/relevance/modification_date/duration/difficulty), order, category (all/featured/qualified), time filter, search, owner, pagination |
| `OnlineLevelResources` | `Online/OnlineLevelResources.cs` | `{ package }` — download URL |
| `RankingEntry` | `Online/RankingEntry.cs` | `{ rank, score, accuracy (0–1), details: { perfect, great, good, bad, miss, maxCombo }, mods, owner, date }` |
| `TierRankingEntry` | `Online/TierRankingEntry.cs` | `{ rank, completion, averageAccuracy, health, maxCombo, owner, date }` |
| `OnlineRecord` | `Online/OnlineRecord.cs` | `{ accuracy, score, date, chart: { type, name, difficulty, notesCount, level }, owner }` |
| `UploadRecord` | `Online/UploadRecord.cs` | `{ score, accuracy, details: { perfect, great, good, bad, miss, maxCombo, info: { clientVersion, uuid, os, model } }, mods, ranked, characterId, hash }` |
| `UploadTierRecord` | `Online/UploadTierRecord.cs` | `{ completion, health, score, averageAccuracy, details, mods, maxCombo, records: [UploadRecord] }` |
| `LevelRating` | `Online/LevelRating.cs` | `{ average, total, rating (user's), like, dislike }` |
| `Leaderboard` | `Online/Leaderboard.cs` | `Entry extends OnlineUser + { rank, rating }` |
| `LibraryLevel` | `Online/LibraryLevel.cs` | `{ date, expiryDate, granted, level: OnlineLevel }` |
| `CollectionMeta` | `Online/CollectionMeta.cs` | `{ id, uid, title, slogan, cover: OnlineImageAsset, owner, levels }` |
| `EventMeta` | `Online/EventMeta.cs` | `{ uid, title, slogan, locked, startDate, endDate, levelId, collectionId, epicId, cover, logo, url }` |
| `TierMeta` | `Online/TierMeta.cs` | `{ name, stages, criteria, character, colorPalette, thresholdAccuracy, maxHealth }` |
| `SeasonMeta` | `Online/TierMeta.cs` | `{ uid, tiers: TierMeta[] }` |
| `Quest` | `Online/Quest.cs` | `OngoingQuest`, `Objective`, `AdventureState`, `SingleQuestState`, `ProgressType` enum |
| `Reward` | `Online/Reward.cs` | `{ type (Character/Level/Badge), value: JObject }` |
| `Badge` | `Online/Badge.cs` | `{ uid, title, description, listed, date, type (Achievement/Event), metadata }` |
| `Announcement` | `Online/Announcement.cs` | `{ currentVersion, minSupportedVersion, message }` |
| `ErrorResponse` | `Online/ErrorResponse.cs` | `{ message }` |
| `OnlinePlayerStateChange` | `Online/OnlinePlayerStateChange.cs` | `{ hasChanges, rewards }` |
| `CharacterMeta` | `Character/CharacterMeta.cs` | `{ id, name, description, illustrator, designer, owned, setId, setOrder, variantName, asset, questId, exp: { currentLevel, totalExp, currentLevelExp, nextLevelExp } }` |

## Local Storage (legacy; removed from Unity core)

**Historical:** the full client used LiteDB (`Cytoid.db`) for settings, level records, profile, library, characters, and training data.

**Current `cytoid-core-unity`:** no embedded database. Unity keeps **in-memory** `LocalPlayerSettings` (defaults until Flutter applies `GameLaunchSettings` via `bridge.play.start` / `bridge.settings.update`) and per-session `LevelRecord` (e.g. `relative_note_offset` returned in `game.play.result`). Cross-session persistence is the **Flutter host** responsibility.

## Offline Support (legacy full client)

- Profile/library/characters/training caches were loaded from LiteDB when offline
- CDN region auto-detection with fallback (try international → try CN → offline mode)
- Network errors on library fetch silently return empty list

## Level Download & Management

### Level Package Format

`.cytoidlevel` zip files stored in `UserDataPath/{levelId}/`

### Download Flow

1. `GET /levels/{id}` → fetch level metadata
2. `POST /levels/{id}/resources` → get download URL
3. `GET {package_url}` → download zip
4. Unpack to local storage

## Level Data Models

| Model | File | Key Fields |
|-------|------|------------|
| `Level` | `Level/Level.cs` | Type (User/Tier/BuiltIn/Temp), IsLocal, OnlineLevel reference, Meta, Record, Path |
| `LevelMeta` | `Level/LevelMeta.cs` | schema_version, id, title, artist, illustrator, charter, music paths, background path, charts [{ type, name, difficulty, path, storyboard }] |
| `LevelRecord` | `Level/LevelRecord.cs` | LevelId, best performances per chart type (Standard + Practice), play counts, added/last played dates |
| `Difficulty` | `Level/Difficulty.cs` | Easy/Hard/Extreme with colors and display levels |
