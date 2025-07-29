# Multiplayer Visibility System

## Overview
Players can now see each other when they're in the same room, creating a more immersive multiplayer experience. This affects both the `look` command and room entry notifications.

## Features

### ?? **Room Occupancy Display**
- **Real-time Updates**: Player lists are always fresh and current
- **Look Command**: Shows all other online players in the current room
- **Directional Look**: When looking in a direction, see players in adjacent rooms
- **Dynamic Updates**: Player lists update immediately when someone enters/leaves

### ?? **Movement Notifications**
- **Entry Notifications**: Other players see when someone enters their room
- **Exit Notifications**: Players see when someone leaves the room they're in
- **Directional Context**: Messages indicate which direction players came from/went to
- **Initial Spawn**: Other players are notified when someone first joins the game

### ?? **Enhanced Messaging**
- **Movement Messages**: Styled as italic action messages in gold color
- **Room Descriptions**: Include player lists as part of the standard room description
- **Contextual Messages**: Different messages for different types of movement

## Examples

### ?? **Look Command with Other Players**
```
> look
You stand at the entrance of a dark, mysterious dungeon. Ancient stone walls 
stretch before you, carved with strange runes that seem to glow faintly in the 
dim light. To the north, a narrow corridor disappears into darkness.

Obvious exits: north

Other players here: Alice, Bob, Charlie
```

### ?? **Movement Notifications**
**When Alice moves north:**
- **Alice sees**: "You move north to Dark Corridor."
- **Players in starting room see**: "Alice leaves north."
- **Players in destination room see**: "Alice arrives from the south."

**When Bob first joins the game:**
- **Other players in starting room see**: "Bob materializes into existence."

### ?? **Directional Looking with Players**
```
> look north
Looking north, you see: Dark Corridor - A dark, moss-covered corridor You can see Alice, Charlie there.
```

## Technical Implementation

### ?? **Enhanced GameService Methods**

**`GetRoomDescriptionAsync()`**:
- Always fetches fresh room data
- Includes current online players in the room
- Excludes the viewing player from the list

**`MovePlayerAsync()`**:
- Captures player lists before movement
- Returns enhanced MovementResult with player information
- Provides fresh room data after movement

**`LookInDirectionAsync()`**:
- Shows players in adjacent rooms
- Refreshes target room data for accuracy

### ?? **Enhanced ChatService Methods**

**`BroadcastToRoomAsync()`**:
- Sends messages to all players in a specific room
- Supports excluding specific connections (like the acting player)
- Saves messages to database with room context

**`BroadcastMovementAsync()`**:
- Specialized method for movement notifications
- Creates appropriately styled action messages
- Manages room-specific broadcasting

### ??? **Enhanced Data Structures**

**MovementResult Class**:
```csharp
public class MovementResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public Room? NewRoom { get; set; }
    public Room? PreviousRoom { get; set; }
    public List<Player> PlayersInNewRoom { get; set; } = new();
    public List<Player> PlayersInPreviousRoom { get; set; } = new();
}
```

## Message Types

### ?? **Movement Messages**
- **Type**: `ChatMessageType.Action`
- **Style**: Gold italic text with reduced opacity
- **Examples**:
  - "Alice leaves north."
  - "Bob arrives from the south."
  - "Charlie materializes into existence."

### ?? **Room Description Updates**
- **Type**: `ChatMessageType.Description`
- **Trigger**: After successful movement
- **Content**: Full room description including current players

## Database Integration

### ?? **Message Storage**
- Movement notifications are saved to the database
- Messages include room context (`RoomId`)
- Part of chat history for players joining later

### ?? **Real-time Synchronization**
- Player locations are immediately updated in database
- Room queries always return current player lists
- No caching of player positions to ensure accuracy

## Player Experience

### ?? **Seamless Integration**
The multiplayer visibility works automatically without any special commands:

1. **Join Game**: See other players immediately in starting room
2. **Look Around**: Always see who else is present
3. **Move Around**: Get notified of others' movements
4. **Explore**: Peek into adjacent rooms to see other players

### ?? **Notification Flow**
**When Alice moves from Room A to Room B**:
1. Players in Room A see: "Alice leaves north."
2. Alice sees: "You move north to Dark Corridor."
3. Players in Room B see: "Alice arrives from the south."
4. Alice sees room description including any players in Room B

## Configuration

### ?? **Customization Options**

**Message Templates** (in `Dungeon.razor`):
- Entry message: `"{playerName} arrives from the {oppositeDirection}."`
- Exit message: `"{playerName} leaves {direction}."`
- Spawn message: `"{playerName} materializes into existence."`

**Styling** (in `dungeon.css`):
```css
.message.action .content {
    color: #fbbf24;        /* Gold color */
    font-style: italic;    /* Italic text */
    opacity: 0.9;          /* Slightly transparent */
}
```

## Future Enhancements

### ?? **Planned Features**
- **Stealth System**: Some players might be hidden from others
- **Player Descriptions**: Custom descriptions for each player
- **Status Indicators**: Show player health, level, or status
- **Room Capacity**: Limits on how many players can be in a room
- **Private Rooms**: Rooms that only certain players can enter

### ?? **Possible Improvements**
- **Player Activities**: Show what other players are doing (fighting, trading, etc.)
- **Emotes System**: Players can express emotions that others see
- **Player Inspection**: Detailed look at other players' equipment
- **Social Features**: Friend lists, groups, guilds

## Troubleshooting

### ?? **Common Issues**
- **Players not visible**: Check if they are marked as online in database
- **Stale player lists**: The system now always fetches fresh data with `GetRoomWithFreshDataAsync()`
- **Missing notifications**: Verify ChatService broadcasting is working
- **Movement/Look synchronization**: Fixed with database consistency delays and fresh data queries

### ??? **Recent Fixes (Database Synchronization)**
- **Added timing delays**: Small delays (50-100ms) ensure database transactions complete before notifications
- **Fresh data queries**: `GetRoomWithFreshDataAsync()` method clears Entity Framework cache for guaranteed current data
- **Improved transaction ordering**: Movement confirmations sent before notifications to improve user experience
- **Debug logging**: Added console logging for player movement tracking

### ??? **Debugging Tools**
- Check `/admin` page to see player locations
- Console logs show player movements with room transitions
- Database queries can verify player positions
- Movement notifications now have better synchronization

### ?? **Technical Details**
The synchronization issue was caused by different Entity Framework contexts seeing different states of the database. The fix includes:

1. **Database Consistency Delays**: Small delays after database updates ensure all contexts see the changes
2. **Fresh Data Queries**: `AsNoTracking()` and `ChangeTracker.Clear()` ensure no stale cached data
3. **Improved Timing**: Movement confirmations happen immediately, notifications after database sync
4. **Enhanced Queries**: Direct queries with explicit `Include()` for player relationships

---

The multiplayer visibility system transforms the Mordecai MUD from a single-player experience into a truly social multiplayer world where players can see and interact with each other naturally!