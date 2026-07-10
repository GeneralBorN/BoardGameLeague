https://console.cloud.google.com/

Napravi novi projekt (ili odaberi postojeći).

U lijevom izborniku odaberi APIs & Services -> Credentials.

Klikni na gumb + CREATE CREDENTIALS (na vrhu ekrana) i odaberi OAuth client ID.

    Napomena: Ako ovo radiš prvi put u projektu, Google će tražiti da prvo konfiguriraš "OAuth consent screen" (samo unesi ime aplikacije i svoj email).

Za "Application type" odaberi Web application.

Pod Authorized redirect URIs dodaj URL svoje lokalne aplikacije s nastavkom /signin-google. Na primjer: https://localhost:7197/signin-google (zamijeni broj porta s onim koji tvoja aplikacija koristi).

Klikni Create. Dobit ćeš skočni prozor (pop-up) u kojem će pisati tvoj Client ID i Client Secret.

Komande:

dotnet user-secrets init

dotnet user-secrets set "Authentication:Google:ClientId" "OVDJE_ZALIJEPI_SVOJ_CLIENT_ID"

dotnet user-secrets set "Authentication:Google:ClientSecret" "OVDJE_ZALIJEPI_SVOJ_CLIENT_SECRET"


Azure
https://portal.azure.com/

Site
boardgameleague-e3aec9frfeh6aee0.austriaeast-01.azurewebsites.net

dotnet add package Microsoft.Playwright.NUnit
