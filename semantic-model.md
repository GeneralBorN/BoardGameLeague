# Semantic DB Model

## Entities

### Player
- Id: GUID (PK)
- Name: string
- Rating: int
- JoinedDate: DateTime
- Country: string
- Role: string
- Teams: 1-N relationship to Team via many-to-many

### BoardGame
- Id: GUID (PK)
- Name: string
- Category: GameCategory enum
- MinPlayers: int
- MaxPlayers: int
- AveragePlayTime: TimeSpan
- Complexity: decimal
- Matches: 1-N relationship to Match

### Team
- Id: GUID (PK)
- Name: string
- Region: string
- FoundedDate: DateTime
- IsActive: bool
- TotalWins: int
- TotalLosses: int
- Players: many-to-many relationship to Player
- Tournaments: many-to-many relationship to Tournament
- WinRate: computed property

### Venue
- Id: GUID (PK)
- Name: string
- City: string
- Country: string
- Capacity: int
- Indoor: bool
- Tournaments: 1-N relationship to Tournament

### Tournament
- Id: GUID (PK)
- Name: string
- Description: string
- StartDate: DateTime
- EndDate: DateTime
- VenueId: GUID (FK)
- Venue: reference to Venue
- IsOpen: bool
- Teams: many-to-many relationship to Team
- Matches: 1-N relationship to Match

### Match
- Id: GUID (PK)
- TournamentId: GUID (FK)
- Tournament: reference to Tournament
- TeamAId: GUID (FK)
- TeamA: reference to Team
- TeamBId: GUID (FK)
- TeamB: reference to Team
- GameId: GUID (FK)
- Game: reference to BoardGame
- StartTime: DateTime
- ScoreA: int
- ScoreB: int
- IsCompleted: bool
- Winner: computed property

## Relationship summary
- Player ↔ Team: many-to-many
- Team ↔ Tournament: many-to-many
- Tournament → Venue: many-to-one
- Tournament → Match: one-to-many
- Match → BoardGame: many-to-one
- Match → TeamA and TeamB: many-to-one
