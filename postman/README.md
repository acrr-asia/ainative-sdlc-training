# Postman: full API flow

1. Start the API: `dotnet run --project src/TaskApi` (Development seeds `admin@example.com` / `Admin123!`).
2. In Postman, import `TaskApi.postman_collection.json` and `TaskApi.Local.postman_environment.json`, then select the **TaskApi Local** environment.
3. Run the whole collection in order with the Collection Runner. Tokens and IDs are saved to collection variables as it goes.

Command line: `npx newman run postman/TaskApi.postman_collection.json -e postman/TaskApi.Local.postman_environment.json`
(40 requests, 51 assertions).

| Folder | What it checks |
|---|---|
| 0. Health | Anonymous route |
| 1. Auth | Register, duplicate 409, validation 400, login, same 401 for wrong password and unknown email (S3), `/auth/me`, missing or forged token 401 (S2) |
| 2. Categories | Member reads but gets 403 on create; admin creates; duplicate 409 |
| 3. Tasks | Owner creates, lists, completes; a second user gets 404 on someone else's task (S4); admin sees all |
| 4. Admin | Member gets 403; no password hashes (S5); unknown role 400; promote to Admin |
| 5. Cleanup | Delete task and category |
| 6. Login throttling | 5 failures then 429 with Retry-After; right password still 429 (S6); same response for unknown email (S7); register not throttled (S8) |

Folder 6 locks your IP out of `/auth/login` for 60 seconds, so it runs last. Wait a minute (or restart the API) before running the collection again.
