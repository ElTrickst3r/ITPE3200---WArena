# WArena — Web Applications, Gamified

WArena is a gamified course-learning project with two independent sub-applications:

- **WArena.Mvc** — ASP.NET Core 10 MVC frontend + backend.
- **WArena.Api + warena-spa** — ASP.NET Core 10 Web API backend + React SPA frontend.

Both apps use their own SQLite database and can be run independently.

## Prerequisites

- .NET 10 SDK
- Node.js **22.16.0** for the React SPA
- npm (installed together with Node.js; record the version returned by `npm --version`)

No external API key is required.

## Seeded learning content

A fresh database contains six categories with 10 seeded questions each: HTML, CSS, ASP.NET Core, JavaScript, Div1, and Div2. Div1 is based on the supplied course demo material, while Div2 is based on the supplied lecture material. Two seeded challenge sets named **Div1** and **Div2** contain the 10 matching questions from those categories.

## Demo accounts

Both sub-applications seed the same accounts:

| Role | Email | Password |
| --- | --- | --- |
| Admin | `admin@warena.local` | `Admin123!` |
| Teacher | `teacher@warena.local` | `Teacher123!` |
| Student | `student@warena.local` | `Student123!` |
| Student 1–5 | `student1@warena.local` … `student5@warena.local` | `Student123!` |

The five numbered student accounts are included for multiplayer/Live Arena testing. New registrations are also created as Students.

## Subapp 1 — MVC

```bash
cd WArena.Mvc
dotnet restore
dotnet build
dotnet run
```

Open `http://localhost:5040`.

The SQLite database `warena-mvc.db` is created automatically on first start.

### MVC tests

```bash
cd WArena.Mvc.Tests
dotnet restore
dotnet test
```

The test project contains more than the required 12 backend unit tests, including at least six positive and six negative/boundary cases.

## Subapp 2 — API + React SPA

Start the API first:

```bash
cd WArena.Api
dotnet restore
dotnet build
dotnet run
```

The API runs at `http://localhost:5050` and creates `warena-api.db` automatically.

In another terminal, verify Node.js/npm and start the SPA:

```bash
node --version
npm --version
cd warena-spa
npm install
npm run build
npm run dev
```

If `npm` is not recognized, install Node.js 22.16.0 (npm is included), then close and reopen the terminal before trying again.

Open `http://localhost:5173`.

`npm run build` is included so build errors are caught before submission.

## Main functionality

- Admin, Teacher and Student roles.
- CRUD for Categories, Questions and Challenge Sets.
- Many-to-many relationship between Questions and Challenge Sets.
- Individual question practice and grouped challenge sets.
- Multi-parameter search and filtering.
- Immediate answer feedback and XP.
- Per-user history, accuracy, badges and daily streaks.
- Repository Pattern and asynchronous EF Core data access.
- MVC ViewModels and API DTOs.
- Client- and server-side validation, logging and error handling.
- React API service layer separated from page/business logic.
- Live Arena in the SPA with room codes, SignalR updates, speed-based scoring, random race themes, dynamic top-three rankings and a final podium.

## Database reset during development

The MVP uses `EnsureCreatedAsync()` rather than migrations to keep examiner setup small. If the entity model changes during development, stop the app, delete the relevant `.db` file, and start the app again.

## Documentation

See [`DOCUMENTATION.md`](DOCUMENTATION.md) for the project report draft, architecture, traceability matrix, security/testing notes and AI usage.


## Account management

Usernames are unique. Every signed-in user can change their own username and password from **Profile**. Admins can edit or delete other accounts from **Users**. Deleting an account also removes its related progress data.

In Live Arena, hosts can enable **Auto-continue** so the next question starts automatically after everyone answers or the timer runs out.
