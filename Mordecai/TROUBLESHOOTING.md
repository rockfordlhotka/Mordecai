# Troubleshooting Database Issues

## Common Database Errors

### Error 1: GameSession Insert Failure (Schema Mismatch)

If you encounter an error like this when trying to log in:
```
Failed executing DbCommand (1ms) [Parameters=[@p0='?' (Size = 36), @p1='?' (DbType = DateTime), @p2='?' (DbType = Int32), @p3='?' (DbType = DateTime)], CommandType='Text', CommandTimeout='30']
INSERT INTO "GameSessions" ("ConnectionId", "EndedAt", "PlayerId", "StartedAt")
VALUES (@p0, @p1, @p2, @p3)
RETURNING "Id", "IsActive";
```

This typically means the database schema is out of sync with the current model definitions.

### Error 2: Foreign Key Constraint Failed

If you encounter an error like this:
```
SQLite Error 19: 'FOREIGN KEY constraint failed'.
INSERT INTO "GameSessions" ("ConnectionId", "EndedAt", "PlayerId", "StartedAt")
VALUES (@p0, @p1, @p2, @p3)
RETURNING "Id", "IsActive";
```

This means the `GameSession` is trying to reference a `PlayerId` that doesn't exist in the `Players` table. This was caused by trying to use `player.Id` before the player was saved to the database.

**This issue has been fixed in the latest version** by ensuring the player is saved first before creating the session.

## Quick Fix Solutions

### Solution 1: Automatic Database Reset (Recommended)
The application now includes automatic database recovery. If it detects a schema mismatch, it will:
1. Log the error
2. Automatically delete the old database
3. Recreate it with the current schema
4. Restore seed data

Simply restart the application and it should work.

### Solution 2: Admin Panel Reset
1. Navigate to `/admin` in your browser
2. Click the "Reset Database" button
3. Wait for the success message
4. Try logging in again

### Solution 3: Manual Database Reset
If the automatic solutions don't work:
1. Stop the application
2. Delete the `mordecai.db` file from the project directory
3. Restart the application
4. The database will be recreated automatically

## Why These Errors Happen

These errors typically occur when:
- **Schema Mismatch**: The Entity Framework model definitions change but the database wasn't updated
- **Foreign Key Issues**: Code tries to reference an entity ID before it's been saved to the database
- **SQLite Constraints**: Database integrity constraints prevent invalid data insertion
- **Version Conflicts**: Database was created with an older version of the code

## Recent Fixes

The latest version includes fixes for:
- **Foreign Key Constraint Error**: Players are now saved before GameSessions are created
- **Better Error Handling**: Enhanced error handling in `Program.cs`
- **Explicit Null Handling**: Better handling of nullable fields like `EndedAt` in GameSession
- **Enhanced Database Configuration**: More robust entity configurations
- **Automatic Recovery**: Automatic database recreation on schema errors

## Prevention

The new version includes:
- Better error handling in `Program.cs`
- Proper transaction ordering (save Player before GameSession)
- Explicit null handling for `EndedAt` in GameSession
- Enhanced database configuration
- Automatic recovery mechanisms

## Database Schema Information

The `GameSession` table should have these columns:
- `Id` (INTEGER, PRIMARY KEY)
- `ConnectionId` (TEXT, NOT NULL, MAX 100 chars)
- `PlayerId` (INTEGER, NOT NULL, FOREIGN KEY to Players.Id)
- `StartedAt` (DATETIME, NOT NULL)
- `EndedAt` (DATETIME, NULL)
- `IsActive` (BOOLEAN, NOT NULL, DEFAULT TRUE)

The `Players` table should have:
- `Id` (INTEGER, PRIMARY KEY, AUTOINCREMENT)
- `Name` (TEXT, NOT NULL, MAX 50 chars)
- `CreatedAt` (DATETIME, NOT NULL)
- Other player fields...

If your database doesn't match this schema, use one of the reset solutions above.

## Testing After Fix

After applying the latest code changes:
1. **Delete the existing `mordecai.db` file** (important!)
2. **Restart the application**
3. **Try logging in** - it should work now
4. **Check the `/admin` page** to verify the database status

The foreign key constraint error should be resolved because:
- Players are now saved to get a valid ID first
- GameSessions are created with proper foreign key references
- Database transactions are properly ordered