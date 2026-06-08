# UniParser (Kaspi Telegram Scraper Bot)

An asynchronous, containerized Telegram bot built on the .NET 10 platform. It provides automated background price tracking and item monitoring for the Kaspi marketplace. The project leverages Microsoft Playwright to bypass dynamic client-side rendering using a headless Chromium instance.

---

## Features:

* **Asynchronous Background Worker:** Continuous price and catalog monitoring using non-blocking cyclic tasks.
* **Playwright Automation:** Full headless Chromium integration pre-configured to run flawlessly inside isolated Linux environments.
* **Self-Ensuring SQLite Database:** Automated initialization using entity models (`EnsureCreated`) to seamlessly provision schemas on the fly.
* **Hardened Architecture:** Robust separation of business logic, production-ready multi-stage Docker builds, and native Docker layer cache pruning configurations.

---

## Tech Stack:

* **Runtime:** .NET 10 (C#)
* **Scraping Engine:** Microsoft.Playwright (Chromium Headless Shell)
* **Bot Framework:** Telegram.Bot
* **Database & ORM:** SQLite
* **Containerization:** Docker (Multi-stage Ubuntu target)

---

## Docker Deployment (Recommended)

This project features a fully autonomous Multi-stage Dockerfile that provisions the .NET runtime, fetches Node.js dependencies, configures system-level graphic libraries (`libnss3`, `libgbm1`, etc.), and installs Chromium automatically inside the container.

### 1. Configuration Check (.gitignore & .dockerignore)
Ensure that local build caches, runtime tokens, and actual database instances never leak into source control or image contexts. Your root directory contains tailored exclusion lists hiding `bin/`, `obj/`, `*.db`, and `.playwright-browsers/`.

### 2. Clean Container Build
I got some caching bugs during active iteration, so I recommend prune the BuildKit engine garbage collector before compilation:


docker builder prune -a -f

docker build --no-cache -t uniparser-bot:v1 .

### 3. Persistent Run Configuration
Run the container in detached (background) mode.
```
docker run -d --name parser-bot -e TELEGRAM_TOKEN="YOUR_TOKEN" uniparser-bot:v1
```
---

## Local Workspace Startup (Windows Native)

Navigate to your project directory via PowerShell and seed the internal Playwright tools:
```
cd UniParser
dotnet tool restore
dotnet build
pwsh ./.playwright/win-x64/playwright.ps1 install chromium
$env:TELEGRAM_TOKEN="YOUR_TOKEN"
dotnet run --project UniParser.csproj
```
**Warning:** Program can't be stopped with ENTER button, use Ctrl + C.

---

## Known Issues & Workarounds

While the core application is fully functional, please keep in mind the following environmental and platform limitations:

### 1. Docker BuildKit Cache Lock (WSL2 Engine)
* **Issue:** Sequential executions of `docker build` may occasionally hang or crash due to file-locking conflicts inside the WSL2 virtual disk garbage collector.
* **Workaround:** Force-clear the internal builder cache layers prior to running a new compilation by executing: `docker builder prune -f`.


---

## Key Takeaways (My First Project)

As my introductory project in software development, UniParser allowed me to gain hands-on experience with core production concepts:
* **Advanced Orchestration:** Moving from native host execution to fully isolated, multi-stage Linux Docker containers.
* **Resource Management:** Handling asynchronous lifecycles (`Task.Delay` instead of blocking UI inputs) to keep background tasks alive in daemon mode.
* **Problem Solving:** Debugging complex environment bugs, from WSL2 build cache locks to dynamic database generation (`EnsureCreated`).

---

## Roadmap

Here is a list of planned features and structural improvements for the upcoming iterations of UniParser:

- [X] **Unsubscribe command Update** Make "/Unsubscribe" command to allow the removal of links from checked positions with the removal of the link from the database in the event of a lack of subscribers.
- [X] **Stealth Scraper Updates:** Integrate randomized delay intervals and dynamic User-Agent rotation inside the `PriceCheck` execution loop to prevent marketplace blocks.
- [ ] **Persistent Data Storage:** Refactor the configuration layer to accept dynamic database file paths via environment variables, enabling proper Docker Volume mounting.
