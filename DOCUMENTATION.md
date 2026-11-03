# WArena — Project Documentation

## 1. Title & administrative metadata

**Project:** WArena — Web Applications, Gamified  
**Task:** Gamification Platform for Course Learning  
**Course context:** The example content is based on topics that fit a Web Applications course, such as HTML, CSS, JavaScript and ASP.NET Core.  
**Sub-application 1:** ASP.NET Core 10 MVC (`WArena.Mvc`)  
**Sub-application 2:** ASP.NET Core 10 Web API (`WArena.Api`) + React SPA (`warena-spa`)  
**Database:** SQLite through Entity Framework Core 10  
**SPA development environment:** Node.js 22.16.0 and npm 10.9.2  

**Group members:** [Add names / student numbers here]

WArena is a gamified learning application where students can practice course questions, complete groups of questions, get immediate feedback, earn XP, build learning streaks and collect achievement badges. Teachers can manage the learning content, while administrators can also manage user roles. The React version includes a Live Arena where students can join a room and compete on the same challenge set in real time.

The project contains two independent implementations of the same overall idea. The MVC app is completely server-rendered and can run without the React/API app. The React SPA uses its own ASP.NET API and its own SQLite database, so it can also run without the MVC app.

### Running the project

The full run instructions and seeded login credentials are in `README.md`. In short, the MVC application is started with `dotnet restore`, `dotnet build`, and `dotnet run`. For the second sub-application, the API is started in the same way, while the SPA is started with `npm install`, `npm run build`, and `npm run dev`.

Three demo accounts are seeded in both sub-applications: Admin, Teacher and Student. New registrations are automatically given the Student role.

---

## 2. Requirements analysis & traceability matrix

### 2.1 Problem and value proposition

Students often understand a topic better after actively testing themselves instead of only reading notes or watching a lecture. WArena adds a simple game layer to this process. The main goal is not to replace course material, but to give students a quick way to check what they know, see their progress and stay motivated to practise regularly.

The application supports two ways of practising. Students can choose any individual question from the question bank, or they can open a named Challenge Set such as “HTML Basics” or “CSS Fundamentals”. A Challenge Set can contain any selection of reusable questions. This makes it possible for a teacher to build different sets without copying the same question several times.

The progress system gives immediate feedback and XP. A student earns the full XP reward the first time a question is answered correctly. Repeating an already completed question is still allowed and awards 20% of the question’s normal XP. This keeps repeat practice rewarding while greatly reducing the value of simple point farming. Correct activity also updates the user’s streak, and milestone conditions can award achievement badges.

The Live Arena in the SPA is a bonus feature designed around the WArena name. A teacher creates a room from an existing Challenge Set and students join using a short code. Correct answers score points, while faster correct answers receive a larger time bonus. Wrong answers receive no points. The current ranking can be shown as a race with cars, horses, rockets or boats, and the final top three are shown on a podium.

### 2.2 Main user groups

**Students** can register/login, browse questions and Challenge Sets, submit answers, receive feedback, earn XP, build streaks, see badges and review their history. In the SPA they can also join Live Arena rooms.

**Teachers** have the student functionality plus CRUD access to Categories, Questions and Challenge Sets. They can create learning content and organise questions into reusable sets. In the SPA they can create and control Live Arena rooms.

**Admins** have all teacher and student access, plus the ability to change user roles.

### 2.3 Requirement traceability

| Requirement | MVC sub-app | API + SPA sub-app |
| --- | --- | --- |
| ASP.NET 10 / correct architecture | ASP.NET Core 10 MVC | ASP.NET Core 10 API + React SPA |
| Interactive web UI | Razor forms and views | React forms/components |
| 3+ domain entities with CRUD | Category, Question, ChallengeSet | Category, Question, ChallengeSet |
| N:M relationship | ChallengeSet ↔ Question through ChallengeSetQuestion | Same |
| Complex server-side rule | Full XP on first correct completion, 20% XP on repeat correct answers; streak calculation | Same + timed Live Arena scoring |
| Conditional rendering | Role-based controls, question type, correct/incorrect result, empty states | Protected routes, role controls, question type, live state, results |
| Multi-parameter search/filter | Question text/category/difficulty/min/max XP; set text/min questions/status | Same through query parameters |
| Repository Pattern / DAL | Repository interfaces + EF repositories | Repository interfaces + EF repositories |
| async/await DB access | EF Core async methods throughout repositories/controllers | Same |
| Client API service layer | N/A | `src/services/api.js` and `authService.js` |
| ViewModels / DTOs | Razor pages receive dedicated ViewModels | API returns DTOs; React never receives EF entities directly |
| Client + server validation | DataAnnotations/ModelState + unobtrusive validation | HTML/React validation + API DataAnnotations/domain checks |
| Error handling | production exception route, NotFound/BadRequest, validation feedback | ProblemDetails middleware + API status codes + client error boxes |
| ILogger | Controllers log auth/content/submission events | Controllers log auth/content/live events |
| Authentication/authorization | ASP.NET Identity cookies + roles | Identity + JWT bearer + client protected routes |
| 12 backend unit tests | `WArena.Mvc.Tests` has 14 tests | Not specifically required |
| Friendly design | Responsive custom CSS + Bootstrap | Responsive custom React design |
| Extra functionality | XP, streaks, badges, roles | XP, streaks, badges, roles, SignalR Live Arena, race themes, podium |

---

## 3. System architecture & engineering decision records

### 3.1 MVC architecture

`WArena.Mvc` follows the normal MVC separation. Controllers receive requests, validate authorization, call repository/service classes and build ViewModels. Razor Views are responsible for presentation and conditional UI. Entity Framework Core is isolated behind repository interfaces for domain data rather than being called directly from content controllers.

The most important folders are:

- `Models`: domain entities and the Identity user.
- `ViewModels`: page/form-specific data models.
- `Repositories`: the Data Access Layer and query logic.
- `Services`: server-side domain rules such as XP/live scoring and streak logic.
- `Controllers`: HTTP/MVC actions and authorization boundaries.
- `Views`: Razor user interface.

### 3.2 API + SPA architecture

`WArena.Api` uses controllers as the HTTP boundary, but the frontend is completely separate. Controllers return DTOs instead of database entities. Repositories perform database access, services contain reusable domain rules, and SignalR provides real-time room state updates.

The React application has its own client structure:

- `services/api.js`: generic authenticated HTTP handling.
- `services/authService.js`: login/register/session operations.
- `context/AuthContext.jsx`: client authentication state.
- `ProtectedRoute.jsx`: client-side authorization/navigation protection.
- `pages`: dashboard, questions, challenge sets, management, progress and Live Arena.

The browser never calls Entity Framework or database code directly. It only communicates with the API through the client API service layer.

### 3.3 Engineering decision: separate databases

Each sub-app uses its own SQLite database (`warena-mvc.db` and `warena-api.db`). We chose this because the requirements say the two applications must work independently. Sharing one database would make the applications more coupled and would make examiner setup less clear.

### 3.4 Engineering decision: explicit ChallengeSetQuestion join entity

A Question can belong to several Challenge Sets, while each Challenge Set contains several Questions. This is a natural N:M relationship. We use an explicit `ChallengeSetQuestion` entity instead of a hidden EF join table because it also stores `Order`. That gives us control over the order in which questions appear in a set and makes the relationship easy to explain in the report.

### 3.5 Engineering decision: role-based authorization

Role-based authorization was optional bonus functionality, but it matches the real task well. Students should not be able to edit course questions, while teachers should not necessarily have user administration rights. The roles are therefore Student, Teacher and Admin. Server-side `[Authorize]` rules are the security boundary; hiding a button in the UI is only an additional usability feature.

### 3.6 Engineering decision: Live Arena only in the SPA

The grading instructions allow the sub-applications to have distinct functionality. The core learning functionality is available in both apps. Live Arena is implemented in the API + SPA because a client-side framework and SignalR are a better fit for frequent real-time updates and animations. This keeps the MVC app understandable while still demonstrating a richer experience in the SPA.

---

## 4. Database structure and access

### 4.1 Core entities

`Category` groups questions by topic, for example HTML, CSS or ASP.NET Core.

`Question` contains the title, prompt, correct answer, question type, difficulty, points and category. A question may be short-answer or multiple-choice. For multiple-choice questions, four options can be stored. Server-side validation checks that all four options exist and that the correct answer matches one of them.

`ChallengeSet` contains a name, description and publication status. The associated questions are stored through `ChallengeSetQuestion`, which contains the two foreign keys and the question order.

`Submission` records the user, question, submitted answer, correctness, earned XP and timestamp. Submission history is therefore not calculated from temporary browser state; it is persistent domain data.

`Achievement` describes a badge condition. `UserAchievement` links a badge to a user when it is earned.

`ApplicationUser` extends ASP.NET Identity and stores learning-specific progress fields such as total XP, current streak and last activity date.

The API database additionally contains `LiveRoom`, `LiveParticipant` and `LiveAnswer`. These store the selected Challenge Set, room code/theme/status, player scores and one answer per user/question.

### 4.2 Repository Pattern and DAL

The applications do not place general EF Core queries directly inside UI components. Repository interfaces describe the data operations required by the application. Examples are `IQuestionRepository`, `IChallengeSetRepository`, `ICategoryRepository`, `ISubmissionRepository`/`IProgressRepository`, and `ILiveArenaRepository`.

Search logic is part of the DAL. A question search can combine several parameters at the same time: text, category, difficulty, minimum XP and maximum XP. Challenge Sets can be filtered by text and minimum question count, and management views can also filter by publication state.

Database calls use async EF Core APIs such as `ToListAsync`, `FirstOrDefaultAsync`, `AnyAsync`, `CountAsync` and `SaveChangesAsync`.

### 4.3 Database setup

For this MVP we use `EnsureCreatedAsync()` and seed sample data automatically. The main reason is examiner convenience: after packages are restored, no separate SQL Server/PostgreSQL installation or migration command is required. The trade-off is that schema evolution is limited. During development, if the model changes, the local SQLite file should be deleted so the app can recreate it. A production version would use EF Core migrations.

---

## 5. Feature breakdown & traceability

### 5.1 Content CRUD

Both apps support full Create, Read, Update and Delete operations for Categories, Questions and Challenge Sets. This exceeds the pass requirement and satisfies the performance requirement of at least three domain entities with CRUD. Auth Identity tables are not counted toward this requirement.

Deleting a Category is intentionally blocked while Questions still reference it. This prevents accidental invalid domain data. Deleting a Question removes its ChallengeSetQuestion links through the configured relationship, but does not delete the Challenge Sets themselves.

### 5.2 Individual practice and Challenge Sets

Students can browse individual questions or select a Challenge Set. The same Question can be reused across several sets. Answers are checked on the server. The correct answer is not included in the student-facing API DTO, so the React client cannot simply read it from the API response.

### 5.3 XP and complex domain rule

The practice scoring rule is implemented in `GamificationService`. A correct answer is trimmed and compared case-insensitively. If the student has never completed that question correctly, the full question XP is awarded. If the student already completed it, a repeat correct answer awards 20% of the normal XP. Wrong or blank answers award 0 XP.

This is a complex domain rule because the result depends on the answer itself and on previous domain state (whether the user already has a correct submission for that question).

### 5.4 Streaks and achievements

When a student answers correctly, the server updates their activity streak. Activity on the same day keeps the streak unchanged, activity on the next day increments it, and a gap of more than one calendar day resets the streak to one.

After correct activity the repository checks achievement definitions that the user has not already earned. Example seeded badges include First Steps, Ten Correct, On Fire and Streak Master.

### 5.5 Live Arena

A Teacher/Admin chooses an existing Challenge Set, a question time limit and a race theme. “Random” chooses cars, horses, rockets or boats. Students join using the generated room code.

For each active question the server calculates elapsed time from the server-side question start timestamp rather than trusting the browser timer. A correct answer within the time limit gets a base score plus a speed bonus. A wrong answer or an answer after the time limit gets 0. The leaderboard is sorted by total score, then number of correct answers, then total response time.

SignalR broadcasts the updated room state. The React UI turns the same scores into a race animation. The race is therefore presentation only and cannot change the scoring result. The top three are shown continuously, and when the room is completed the final three are shown on an animated podium.

---

## 6. Security, resilience & testing

### 6.1 Authentication and authorization

The MVC app uses ASP.NET Identity cookie authentication. The API uses the same Identity user/role store but issues a signed JWT to the SPA. API endpoints validate the token before protected data is returned. Live Arena SignalR connections pass the bearer token when connecting to the hub.

Authorization is enforced on the server. Student requests cannot call Teacher/Admin CRUD actions even if the user manually constructs the URL. The SPA also has protected routes and role-aware navigation, but these are usability features rather than the security boundary.

The JWT signing key included in `appsettings.json` is a development/MVP key so the examiner can run the application without secret configuration. A production deployment should move it to an environment variable/secret store and rotate it.

### 6.2 Validation and error handling

Models/DTOs use DataAnnotations for required fields, length limits and numeric ranges. Both frontends also provide client-side validation. Business-specific validation, such as four required options for a multiple-choice question, is checked again on the server.

MVC uses the application error route outside development and returns `NotFound`, `BadRequest` or validation errors for expected cases. The API uses ASP.NET ProblemDetails/exception handling together with appropriate 400, 401, 403, 404 and 409 responses. React catches failed API calls and displays readable error messages instead of crashing the page.

`ILogger` is used for important operations such as login failures, registration, content changes, submissions and Live Arena room events. Passwords and raw authentication tokens are not written to logs.

### 6.3 Unit tests

The MVC test project contains 14 tests for the `GamificationService`, exceeding the required minimum of 12. Positive tests cover first correct completion, case-insensitive answers, trimmed answers, the 20% repeat-correct XP rule, live time bonuses and streak behaviour. Negative/boundary tests cover incorrect answers, blank answers, wrong live answers, late live answers, invalid time values and streak resets.

These tests focus on the domain rules rather than Razor HTML, which keeps them fast and independent of a browser/database. Before submission the group should run:

```bash
dotnet build WArena.sln
dotnet test WArena.Mvc.Tests/WArena.Mvc.Tests.csproj
cd warena-spa
npm install
npm run build
```

Any warning or failing test should be reviewed before delivering the project.

---

## 7. AI usage and verification

ChatGPT was used as a development assistant during planning and implementation. We used it to discuss the original gamification idea, brainstorm the name “WArena — Web Applications, Gamified”, explore UI layouts, plan the original MVC structure, troubleshoot Razor/CSS issues, review the updated grading requirements and redesign the project around two independent sub-applications.

AI assistance was also used to suggest implementation patterns for challenge sets, user roles, XP/streak rules, achievement badges, Live Arena scoring and project documentation. Some code was drafted with AI assistance, including example controller/service/repository structures and frontend components.

AI output was not treated as automatically correct. The group’s verification process should include reading and understanding the code, checking that the implementation matches the assignment, testing normal and invalid input, running the backend unit tests, building all .NET projects, running the SPA production build, and manually testing each role. The documentation should be updated if the group changes the code after this draft.

Examples of errors found during earlier AI-assisted work included an HTML table with a nested `<td>`, Razor expression formatting, and missing frontend library files. These were corrected during development. This is also why the final project uses explicit build/test instructions instead of assuming generated code works.

---

## 8. User experience & usability

Both applications use a consistent WArena identity with a dark navy/purple header, clear content cards, status badges and responsive layouts. Navigation is based on user role so students are not shown management links they cannot use.

The main learning flow is intentionally short: choose a question/set, answer, get immediate feedback, continue. The dashboard provides meaningful dynamic content instead of static text: XP, completed questions, accuracy, streak and recent badges.

Challenge Sets are presented separately from the individual question bank because they serve different intentions. Individual practice is useful when a student wants to pick any topic. Sets are useful when the student wants a structured group such as “HTML Basics”. Search and filters reduce the amount of scrolling when the question bank grows.

The SPA Live Arena uses animation as feedback rather than as the source of scoring. Race positions move when the leaderboard changes, and the top-three panel gives a simpler text representation at the same time. This helps users who do not want to interpret the animation. The final podium gives a clear end state.

Responsive CSS changes multi-column content into a single-column layout on small screens. Controls use normal buttons, links, inputs, radios and selects so keyboard/mobile interaction remains familiar. Reduced-motion preferences disable the main race/podium transitions.

---

## 9. Conclusion & self-reflection

WArena demonstrates that the gamified learning concept is technically possible in both required architectures. The MVC version focuses on clear server-side MVC structure, while the API + SPA version demonstrates a separated frontend/backend architecture and adds a real-time multiplayer experience.

The strongest part of the design is that questions are reusable domain objects rather than being tied to one quiz. This makes individual practice, Challenge Sets and Live Arena use the same source of content. The role model also maps naturally to the task: students learn, teachers manage course material, and admins manage access.

The main MVP limitations are that SQLite and `EnsureCreatedAsync()` are intended for easy project setup rather than production operation, the JWT configuration uses a submission-friendly development key, and Live Arena room state is designed for a single API instance rather than a distributed cloud environment. A larger system would use EF migrations, secure external configuration, stronger token/session architecture, automated integration tests and a distributed SignalR/backplane solution if several API instances were required.

Possible future work includes teacher analytics, richer achievement rules, coding tasks with sandboxed execution, configurable question timers, question import/export and more Live Arena themes. These are not necessary for the proof of concept, but the current domain structure leaves room for them.

Overall, the project satisfies the core learning goal while deliberately connecting technical grading requirements to useful product features instead of adding unrelated entities or artificial relationships only for the rubric.


### Later usability refinements

Usernames are unique and can be changed by the account owner; administrators can manage other users, reset passwords, change roles, or delete an account together with its related learning data. Live Arena includes an optional Auto-continue mode. Its race position uses the maximum possible score for the whole session, which prevents racers from jumping backwards when a new question begins. Cars, horses, and boats are mirrored so they face the finish line, while rockets keep their original direction.
