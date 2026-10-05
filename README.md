# HR Management System

Web app I built to manage day-to-day HR work: employees, attendance, leave, payroll, and reports.

**Demo:** https://hr-system-prod-999.web.app

## Stack

- **Frontend:** Angular 19 (TypeScript), English + Khmer
- **Mobile:** Flutter (`Frontend/Flutter`)
- **Backend:** ASP.NET Core 8 Web API, JWT auth
- **Database:** SQL Server
- **Hosting:** Firebase Hosting (UI) → Cloud Run (API)

## What it does

- Login / forgot password / change password
- Role-based access (routes and API checks)
- Employee management
- Attendance tracking
- Leave requests and balances
- Payroll
- Report export to Excel
- Dashboard summary

## Project structure

```
Api/                 ASP.NET Core API + Dockerfile
Frontend/Angular/    Web UI + Firebase config
Frontend/Flutter/    Mobile client
```

## Run locally

**API**

```bash
cd Api
# set ConnectionStrings and Auth:SigningKey in appsettings.json
dotnet run
```

Runs on `http://localhost:5088`.

**Angular**

```bash
cd Frontend/Angular
npm install
npm start
```

Runs on `http://localhost:4200` (`/api` proxied to the local API).

## Deploy notes

- UI: Firebase Hosting (`npm run deploy:web` in `Frontend/Angular`)
- API: Docker image on Cloud Run (`Api/Dockerfile`)
- Firebase rewrites `/api/**` to the Cloud Run service

Keep real keys and DB passwords out of git. Use placeholders in `appsettings.json` and secrets in the cloud.
