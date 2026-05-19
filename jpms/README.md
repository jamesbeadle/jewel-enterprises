# JPMS — Jewel Project Management System (Blazor WebAssembly PWA)

The production web app Jewel Enterprises and its clients will use day-to-day. This is **not** the scoping prototype — that lives in `/prototypes/journey-index/`. This folder is the start of the real product.

- **Tech:** Blazor WebAssembly · .NET 8 LTS · PWA · Tailwind (via CDN for now)
- **Hosts on:** Azure Static Web Apps (will move to App Service once an ASP.NET Core API is added)
- **Auth (current):** mocked Microsoft + Google sign-in with a hard-coded internal allow-list
- **Auth (target):** Microsoft Entra ID + Google Identity Services, with an admin-managed user directory in the JPMS backend

---

## What's built in this first cut

| Route | Page | Behaviour |
|---|---|---|
| `/` | `Pages/Login.razor` | Landing page. Two buttons: **Continue with Microsoft** and **Continue with Google**. The buttons currently open a mock prompt asking which email to sign in as. |
| `/dashboard` | `Pages/Dashboard.razor` | After sign-in. Looks the user up in the internal allow-list. If approved → welcome dashboard. If not approved → renders the **Request access** view. |

The mock allow-list lives in `Services/AllowListUserDirectory.cs`. Add or remove approved emails there while we build the rest of the platform.

### Sign-in flow

```
Login screen  ──►  Pick provider  ──►  Enter email (mock)  ──►  Dashboard route
                                                                  │
                                                                  ├─ email is on allow-list ─►  Home dashboard
                                                                  └─ email is NOT on list  ─►  Request access screen
```

When real OAuth is wired up, the *"Enter email (mock)"* step disappears — the provider returns the email itself.

---

## Run locally on Mac

### 1. Install the .NET 8 SDK

```bash
brew install --cask dotnet-sdk
```

Or download the macOS .NET 8 SDK from <https://dotnet.microsoft.com/en-us/download/dotnet/8.0> (Arm64 for Apple Silicon, x64 for Intel).

Verify:

```bash
dotnet --version
# expect 8.0.x
```

### 2. Restore + run

From the repo root:

```bash
cd jpms
dotnet restore
dotnet run
```

Then open the URL the console prints (typically `https://localhost:5001`). The PWA install button shows up in Chrome/Edge after a moment.

> **Hot reload:** use `dotnet watch` instead of `dotnet run` to auto-rebuild on file save.

### 3. Build a production bundle

```bash
dotnet publish -c Release -o publish
```

The deployable static site is at `publish/wwwroot/`.

---

## Project layout

```
jpms/
├── Jewel.JPMS.csproj          Project file — targets net8.0
├── Program.cs                 App entry point. Registers AuthService + IUserDirectory.
├── App.razor                  Top-level router
├── _Imports.razor             Razor using directives (every .razor sees these)
├── Layout/
│   └── MainLayout.razor       Header (with sign-in/out) + footer
├── Pages/
│   ├── Login.razor            Landing page — provider buttons
│   └── Dashboard.razor        Post-sign-in home (or request-access view if not approved)
├── Components/
│   ├── ProviderButton.razor   Microsoft / Google sign-in button with its logo
│   ├── RequestAccessView.razor   Shown when a signed-in user isn't on the allow-list
│   └── Stat.razor             Small labelled stat card used on the dashboard
├── Models/
│   └── User.cs                AuthProvider, AuthenticatedUser, DirectoryUser records
├── Services/
│   ├── AuthService.cs         Holds the current user + notifies subscribers on sign-in/out
│   ├── IUserDirectory.cs      Backend-shaped contract for looking up approved users
│   └── AllowListUserDirectory.cs   Hard-coded implementation — replace with API later
└── wwwroot/                   Everything served to the browser (HTML, CSS, icons, SW)
```

The style language matches the scoping prototype on purpose — same Tailwind palette (slate + accent dots), same rounded cards, same typography stack — so anything we already learned in the prototype carries over.

---

## Replacing the mocks

There are exactly two seams to wire up when we go live:

### 1. Real OAuth in `AuthService.SignInAsync(...)`

- Add Microsoft Entra ID via `Microsoft.Authentication.WebAssembly.Msal` and register a SPA app in Entra.
- Add Google sign-in via Google Identity Services. Register the client ID in Google Cloud Console.
- Replace the body of `SignInAsync` so it consumes the OAuth callback rather than accepting any email.
- Configure both client IDs in `wwwroot/appsettings.json` (added at that time — not committed today).

### 2. Real directory in `IUserDirectory`

- Stand up an `/api/users/me` endpoint on the ASP.NET Core backend that returns the directory record (or 404) for the authenticated user.
- Add a `HttpUserDirectory : IUserDirectory` implementation that calls that endpoint.
- Swap the DI registration in `Program.cs` — every page already talks to the interface, nothing else changes.

Both swaps are intentionally isolated so we can promote the app to "real auth + real directory" in a single small PR each.

---

## Deploy to Azure Static Web Apps

Identical to the prototype's flow — same Blazor preset, just point at `/jpms` instead of `/prototypes/journey-index`. Once the backend lands, this app moves behind a proper ASP.NET Core API and that deploy plan will get its own section here.

---

## Known limitations (today)

- **Mock sign-in.** Any email you type is accepted. Wire OAuth before any real users see this.
- **Hard-coded allow-list.** Lives in `Services/AllowListUserDirectory.cs`. Edit the list to test the two flows.
- **No backend.** Dashboard tiles are placeholders.
- **Tailwind via CDN.** Convenient for dev, swap for the Tailwind CLI build before going live.
- **No persisted session.** Refreshing the browser signs the user out. localStorage / a real OAuth token cache will fix this.
