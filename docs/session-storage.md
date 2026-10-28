# Session token storage

The AoC `session` token is stored in the operating system secret store, never in the app-data database, config, logs or the solutions repo.

- Linux: Secret Service through libsecret (`libsecret-1.so.0`), default collection.
- Windows: Credential Manager (generic credential `AoCEditor/session-token`).
- The store is chosen at runtime by `SecretStores.CreateForCurrentOs()`.

`ISecretStore` gets, sets or deletes one named secret and returns `Ok`, `NotFound` or `Unavailable(reason)`. `SessionTokenProvider` implements `ISessionTokenProvider` on top of it and adds `SaveAsync` and `ClearAsync` (sign out). Results and exceptions never contain the token.

When no Secret Service or libsecret is present the result is `Unavailable`; there is no fallback storage yet.

Integration tests (`Category=Integration`) use the real store and skip when none is available.
