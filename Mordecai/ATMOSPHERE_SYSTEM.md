# Atmosphere System - Random Thunder Messages

## Overview
The Mordecai MUD now includes an atmosphere system that generates random environmental messages to enhance immersion. Currently implemented: **thunder messages** that occur approximately every 15 minutes.

## Features

### ??? Thunder Messages
- **Frequency**: Every 15 minutes (±5 minutes randomization)
- **Variety**: 8 different thunder message variations
- **Delivery**: Broadcast to all connected players
- **Persistence**: Messages are saved to database and included in chat history
- **Styling**: Special purple styling with glow effects

### ?? Message Styling
Thunder messages appear with:
- **Purple text** with subtle glow effect
- **Italic styling** to distinguish from other messages
- **Background highlight** for visibility
- **Left border accent** in purple
- **Source**: "Nature" as the message sender

### ? Message Variations
The system randomly selects from these thunder messages:
1. "You hear thunder rumbling in the distance."
2. "A low rumble of thunder echoes through the dungeon."
3. "Thunder booms somewhere far above the stone halls."
4. "The sound of distant thunder reverberates through the corridors."
5. "A deep, rolling thunder can be heard from beyond these walls."
6. "Thunder crashes in the distance, making the ground tremble slightly."
7. "You hear the ominous sound of thunder rolling across the sky."
8. "A thunderclap echoes through the dungeon, distant but powerful."

## Technical Implementation

### ?? Components
- **AtmosphereService**: Background service that generates messages
- **ChatService**: Enhanced with `BroadcastAtmosphereMessageAsync()` method
- **ChatMessageType.Atmosphere**: New message type for ambient messages
- **CSS Styling**: Custom `.atmosphere` class for visual distinction

### ?? Smart Features
- **Player Detection**: Only sends messages when players are connected
- **Database Integration**: Messages are persisted and included in chat history
- **Error Handling**: Robust error handling and retry logic
- **Development Mode**: Faster initial thunder (2 minutes) for testing

### ??? Service Configuration
```csharp
// Registered in Program.cs as a hosted background service
builder.Services.AddHostedService<AtmosphereService>();
```

## Usage

### ?? Player Experience
Players will see thunder messages appear naturally during gameplay:
```
[14:23:15] Nature: You hear thunder rumbling in the distance.
```

### ?? Testing
For immediate testing, players can use:
```
/thunder
```
This manually triggers a thunder message for all connected players.

### ?? Commands
- **Player Command**: `/thunder` - Manually trigger thunder (testing)
- **Help Updated**: `/help` now shows the thunder test command

## Development Notes

### ?? Timing Configuration
- **Base Interval**: 15 minutes
- **Random Variation**: ±5 minutes (10-20 minute range)
- **Development Mode**: First thunder at 2 minutes for testing
- **Production**: Standard 15-minute intervals

### ?? Background Service Lifecycle
- **Startup**: Service starts with application
- **Runtime**: Continuously generates messages while app is running
- **Shutdown**: Gracefully stops when application shuts down
- **Error Recovery**: Automatic retry with 1-minute delay on errors

### ?? Database Impact
- Messages are stored in `ChatMessages` table
- **PlayerId**: null (system message)
- **RoomId**: null (global message)
- **Type**: `Atmosphere`
- **PlayerName**: "Nature"

### ?? CSS Classes
```css
.message.atmosphere .content {
    color: #a78bfa;                    /* Purple text */
    font-style: italic;                /* Italic styling */
    font-weight: 500;                  /* Medium weight */
    text-shadow: 0 0 10px rgba(167, 139, 250, 0.3); /* Glow effect */
    background: linear-gradient(...);   /* Background highlight */
    border-left: 3px solid ...;       /* Left accent border */
}
```

## Future Enhancements

### ?? Planned Features
- **More Weather**: Rain, wind, distant sounds
- **Time-Based Events**: Different messages for day/night cycles
- **Location-Specific**: Different atmosphere per room type
- **Seasonal Events**: Holiday-themed atmospheric messages
- **Player Triggers**: Atmosphere that responds to player actions

### ?? Possible Expansions
- **Random Encounters**: NPCs or creatures appearing
- **Environmental Changes**: Room descriptions that change over time
- **Player Notifications**: Private atmospheric messages
- **Sound Integration**: Audio cues for atmosphere messages

## Configuration

### ?? Customizing Intervals
To change thunder frequency, modify `AtmosphereService.cs`:
```csharp
var baseInterval = TimeSpan.FromMinutes(15); // Change base time
var randomVariation = TimeSpan.FromMinutes(_random.Next(-5, 6)); // Change variation
```

### ?? Customizing Messages
Add new messages to the `_thunderMessages` array in `AtmosphereService.cs`:
```csharp
private readonly string[] _thunderMessages = 
[
    "Your new thunder message here.",
    // ... existing messages
];
```

### ??? Customizing Styling
Modify the `.atmosphere` CSS class in `dungeon.css` for different visual effects.

## Troubleshooting

### ?? Common Issues
- **No thunder messages**: Check if players are connected (service only sends to active players)
- **Messages not styled**: Ensure `dungeon.css` is loaded and `.atmosphere` class exists
- **Service not starting**: Check console logs for `AtmosphereService` startup messages

### ?? Debug Logging
The service logs important events:
- Service startup/shutdown
- Thunder message generation
- Player count checks
- Error conditions

### ?? Testing
Use `/thunder` command to immediately test the system without waiting for the timer.

---

The atmosphere system adds a new layer of immersion to the Mordecai MUD, making the dungeon feel more alive and dynamic with unpredictable environmental events!