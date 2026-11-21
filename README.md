# College ERP — multi-college management platform

A college management system built to be handed to **any number of colleges**.
A platform admin onboards a college in one step, and the system **creates a separate
database for that college automatically**. College data is never mixed in a shared
database.

| Layer    | Tech |
|----------|------|
| Backend  | **.NET 10** (ASP.NET Core Minimal APIs), EF Core 10, JWT auth, BCrypt |
| Frontend | **React 19** (form Actions, `useActionState`, `use`), React Router 7, Vite 8 |
| Database | **PostgreSQL** (production) or **SQLite** (no-setup dev), selected by config |

---

## 1. How multi-tenancy works (database per college)

```
                ┌──────────────────────────── Master DB (college_master) ─────────┐
 Platform admin │  super_admins                                                    │
 /api/platform ─┤  colleges: code | name | plan | status | database_name | ...     │
                └──────────────────────────────────────────────────────────────────┘
                          │ "POST /api/platform/colleges"  → CREATE DATABASE + schema + first admin
        ┌─────────────────┼──────────────────────┬──────────────────────┐
        ▼                 ▼                      ▼                      ▼
 college_stxavier   college_greenfield     college_mit           … unlimited
 (users, students,  (its own users,        (its own …)
  courses, fees …)   students, fees …)

 Request:  /api/t/{collegeCode}/students
   1. TenantResolutionMiddleware looks up {collegeCode} in the master DB (cached 60 s)
      → 404 if unknown, 403 if suspended
   2. The request's CollegeDbContext is bound to THAT college's connection string
   3. Authorization checks that the JWT was issued by THIS college (a token from
      college A gets 403 at college B), then checks the user's role
```

* **SQLite mode:** each college is a file `data/colleges/college_<code>.db`.
* **PostgreSQL mode:** each college is a real database `college_<code>` on the server
  (`CREATE DATABASE` at onboarding, `DROP DATABASE` if the admin deletes it with
  *drop database* ticked).
* **Unlimited colleges:** nothing is held per college in memory except a short
  cache entry; ADO.NET pools are per connection string with small limits and idle
  release, so thousands of colleges can share one server. When one server is full,
  point `Database:TenantServerConnectionString` at another server for new colleges
  (or extend `IDatabaseEngine`).
* **Schema upgrades for all colleges:** versioned tenant migrations run against every
  college database at startup (or via **Upgrade all DBs** / `POST /api/platform/colleges/migrate`).
  Each database records its own version in `schema_versions`.
* **Per-college flexibility:** plan & student limit per college, suspend/activate,
  and a free-form JSON **Settings** store per college (academic year, grading, branding…).

---

## 2. Project structure (Clean Architecture + SOLID)

```
college-erp/
├── backend/
│   ├── CollegeErp.slnx
│   ├── src/
│   │   ├── CollegeErp.Domain/          Entities + domain rules. No dependencies.
│   │   ├── CollegeErp.Application/     Use cases (services), DTOs, interfaces it NEEDS
│   │   │   ├── Abstractions/           IMasterDbContext, ICollegeDbContext, ITenantDatabaseManager,
│   │   │   │                           ICurrentTenant, ITokenService, IPasswordHasher, ICurrentUser …
│   │   │   ├── Platform/               College onboarding/administration, super admins
│   │   │   └── Campus/                 Everything inside one college
│   │   │       ├── Crud/               Generic CRUD + extension points (rules, filters)
│   │   │       ├── Academics/          Departments, faculty, students, courses, enrollment,
│   │   │       │                       attendance, exams & marks, course access policy
│   │   │       ├── Finance/            Invoices & payments
│   │   │       ├── Identity/           College login & user accounts
│   │   │       └── Portal/             Student self-service
│   │   ├── CollegeErp.Infrastructure/  Implements the abstractions
│   │   │   ├── Databases/              IDatabaseEngine strategy: SQLite, PostgreSQL
│   │   │   ├── Persistence/            EF Core MasterDbContext / CollegeDbContext
│   │   │   ├── Tenancy/                CurrentTenant, cached directory, DB manager, migrations
│   │   │   └── Security/               BCrypt hasher, JWT issuer
│   │   └── CollegeErp.Api/             Composition root, middleware, policies, endpoints
│   └── tests/CollegeErp.IntegrationTests/   End-to-end API tests (isolation, roles, workflow)
├── frontend/                           React 19 SPA
│   └── src/
│       ├── api/          http.js (transport), platformApi.js, collegeApi.js (per-college client factory)
│       ├── auth/         AuthContext (session; plugs token into http via configureHttp)
│       ├── components/   DataTable, EntityForm (schema-driven), ResourcePage (generic CRUD), ui
│       ├── config/       resources.js (screen definitions), navigation.js (role → menu)
│       └── pages/        platform/* (super admin), college/* (staff + student portal)
├── Dockerfile            Builds frontend + API into one image
└── docker-compose.yml    PostgreSQL + app
```

### Where each SOLID principle shows up

| Principle | Examples |
|-----------|----------|
| **S**ingle responsibility | `CollegeOnboardingService` only onboards; `CollegeAdministrationService` only manages; `TenantResolutionMiddleware` only resolves the tenant; `AppExceptionHandler` only maps errors; `ReferenceGuard` only validates foreign keys; on the frontend `http.js` only does transport and `ResourcePage` only renders a resource. |
| **O**pen/closed | Add a business rule by registering a new `IEntityRule<T>` / `IDeleteRule<T>` (e.g. `StudentCapacityRule`) — `CrudService` is untouched. Add a database engine by implementing `IDatabaseEngine`. Add a schema change by adding an `ITenantMigration`. Add an entity screen by adding an entry to `config/resources.js`. |
| **L**iskov substitution | `SqliteDatabaseEngine` and `PostgreSqlDatabaseEngine` are interchangeable behind `IDatabaseEngine`; every `IQueryFilter<T>` (incl. `NoFilter<T>`) can replace another. |
| **I**nterface segregation | `ICurrentTenant` (read) is separate from `ITenantSetter` (write — only the middleware sees it); `ITenantDatabaseManager` (lifecycle) is separate from `ICollegeDbContextFactory` (open); `IPasswordHasher` and `ITokenService` are separate. |
| **D**ependency inversion | Application defines the interfaces; Infrastructure implements them; the API composes them (`AddApplication()`, `AddInfrastructure()`). Application never references SQLite, Npgsql, BCrypt or JWT libraries. Project references only point inward: Api → Infrastructure → Application → Domain. |

---

## 3. Running it

### Prerequisites
* .NET SDK 10
* Node.js 20+ (22 recommended)
* (optional) PostgreSQL 14+ or Docker

### Option A — local dev with SQLite (zero setup)

```bash
cd backend
dotnet run --project src/CollegeErp.Api
```
The API listens on **http://localhost:5080** and creates `data/master.db` plus one
file per college under `data/colleges/`.

In a second terminal:
```bash
cd frontend
npm install
npm run dev
```
Open **http://localhost:5173** (Vite proxies `/api` to the API).

### Option B — PostgreSQL (database server per college)

Edit `backend/src/CollegeErp.Api/appsettings.PostgreSql.json` with your server
credentials (the user needs `CREATEDB`), then:
```bash
cd backend
ASPNETCORE_ENVIRONMENT=PostgreSql dotnet run --project src/CollegeErp.Api --no-launch-profile --urls http://localhost:5080
```
(`--no-launch-profile` stops `launchSettings.json` from forcing the Development environment.)
Or set the values with environment variables:
`Database__Provider=PostgreSql`, `Database__MasterConnectionString=...`,
`Database__TenantServerConnectionString=...`.

### Option C — Docker (PostgreSQL + app in one command)
```bash
JWT_SECRET='a-long-random-secret-of-at-least-32-chars' docker compose up --build
```
Open **http://localhost:8080**.

### Default platform admin
| Email | Password |
|-------|----------|
| `admin@platform.local` | `Admin@12345` |

Change it via `Bootstrap:SuperAdminEmail` / `Bootstrap:SuperAdminPassword` **before the
first start** (it is only created when no platform admin exists).

---

## 4. Using it

1. **Platform admin** → *Colleges* → **+ New college**: enter a code (e.g. `stxavier`),
   name, plan, optional student limit and the college's first admin account.
   The database `college_stxavier` is created and migrated on the spot.
2. Give the college its portal URL: **`/c/stxavier/login`** (or the code on the home page).
3. **College admin** sets up departments, faculty, courses, students, enrollments,
   user accounts (faculty/accountant/student logins), settings.
4. **Faculty** mark attendance and enter marks for *their own* courses only.
5. **Accountant** bills students (individually or a whole semester) and records payments.
6. **Students** see their courses + attendance %, marks and fee balance.

### Roles inside a college
| Role | Can do |
|------|--------|
| `college_admin` | Everything in that college |
| `faculty` | View academics; attendance, exams and marks for courses assigned to them |
| `accountant` | View students; fees & payments |
| `student` | Own courses, attendance, marks, invoices, profile |

---

## 5. API overview

OpenAPI document: `GET /openapi/v1.json`

**Platform** (`/api/platform`, super-admin token)
| Method & path | Purpose |
|---|---|
| `POST /auth/login` | Super-admin login |
| `GET/POST /colleges` | List (search, status) / onboard a college (creates its DB) |
| `GET/PUT /colleges/{id}` | Read / update name, contacts, plan, student limit |
| `POST /colleges/{id}/suspend` · `/activate` | Block / unblock a college |
| `DELETE /colleges/{id}?dropDatabase=true` | Remove a college (optionally drop its DB) |
| `GET /colleges/{id}/stats` | Usage counts from the college's DB |
| `POST /colleges/migrate` | Upgrade every college DB to the latest schema |
| `GET/POST /admins` | Platform admins |

**College** (`/api/t/{collegeCode}`, college token)
| Method & path | Purpose |
|---|---|
| `POST /auth/login` · `GET /auth/me` | College login / current user |
| `GET /dashboard` | Counts and fee totals |
| `/departments` `/faculty` `/students` `/courses` `/exams` | CRUD (`?search=&skip=&take=` + filters such as `departmentId`, `semester`, `status`, `courseId`) |
| `/users` | Login accounts (admin only) |
| `GET/POST /enrollments`, `DELETE /enrollments/{id}` | Course enrollment per academic year |
| `POST /attendance`, `GET /attendance`, `GET /attendance/summary?courseId=` | Mark (idempotent per day) / list / percentages |
| `POST /exams/{id}/marks`, `GET /marks` | Record / list marks |
| `/fees/invoices`, `/fees/invoices/{id}/payments` | Billing (single or whole semester) and payments |
| `GET/PUT /settings` | Per-college JSON settings |
| `GET /me/profile` `/me/courses` `/me/marks` `/me/invoices` | Student portal |

Errors use RFC 7807 problem details (400 validation, 401, 403, 404, 409 conflict).

---

## 6. Extending

* **New business rule** — implement `IEntityRule<Student>` (or any entity) and register
  it in `Application/DependencyInjection.cs`.
* **New entity** — add the entity (Domain), map it in `CollegeDbContext`, add a
  `IWriteModel<T>` (+ optional `IQueryFilter<T>`), map it with one line
  `college.MapCrud<T, TWrite>(...)`, add a migration for existing colleges, and add a
  screen entry in `frontend/src/config/resources.js`.
* **Schema change for existing colleges** — see `Infrastructure/Tenancy/Migrations/TenantMigrations.cs`
  (implement `ITenantMigration`, register with `AddTenantMigration<T>()`).
* **Another database engine** (e.g. SQL Server) — implement `IDatabaseEngine` and add it
  to the provider switch in `Infrastructure/DependencyInjection.cs`.

---

## 7. Tests

```bash
cd backend
dotnet test
```
The integration tests boot the real API on throw-away SQLite databases and verify:
separate database per college, data isolation between colleges, cross-college token
rejection, platform/college scope separation, suspension, plan student limits,
duplicate handling, dropping a college database, and a full semester workflow
(departments → courses → enrollment → attendance → exams/marks → fees → student portal),
including that faculty cannot touch courses they don't teach.

---

## 8. Production checklist

* Set a long random `Jwt:Secret` (≥ 32 bytes) via environment/secret store.
* Change the bootstrap admin credentials before first start.
* Use PostgreSQL; give the app user `CREATEDB` (it creates one database per college).
* Serve over HTTPS (reverse proxy or Kestrel certificates); set `Cors:Origins` if the
  frontend is hosted on another origin (and `VITE_API_BASE_URL` at build time).
* Back up the master DB **and** every `college_*` database (they are independent, so
  each college can also be backed up/restored/exported on its own).
* The master DB uses `EnsureCreated`; if you change the master schema later, switch it to
  EF Core migrations (`dotnet ef migrations add ...`).
