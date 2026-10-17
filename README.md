# Rubik's Cube Simulator

A 3×3 Rubik's cube that correctly rotates any face: a C# (.NET 10) engine and Web API with a React + three.js UI. It is hosted on [Render](https://render.com) (free tier, Frankfurt) and redeploys from `main` after CI passes.

| What | URL |
|---|---|
| App (3D cube, net view, moves, scramble, solve) | <https://rubik-xi5x.onrender.com> |
| Swagger UI (API docs, try it out) | <https://rubik-xi5x.onrender.com/swagger> |
| Health check | <https://rubik-xi5x.onrender.com/health> |
| Source | <https://github.com/GVLTodorov/Rubik> |

> The free tier sleeps after about 15 minutes without traffic, so the first visit can take up to a minute to load.

| Project | Line coverage | Branch coverage |
|---|---|---|
| Backend | ![Backend line coverage](https://github.com/GVLTodorov/Rubik/raw/badges/coverage/backend/line.svg) | ![Backend branch coverage](https://github.com/GVLTodorov/Rubik/raw/badges/coverage/backend/branch.svg) |
| Frontend | ![Frontend line coverage](https://github.com/GVLTodorov/Rubik/raw/badges/coverage/frontend/line.svg) | ![Frontend branch coverage](https://github.com/GVLTodorov/Rubik/raw/badges/coverage/frontend/branch.svg) |

![Demo: Swagger UI opens, its UI button opens the app, Scramble plays, then Solve plays the solution stage by stage.](docs/demo.gif)

## Run it

You need the .NET 10 SDK and Node.js 22 or later (see [Prerequisites](#prerequisites)). The first Debug build of `Rubik.Api` runs `npm ci` in `frontend/` if its packages aren't installed yet, so a fresh clone needs no extra step.

Start it one of two ways. Both start the API on <http://localhost:5080> and the React app on <http://localhost:5173>, and open Swagger at <http://localhost:5080/swagger>. Click the green-outlined **UI** button in Swagger's top bar to open the app.

- **Visual Studio**: open `backend/Rubik.sln` and press **F5** (`Rubik.Api` is the default startup project). The API starts the Vite dev server itself, or reuses one already running on port 5173. Stopping the API stops it too.
- **VS Code**: open the `Rubik` folder, install the recommended extensions when prompted (C# Dev Kit, ESLint, Prettier), press **F5** and choose **Backend + UI**. Stopping the debug session stops both.

CI ([`ci.yml`](.github/workflows/ci.yml)) regenerates the coverage badges on every push to `main` and stores them on the `badges` branch. Backend numbers combine unit and integration tests. The demo above is recorded by the [Demo workflow](.github/workflows/demo.yml) (see [Record the demo](#record-the-demo)).

A 3×3 Rubik's cube simulator that correctly rotates any face. It has three parts:

- **Rubik.Core**: a pure C# cube engine with no I/O and no ASP.NET dependency.
- **Rubik.Api**: an ASP.NET Core Web API (.NET 10) with Swagger UI. Every endpoint is on one `CubeController` under `/api/cube`: in-memory sessions (`/api/cube/{id}`) that keep a cube and its undo/redo history, scrambles, and the solver.
- **frontend**: a React + TypeScript app (Vite) with an animated 3D cube, a 2D net view, move buttons, keyboard shortcuts, undo/redo and scrambles.

A small **console runner** prints the brief's required test output without starting anything else.

The cube starts **solved, with Green front, Red right and White up**, the same as [rubiks-cube-solver.com](https://rubiks-cube-solver.com/).

---

## Prerequisites

All tools are free and run on Windows.

| Tool | Version | Download |
|---|---|---|
| .NET SDK | 10.0 or later | <https://dotnet.microsoft.com/download/dotnet/10.0> |
| Node.js (includes npm) | 22 LTS or later | <https://nodejs.org/en/download> |
| Git | any recent | <https://git-scm.com/download/win> |
| VS Code *(optional)* | any recent | <https://code.visualstudio.com/> |
| Docker Desktop *(optional, for the image)* | any recent | <https://www.docker.com/products/docker-desktop/> |

Check them:

```powershell
dotnet --version   # 10.x
node --version     # v22.x or later
```

## Get the code

```powershell
git clone https://github.com/GVLTodorov/Rubik.git
cd Rubik
```

---

## Console runner

Prints the brief's test output.

```powershell
dotnet run --project backend/Rubik.Console
dotnet run --project backend/Rubik.Console -- "R U R' U'"    # any other sequence
dotnet run --project backend/Rubik.Console -- --plain         # letters only, no ANSI colours
```

Colours are shown as ANSI blocks when the terminal supports them. The runner falls back to plain letters when output is redirected or `NO_COLOR` is set.

## Run with Docker

One image holds the API and the React UI (served by nginx), the same setup as [Browse](https://github.com/GVLTodorov/Browse). It uses the same ports as local development, so every URL in this README works unchanged.

```powershell
docker build -t rubik .
docker run --rm -p 5080:5080 -p 5173:5173 rubik
```

- App: <http://localhost:5173>. nginx forwards `/api` and `/health` on this port to the API, so the app calls it same-origin, as the Vite dev proxy does.
- Swagger UI: <http://localhost:5080/swagger>

The image builds in three stages: `npm run build` on `node:24-slim`, `dotnet publish` on the .NET 10 SDK, and a runtime on `aspnet:10.0` with nginx added. [docker/entrypoint.sh](docker/entrypoint.sh) runs nginx and the API side by side. If either one exits, the container stops instead of half-running. Everything runs as the image's non-root `app` user. Tests are not run inside the image; CI runs them before it builds the image.

CI pushes the image to `ghcr.io/gvltodorov/rubik` on pushes to `main` (tags `main`, `latest`, `sha-…`), on releases (semver tags) and on manual runs. Pull requests only build it.

### Hosted on Render

The app is live at <https://rubik-xi5x.onrender.com> (Swagger at <https://rubik-xi5x.onrender.com/swagger>). [render.yaml](render.yaml) describes the free [Render](https://render.com) web service that runs the same image. Render's own auto-deploy is off. Instead, the **Deploy to Render** job in CI calls the service's deploy hook (repository secret `RENDER_DEPLOY_HOOK_URL`) after the tests pass and the image builds, so a failing commit never goes live. Render routes a single port, so the service uses port 8080. On that port nginx serves the app at `/` and forwards `/api`, `/health` and `/swagger` to the API. `links__ui=/` points Swagger's **UI** button at the same origin. To try that setup locally:

```powershell
docker run --rm -p 8080:8080 -e links__ui=/ rubik
```

Free services sleep after about 15 minutes without traffic. The first request after that takes about a minute while the service wakes, and in-memory cube sessions start fresh.

## Run the tests

```powershell
dotnet test backend/Rubik.Test                    # 484 unit tests: engine, timeline, parser, scramble, renderer, solver
                                                  #   (300 random scrambles, every stage, unsolvable states), API controllers,
                                                  #   session store, error handler, config, console formatting
dotnet test backend/Rubik.Integration.Test        # 39 integration tests: the API hosted in memory (endpoints, solver, errors,
                                                  #   CORS, Swagger) and the console runner end to end
dotnet test backend                               # both (NUnit, like Browse)

cd frontend
npm test                            # 75 frontend tests: model, notation, geometry, API client, net view, session,
                                    #   solution panel, app
npm run test:coverage               # the same, plus a coverage report in frontend/coverage/
npm run lint                        # ESLint (typescript-eslint strict)
npm run format:check                # Prettier
npm run build                       # type-check + production build
```

To see backend coverage locally, add `--collect:"XPlat Code Coverage"`. Each project then writes a `coverage.cobertura.xml` under its `TestResults/` folder.

### CI

[.github/workflows/ci.yml](.github/workflows/ci.yml) runs on `ubuntu-latest`. It works like Browse's:

| Job | What it does |
|---|---|
| **backend tests** | restore, build, `dotnet format` check, unit tests, integration tests, the console acceptance output, a merged coverage report (job summary and badges), and a test report from the `.trx` files |
| **frontend tests** | `npm ci`, lint, Prettier check, tests with coverage, production build, and a coverage report (job summary and badges) |
| **Publish coverage badges** | on `main` pushes only: commits the line and branch badges to the `badges` branch that the table at the top links to |
| **Docker image** | after both test jobs pass: builds the image, and pushes it to GHCR unless the run is a pull request |

### Record the demo

`docs/demo.gif` is recorded by `backend/Rubik.Demo`, a Playwright console app that drives a real Chromium. It opens Swagger UI, clicks the **UI** button, clicks **Scramble**, waits for the cube to settle, then clicks **Solve** and waits for "Solved in N moves". Every wait targets an element, so a broken flow fails with a timeout. That makes it an end-to-end smoke test too. It is not part of `dotnet test`.

The [Demo workflow](.github/workflows/demo.yml) is manual: run it from the Actions tab. It starts the Docker image, records both tabs, joins them into `docs/demo.gif` with ffmpeg, and commits the GIF to `main`. To record locally, start the app (Docker, Visual Studio or VS Code), then run:

```powershell
dotnet build backend/Rubik.Demo
pwsh backend/Rubik.Demo/bin/Debug/net10.0/playwright.ps1 install chromium   # once
dotnet run --project backend/Rubik.Demo -- http://localhost:5080 artifacts/demo-video
```

It prints one `VIDEO_PATH=` per tab (Swagger, then the app) as `.webm` files.

Frontend coverage leaves out `main.tsx` (bootstrap only) and `Cube3D.tsx`, the WebGL scene. jsdom cannot render WebGL, so the app tests mock that component, and the maths it uses lives in `cube/geometry.ts`, which is tested. Backend coverage leaves out source-generated logging code (`*.g.cs`).

---

## Expected output (acceptance test)

Starting from solved, the brief applies `F R' U B' L D'`:

| Step | Move | Meaning |
|---|---|---|
| 1 | `F` | Front clockwise |
| 2 | `R'` | Right anti-clockwise |
| 3 | `U` | Up clockwise |
| 4 | `B'` | Back anti-clockwise |
| 5 | `L` | Left clockwise |
| 6 | `D'` | Down anti-clockwise |

`dotnet run --project backend/Rubik.Console` prints:

```
        R O G
        B W W
        B B B

G Y Y   O R R   Y B O   Y B W
O O G   O G W   R R W   O B Y
B G O   W W W   O Y R   Y Y W

        G G B
        R Y R
        R G G
```

The middle row is **L F R B**. Colours: W = White, O = Orange, G = Green, R = Red, B = Blue, Y = Yellow.

The same result is checked in three places:

- A unit test in `Rubik.Test`.
- Integration tests in `Rubik.Integration.Test`, on `POST /api/cube/{id}/moves` for a new session and on the console runner's output.
- The frontend's net-view and model tests.

To see it in the UI, open the **2D net** tab, type `F R' U B' L D'` in the sequence field and press ✓.

---

## Using the app

| Action | How |
|---|---|
| Turn a face | Buttons `F R U B L D` (clockwise) and `F' R' U' B' L' D'` (anti-clockwise), or keys `F R U B L D` / `Shift` + key |
| Look around | Drag the cube, or use the ⟲ ⟳ ⤒ ⤓ buttons; ⌂ resets the view. This moves the camera only, never the cube's state. |
| Apply a sequence | Type it (e.g. `F R' U2`) and press ✓ to apply it at once, or ▶ to play it move by move. Invalid tokens are highlighted with the API's error. |
| Scramble / Reset | **Scramble** fetches 20 random moves from the API, shows them and animates them. **Reset** returns to solved and clears history. |
| Solve | **Solve** asks the API for a layer-by-layer solution and plays it move by move at the **Turn speed** (change the speed while it plays). The **Solution** panel lists the 7 stages with their moves, and highlights the current stage, its goal and the move on screen. The moves go into the history, so Undo still steps back one move at a time. Any other action hides the panel. |
| Undo / Redo | Buttons, or `Ctrl+Z` / `Ctrl+Y` |
| Views | **3D** tab (default) or **2D net** tab; both always show the same state |
| Share | The address bar holds `?session=<id>`. Copy the link to reopen the same cube, history and redo stack, in another tab or by someone else. Links work until the session expires (60 minutes unused by default) or the API restarts; then a new session starts. |
| Speed | The **Turn speed** slider (50–1000 ms per turn, default 250 ms) |

The **?** button lists the keyboard shortcuts.

## API

The cube and its history are kept on the server in a session:

| Method | Route | Body / query | Response |
|---|---|---|---|
| `POST` | `/api/cube` | none | `201 Created`, `Session` (solved) |
| `GET` | `/api/cube/{id}` | none | `Session` |
| `POST` | `/api/cube/{id}/moves` | `{ "moves": "F R' U" }` | `{ "session": Session, "appliedMoves": [...] }` |
| `POST` | `/api/cube/{id}/undo` | none | same; `appliedMoves` is the inverse move turned |
| `POST` | `/api/cube/{id}/redo` | none | same; `appliedMoves` is the move turned again |
| `POST` | `/api/cube/{id}/reset` | none | same; back to solved, history cleared |
| `GET` | `/api/cube/{id}/solution` | none | `Solution` (see below) for the session's cube; the session is not changed |
| `GET` | `/api/cube/scramble` | `?length=20&seed=123` (length 1–100, seed optional) | `{ "moves": "R U2 F' ..." }` |
| `GET` | `/health` | none | `200 Healthy` |

`Session` is `{ "id", "state": CubeState, "history": [...], "redoStack": [...] }`. An unknown or expired id returns `404`, and undo/redo with nothing to do returns `409`.

Scramble and solution only suggest moves; the app then sends them to `/api/cube/{id}/moves`. `Solution` is `{ "moves": "F' D R2 ...", "moveCount": 111, "steps": [{ "stage": "Cross", "title": "White cross", "description": "...", "moves": ["F'", "D"] }, ...] }`. There are always 7 steps, in order: `Cross`, `FirstLayerCorners`, `MiddleLayer`, `LastLayerCross`, `LastLayerFace`, `LastLayerCorners`, `LastLayerEdges`. A stage that is already done has no moves.

`CubeState` is `{ "faces": { "U": [9 codes], "L": [...], "F": [...], "R": [...], "B": [...], "D": [...] } }`, using colour codes `W O G R B Y`. Invalid input returns `400` with RFC 7807 `application/problem+json`. An invalid move also includes `token` and `position`:

```json
{ "title": "Invalid move", "status": 400, "detail": "Invalid move 'Q' at position 2 (move #2). ...", "token": "Q", "position": 2 }
```

### Configuration

| Setting | Where | Default |
|---|---|---|
| API URL | `backend/Rubik.Api/Properties/launchSettings.json`, `.vscode/launch.json` | `http://localhost:5080` |
| Swagger **UI** button target | `links:ui` in `appsettings.json` | `http://localhost:5173` |
| CORS origins | `cors:origins` in `appsettings.json` | `["http://localhost:5173"]` |
| Session expiry (minutes unused) | `sessions:slidingExpirationMinutes` in `appsettings.json` | `60` |
| API base URL used by the app | `VITE_API_BASE_URL` (see `frontend/.env.example`) | unset, so the Vite proxy forwards to `http://localhost:5080` |

---

## Architecture

```
backend/
  Rubik.Core/            Cube, Move, FaceTurn (the turn mechanism), CubeTimeline (undo/redo),
                         MoveParser, ScrambleGenerator, NetRenderer,
                         Solving/ (BeginnerSolver: layer-by-layer method, see below)
  Rubik.Api/             Program (generic host) + Startup (services and pipeline; no minimal APIs),
                         CubeController (every endpoint: sessions in IMemoryCache, scramble, solver),
                         CubeExceptionHandler (ProblemDetails), SwaggerExtension (UI button)
  Rubik.Console/         Prints the acceptance net
  Rubik.Test/                    Unit tests, no web host: Core/ (including a 3D reference model that
                                 cross-checks the turn table), Api/ (controller, error handler, config,
                                 Swagger button) and Console/ (ANSI formatting)
  Rubik.Integration.Test/        WebApplicationFactory tests (endpoints, errors, CORS, Swagger) and the
                                 console runner run end to end
  Rubik.Demo/                    Playwright recorder for docs/demo.gif (Swagger → UI → Scramble → Solve)
docker/                  nginx.conf and entrypoint.sh for the Docker image
frontend/src/
  api/          Typed API client (fetch + ProblemDetails → ApiError)
  cube/         Types, notation, sticker geometry, local move model (drives animations)
  session/      CubeSession: holds the server session id, serialises API calls, queues animations,
                tracks a playing solution's stage and move
  components/   Cube3D (react-three-fiber), CubeNet, MoveButtons, SequenceInput, Toolbar, SolutionPanel, ...
```

### Key design decisions

- **Sticker model.** A cube is 54 stickers: 6 faces × 9, indexed 0–8 row-major as seen from outside each face. `U`'s top row is next to `B`, `D`'s top row is next to `F`, and the side faces have `U` at the top. This is the same layout as the net, so rendering is a direct read. `Cube` is immutable: `Apply` returns a new cube.
- **One rotation algorithm, made of 4-cycles.** A clockwise turn is five 4-cycles (A → B → C → D → A): the face's own corners `0 → 2 → 8 → 6`, its edges `1 → 5 → 7 → 3`, and three cycles through the 12 neighbouring stickers. The only face-specific part is data: `FaceTurn` lists the 4 neighbouring 3-sticker strips for each face. Anti-clockwise is three quarter turns and a half turn is two. A unit test checks the table for every face against an independent model that places every sticker in 3D space and rotates it 90°.
- **Server-side sessions.** `POST /api/cube` starts a session, and the server keeps the cube, history and redo stack in memory under a GUID (`/api/cube/{id}`). The URL then only needs `?session=<id>` instead of every move. The undo/redo rules are a small mutable `CubeTimeline` in Core. `CubeController` keeps one timeline per session in `IMemoryCache`, locks it for each request so concurrent requests can't lose moves, and lets it expire after a period without use. Because sessions are in-memory, they are lost on restart and not shared across API instances.
- **Backend is the source of truth.** Every UI action is one request to the session (moves, undo, redo or reset). Requests run one at a time, and each response reports the moves actually turned. The frontend animates the returned moves with a local copy of the model, then re-syncs to the server's state at the end of each batch. A mismatch is logged and the server wins.
- **Camera, not state.** Orbiting and the view buttons move the camera only. The cube's orientation is fixed: Green front, Red right, White up.
- **Solver: the beginner's method, so every move is explainable.** `BeginnerSolver` solves the White (U) layer first: the cross, then the corners. Then it does the middle layer. Last, the Yellow (D) layer: cross, face, corners, edges. A solution averages about 110 moves (67–168 over 2,000 random scrambles) and takes about 2 ms; an optimal solver would need about 20 moves but couldn't explain them. The centres never move, so textbook algorithms, written as "last layer on top, this face in front", are renamed onto the real faces (`ViewFrame`) instead of rotating the cube. Each stage then runs a small, bounded search over those algorithms and D-layer turns (`MacroSearch`). Its goal is "the next piece is solved and nothing solved earlier was disturbed", so there are no hand-written case tables to get wrong. Searches use a 54-byte sticker array whose move tables are built by `Cube`'s own turn code, so the solver and the engine can't disagree. If no search succeeds, the cube can't be solved.

### Not included

Camera scanning, other cube sizes, slice/wide/whole-cube moves and colour painting are out of scope. Beyond the brief, shareable links (`?session=<id>`) and a solver (the beginner's method) are implemented. The parser accepts 180° moves (`F2`), but there are no 180° buttons.
