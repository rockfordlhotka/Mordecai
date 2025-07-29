# Mordecai MUD - SQLite Database Setup

## Overview
This Blazor Server application uses SQLite with Entity Framework Core to store all persistent game data including players, rooms, items, chat messages, and game sessions. The game features a room-based movement system with validated directions and proper world geography.

## Database Features
- **SQLite Database**: Lightweight, file-based database perfect for development and small deployments
- **Entity Framework Core**: Modern ORM with LINQ support
- **Automatic Database Creation**: Database and schema are created automatically on first run
- **Seed Data**: Initial rooms and items are automatically populated
- **Room-Based Movement**: Proper directional movement with validation and room connections

## Database Schema

### Tables
- **Players** - Player characters with stats, location, and login information
- **Rooms** - Game world locations with descriptions and directional connections
- **GameItems** - Available items in the game world
- **PlayerItems** - Player inventory (many-to-many relationship)
- **ChatMessages** - Persistent chat and game message history
- **GameSessions** - Player connection sessions for tracking

### Room System
The game uses a directional room system where each room can have connections in four directions:
- `NorthRoomId` - Room to the north (nullable)
- `SouthRoomId` - Room to the south (nullable)  
- `EastRoomId` - Room to the east (nullable)
- `WestRoomId` - Room to the west (nullable)

#### Movement Validation
- Players can only move in directions where a connected room exists
- Attempting to move in an invalid direction shows available exits
- Movement updates the player's current room location in the database
- Room descriptions include available exits and other players present

#### Seeded World
The initial world contains 7 connected rooms:
1. **Dungeon Entrance** (Start) ? North to Dark Corridor
2. **Dark Corridor** ? North to Chamber of Echoes, South to Entrance, East to Armory
3. **Chamber of Echoes** ? South to Dark Corridor, West to Crystal Cavern
4. **The Armory** ? West to Dark Corridor, North to Guard Tower  
5. **Crystal Cavern** ? East to Chamber of Echoes, North to Underground Lake
6. **Guard Tower** ? South to Armory
7. **The Underground Lake** ? South to Crystal Cavern

### Key Features
- **Persistent Player Location**: Players resume at their last location
- **Real-time Movement**: Other players see when someone enters/leaves rooms
- **Dynamic Room Descriptions**: Shows exits and other players
- **Inventory System**: Stackable and non-stackable items
- **Chat Integration**: Messages tied to rooms and players
- **Session Tracking**: Complete login/logout history

## Game Commands

### Movement Commands
- `north`, `n` - Move north (if exit exists)
- `south`, `s` - Move south (if exit exists)
- `east`, `e` - Move east (if exit exists)
- `west`, `w` - Move west (if exit exists)

### Information Commands
- `look`, `l` - Get current room description with exits and players
- `look <direction>`, `l <direction>` - Look in a specific direction (north/south/east/west) to see the connected room's name and short description
- `inventory`, `inv`, `i` - View your inventory
- `help` - List available commands

### Chat Commands
- `/say <message>` - Send a chat message to other players
- `/who` - List online players
- `/help` - Show command help

## Configuration

### Connection String
Located in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=mordecai.db"
  }
}
```

### Database Initialization
The database is automatically created and seeded when the application starts:
- Uses `EnsureCreatedAsync()` for automatic schema creation
- Seeds 7 connected rooms with detailed descriptions
- Seeds 5 different item types (weapons, armor, consumables, tools, treasure)
- Creates room connections for proper navigation

## Usage

### Running the Application
1. Start the application with `dotnet run`
2. Database will be automatically created as `mordecai.db` in the project directory
3. Navigate to `/dungeon` to enter the game world
4. Navigate to `/admin` to view database status and world map

### Admin Interface
Visit `/admin` to see:
- Database connection status and statistics
- ASCII dungeon map showing room layout
- Room connection table with directional links
- Player locations and online status
- Complete room and item listings

### Movement System Example
```
> look
You stand at the entrance of a dark, mysterious dungeon...
Obvious exits: north

> look north
Looking north, you see: Dark Corridor - A dark, moss-covered corridor

> north
You move north to Dark Corridor.
A dimly lit stone corridor stretches before you...
Obvious exits: north, south, east

> look east
Looking east, you see: The Armory - An abandoned armory with old weapons

> west
You cannot go west from here. Valid directions are: north, south, east

> east
You move east to The Armory.
You find yourself in what appears to be an old armory...
```

## Service Architecture

### Enhanced Services
- **GameService**: Complete room and movement management
  - `MovePlayerAsync()` - Validates and executes player movement
  - `GetRoomDescriptionAsync()` - Dynamic room descriptions with exits
  - `GetPlayerInventoryAsync()` - Inventory management
- **ChatService**: Real-time chat with room awareness
- **DatabaseInitializationService**: Database setup with enhanced logging

### Movement System Classes
- **MovementResult**: Contains success status, messages, and room information
- **Room Navigation**: Automatic exit detection and validation
- **Player Tracking**: Real-time location updates and room occupancy

## Development Notes

### Room Design Philosophy
- **Connected World**: All rooms are reachable from the starting location
- **Logical Layout**: Room connections make geographical sense
- **Scalable System**: Easy to add new rooms and connections
- **Validation First**: Movement validation prevents impossible actions

### Database Relationships
- Players have foreign key to CurrentRoom
- Rooms connect to other rooms via directional IDs
- Messages are tied to both players and rooms
- Sessions track player movement history

### Future Enhancements
- **Item Placement**: Items can be placed in specific rooms
- **NPCs and Monsters**: Room-based encounters
- **Quest System**: Location-based objectives
- **World Building**: Tools for creating complex room networks
- **Room Instances**: Support for multiple versions of the same room

## Debugging

### Admin Tools
- **Room Map**: Visual representation of world layout
- **Connection Matrix**: Table showing all room connections
- **Player Tracking**: Real-time player locations
- **Movement Logs**: Database queries for movement history

### Common Issues
- **Invalid Movement**: Check room connections in admin panel
- **Player Location**: Verify CurrentRoomId is set correctly
- **Database State**: Use admin page to monitor room and player data

### Testing Movement
1. Login to the dungeon with a character name
2. Use `look` to see current room and available exits
3. Try moving in each direction to test connections
4. Use `/admin` page to see your location update in real-time
5. Test invalid directions to see error handling

The room-based movement system provides a solid foundation for a traditional MUD experience with proper world geography and movement validation.