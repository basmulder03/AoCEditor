# Storage

All app data lives in one per-user folder: `$XDG_DATA_HOME/AoCEditor` (fallback `~/.local/share/AoCEditor`) on Linux, `%LOCALAPPDATA%\AoCEditor` on Windows. The location can be overridden in settings. Folders are created the first time they are used.

The app refuses a data location that is inside, or contains, the solutions folder, so puzzle text and inputs cannot end up in the solutions repository. The check compares resolved paths and does not follow symlinks.

## Database

`aoceditor.db` in the data folder is SQLite, accessed with raw ADO.NET (no ORM). Each connection runs in WAL mode with foreign keys on. Timestamps are stored as unix-millisecond integers.

Schema changes are numbered SQL files embedded in the assembly (`Migrations/NNNN_name.sql`). On open, each unapplied file runs in its own transaction and is recorded in `schema_migrations`; a failing file is rolled back and the open fails. Current tables: `kv` (settings) and `sync_state` (year, day, stars, synced_at), exposed through `ISyncStateStore` (`SqliteSyncStateStore`).

The native SQLite library ships next to the executable, so single-file deployment is not possible. The Windows library has not been verified yet.
