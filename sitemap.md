# Sitemap

## Routes and Controllers

- `/` or `/dashboard`
  - Controller: `HomeController`
  - Action: `Index`
  - View: `Views/Home/Index.cshtml`

- `/privacy`
  - Controller: `HomeController`
  - Action: `Privacy`
  - View: `Views/Home/Privacy.cshtml`

- `/teams`
  - Controller: `TeamsController`
  - Action: `Index`
  - View: `Views/Teams/Index.cshtml`

- `/teams/{id}`
  - Controller: `TeamsController`
  - Action: `Details`
  - View: `Views/Teams/Details.cshtml`

- `/players`
  - Controller: `PlayersController`
  - Action: `Index`
  - View: `Views/Players/Index.cshtml`

- `/players/{id}`
  - Controller: `PlayersController`
  - Action: `Details`
  - View: `Views/Players/Details.cshtml`

- `/games`
  - Controller: `BoardGamesController`
  - Action: `Index`
  - View: `Views/BoardGames/Index.cshtml`

- `/games/{id}`
  - Controller: `BoardGamesController`
  - Action: `Details`
  - View: `Views/BoardGames/Details.cshtml`

- `/tournaments`
  - Controller: `TournamentsController`
  - Action: `Index`
  - View: `Views/Tournaments/Index.cshtml`

- `/tournaments/{id}/schedule`
  - Controller: `TournamentsController`
  - Action: `Details`
  - View: `Views/Tournaments/Details.cshtml`

- `/matches`
  - Controller: `MatchesController`
  - Action: `Index`
  - View: `Views/Matches/Index.cshtml`

- `/matches/{id}/result`
  - Controller: `MatchesController`
  - Action: `Details`
  - View: `Views/Matches/Details.cshtml`

- `/venues`
  - Controller: `VenuesController`
  - Action: `Index`
  - View: `Views/Venues/Index.cshtml`

- `/venues/{id}/location`
  - Controller: `VenuesController`
  - Action: `Details`
  - View: `Views/Venues/Details.cshtml`
