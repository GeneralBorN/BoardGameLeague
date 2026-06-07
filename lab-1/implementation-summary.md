# Lab 1 Implementation Summary

## Lab requirements met

- Object model
  - The application contains a model with more than 7 classes:
    - `Player`, `Team`, `BoardGame`, `Venue`, `Tournament`, `Match`, `Attachment`, `AppUser`, etc.
  - At least 4 complex classes with more than 5 properties:
    - `Player`, `Team`, `BoardGame`, `Tournament`, `Match`, `Venue`.
  - A custom enum is present: `GameCategory`.
  - DateTime properties are used in the model: `JoinedDate`, `FoundedDate`, `StartDate`, `EndDate`, `StartTime`.
  - Correct relationships are defined:
    - 1-N: `Venue` → `Tournament`, `Tournament` → `Match`, `Team` → `Players`.
    - N-N: `Team` ↔ `Tournament`.

- Sample data creation in main program
  - `Program.cs` seeds the database using `LeagueDataFactory.CreateSampleLeagueAsync()`.
  - The factory creates multiple tournaments, teams, board games, players, venues, and matches.
  - At least 3 main objects are created and linked into a meaningful domain graph.

- LINQ queries
  - The sample data factory includes LINQ examples:
    - top teams by average player rating and win rate
    - teams filtered by win rate
    - upcoming tournaments ordered by date
    - most popular board games by match count
    - aggregated match counts by venue

- Async/await
  - The sample factory demonstrates async behavior via `CreateSampleLeagueAsync()` and `await Task.Delay(1)`.
  - The data seeding in `Program.cs` uses async EF operations and awaits them.

- Lab artifact folder
  - Added `lab-1/ai-usage-log.txt` to the project root.
  - Added `lab-1/implementation-summary.md` to explicitly show how requirements were satisfied.
