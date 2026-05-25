# JPMS — Jewel Project Management System (Blazor WebAssembly PWA)

The production web app for Jewel Enterprises and its clients. Blazor WebAssembly + Tailwind, hosted on Azure Static Web Apps with Microsoft Entra ID authentication and Azure SQL Serverless for persistence.

## Stack

| Layer | Tech | Where it runs |
|---|---|---|
| Front-end | Blazor WebAssembly · .NET 8 LTS · PWA | Azure Static Web Apps (Free tier, West Europe) |
| Styling | Tailwind v3 CLI build | compiled at `dotnet publish` time |
| Authentication | Microsoft Entra ID via SWA managed auth (`/.auth/login/aad`) | Static Web Apps |
| Database (next) | Azure SQL Database GP_S_Gen5 Serverless, Free-Limit if available | UK South |

## What's built today

| Route | Page | Behaviour |
|---|---|---|
| `/` | `Pages/Login.razor` | Landing page with a single **Continue with Microsoft** button. Redirects authenticated users straight to `/dashboard`. |
| `/dashboard` | `Pages/Dashboard.razor` | Reads `/.auth/me`, then routes to the admin home, the placeholder role home, or the request-access view. |
| `/login` | (config) | Redirects to `/.auth/login/aad?post_login_redirect_uri=/dashboard`. |
| `/logout` | (config) | Redirects to `/.auth/logout?post_logout_redirect_uri=/`. |

The dashboard resolves to one of three states:

| State | When | Renders |
|---|---|---|
| Admin home | Signed-in email is in the directory with `Role.Admin` | `AdminHome` (stats row + pending requests + users + what's-next) |
| Role placeholder | Signed-in email is in the directory with any other role | `PlaceholderHome` |
| Request access | Signed-in email isn't in the directory | `RequestAccessView` — submits a request into the admin queue |

## Authentication flow

```
/ ──► Continue with Microsoft ──► /login (redirect) ──► /.auth/login/aad ──► Entra ID
                                                                                │
/dashboard ◄── set auth cookies ◄── /.auth/login/aad/callback ◄─────────────────┘
       │
       ├─ fetch /.auth/me
       ├─ AuthService.CurrentUser = email + provider
       ├─ UserDirectory.Find(email)
       │     ├─ Admin    ──► AdminHome
       │     ├─ Other    ──► PlaceholderHome
       │     └─ Missing  ──► RequestAccessView
```

When the user clicks **Sign out**, the app navigates to `/logout`, which the SWA configuration rewrites to `/.auth/logout?post_logout_redirect_uri=/`.

## Run locally on macOS

```bash
brew install --cask dotnet-sdk node
cd jpms
npm install
dotnet restore
dotnet run
```

Open the URL printed in the console. Locally there is no SWA emulator and no real `/.auth/me` endpoint, so the AuthService returns `null` and the dashboard redirects to `/login` (which 404s locally). To exercise the full sign-in flow, install the SWA CLI:

```bash
npm install -g @azure/static-web-apps-cli
swa start http://localhost:5000 --run "dotnet run --project jpms"
```

Hot reload:

```bash
dotnet watch --project jpms
```

CSS-only watch:

```bash
cd jpms && npm run watch:css
```

## Deploy to Azure

Two steps. After step 1 you can re-deploy as often as you like with no further setup.

### 1. Provision the Azure resources (one-off)

```bash
az login
az account set --subscription 08c5510c-bb27-4da8-b826-a8e76fb270ec
../infra/azure-setup.sh
```

The script creates the resource group, SQL Server + Serverless DB, Static Web App, and Entra ID app registration; it writes connection details to `infra/.azure-output.env` (gitignored). Copy `SWA_DEPLOYMENT_TOKEN` from that file into a GitHub repository secret named `AZURE_STATIC_WEB_APPS_API_TOKEN`.

### 2. Push to `main`

The workflow in `.github/workflows/jpms-swa.yml` installs Node + .NET, runs `npm install` and `dotnet publish` (which triggers the Tailwind build), then deploys `publish/wwwroot` to Static Web Apps.

## Project layout

```
jpms/
├── Jewel.JPMS.csproj              Blazor WASM project + Tailwind MSBuild target
├── Program.cs                     App entry point — registers HttpClient + RBAC services
├── App.razor                      Top-level router
├── _Imports.razor                 Razor using directives
├── staticwebapp.config.json       SWA routes + AAD identity provider
├── package.json                   Tailwind CSS toolchain
├── tailwind.config.js             Tailwind theme + content paths
├── Styles/
│   └── app.tailwind.css           Tailwind source — compiled to wwwroot/css/app.built.css
├── Layout/
│   ├── MainLayout.razor           Header + sign-out + footer
│   └── PrimaryNav.razor           Top-level navigation links
├── Pages/
│   ├── Login.razor                Landing page — Microsoft sign-in
│   ├── Dashboard.razor            Role router
│   ├── Projects.razor             /projects — portfolio list
│   └── ProjectDetail.razor        /projects/{id} — project detail hub
├── Components/
│   ├── AdminHome.razor            Admin homepage composition
│   ├── AdminStatsRow.razor        Stats grid (approved / pending / roles)
│   ├── PendingRequestsPanel.razor List of pending access requests
│   ├── PendingRequestRow.razor    One request — approve/decline/role-pick
│   ├── RoleAssignmentForm.razor   Role dropdown + confirm/cancel
│   ├── ApprovedUsersPanel.razor   List of approved directory users
│   ├── ApprovedUserRow.razor      One approved user — revoke
│   ├── WhatsNextPanel.razor       Next-up backlog tiles
│   ├── PlaceholderHome.razor      Non-admin role landing page
│   ├── ProjectsTable.razor        Project list table with row navigation
│   ├── ProjectDetailView.razor    Project detail page body
│   ├── ProjectStageBadge.razor    Coloured project stage chip
│   ├── RoleSwitcher.razor         Header role dropdown for multi-role users
│   ├── ProviderButton.razor       OAuth provider sign-in button
│   ├── RequestAccessView.razor    For signed-in users not yet on the directory
│   ├── RoleBadge.razor            Small coloured role chip
│   ├── Stat.razor                 Small labelled stat card
│   └── Icons/
│       ├── MicrosoftLogoIcon.razor
│       └── GoogleLogoIcon.razor
├── Models/
│   ├── Role.cs                    Role enum + display / RBAC helpers
│   ├── User.cs                    AuthProvider, AuthenticatedUser, DirectoryUser records
│   ├── AccessRequest.cs           Pending-request record
│   ├── ClientPrincipal.cs         Shape of /.auth/me response
│   ├── JpmsAdministrators.cs      Hardcoded admin email allowlist
│   ├── Project.cs                 Project record
│   ├── ProjectStage.cs            Project lifecycle stage enum + display
│   └── Organisation.cs            Jewel entity enum (JBB / JPS / JPF)
├── Services/
│   ├── AuthService.cs             Fetches /.auth/me, exposes CurrentUser
│   ├── SessionService.cs          Holds active role + computed effective roles
│   ├── EffectiveRoles.cs          Merges hardcoded admin + directory roles
│   ├── IUserDirectory.cs          Directory contract
│   ├── AllowListUserDirectory.cs  In-memory directory (mutable for the session)
│   ├── IAccessRequestStore.cs     Access-request queue contract
│   ├── InMemoryAccessRequestStore.cs   Session-scoped queue
│   ├── IProjectStore.cs           Project store contract
│   └── InMemoryProjectStore.cs    Seeded in-memory project store
└── wwwroot/                       Everything served to the browser
    └── staticwebapp.config.json   SWA routes + AAD identity provider
```

## Code style

This codebase follows the style rules in the repository-root `CLAUDE.md`:

- Hard limit of 100 lines per file.
- No comments that explain *what* the code does — the names carry it.
- Booleans use `is`/`has`/`should`/`can` prefixes.
- No abbreviations in identifiers.
- Magic colours and strings live in Tailwind tokens or named constants.

When adding new code, run mentally through CLAUDE.md's pre-commit checklist before merging.

## What lands next

1. **Azure SQL persistence.** Replace `AllowListUserDirectory` and `InMemoryAccessRequestStore` with implementations that call a new API (running on Static Web Apps managed Functions).
2. **Invite-by-email.** Admin can pre-approve a user; the SWA AAD role assignment then surfaces them as a `DirectoryUser` on first sign-in.
3. **Google + email/password.** Add the Google identity provider to `staticwebapp.config.json`; the UI already renders a Google logo button.
4. **First domain screen.** The Programme Valuation Report is the highest-value module — that's the first screen wired to real entities.

Source of truth for what to build next is the user-story register in `/docs/03-workflows/`.

## Cost expectation (testing)

| Resource | Idle | Active |
|---|---|---|
| Static Web App (Free tier) | £0 | £0 up to 250GB/month bandwidth |
| Azure SQL Serverless (with Free-Limit) | £0 (paused) | £0 up to 100k vCore-sec + 32GB/month |
| Entra ID app registration | £0 | £0 |

If the Free-Limit flag isn't accepted on the subscription, the SQL falls back to standard Serverless: ~£3-5/month idle (storage only), pennies per active hour.
