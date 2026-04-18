# Automated Render PostgreSQL Free-Tier Rotation

## Context

Render's free PostgreSQL tier expires every **~30 days** and is deleted unless upgraded.
All three FinTrackPro services (API, UI, DB) run on Render's free tier in Oregon.
Co-locating the DB on Render is the right call for this project stage — the API and DB
communicate over Render's internal network (zero egress cost, low latency).

A GitHub Actions cron workflow + shell script automates the rotation:
1. Provisions a new free Render PostgreSQL instance via the Render REST API
2. Dumps data from the expiring DB and restores it to the new one
3. Updates the API service's connection string env var to point to the new DB
4. Verifies the API is healthy, then leaves the old DB intact for manual review

No Terraform is involved in the rotation — Terraform's lifecycle
(`ignore_changes = all`, `prevent_destroy = true`) intentionally locks the DB resource.
The rotation is handled entirely through the Render API + shell script.

---

## Files

| File | Purpose |
|---|---|
| `scripts/rotate-render-db.sh` | Core rotation logic |
| `.github/workflows/db-rotation.yml` | Cron trigger + orchestration |

---

## Prerequisites (one-time setup)

### GitHub repository Secrets
| Secret | Value |
|---|---|
| `RENDER_API_KEY` | Same key used for Terraform Cloud |
| `RENDER_OWNER_ID` | Render owner/team ID |

### GitHub repository Variables (not secrets — visible in logs)
| Variable | Value |
|---|---|
| `RENDER_SERVICE_ID` | Render service ID for `fintrackpro-api` (from Render dashboard URL) |
| `API_HEALTH_URL` | `https://fintrackpro-api.onrender.com/health` |

> **Where to find `RENDER_SERVICE_ID`:** Open the `fintrackpro-api` service in the Render
> dashboard. The URL will be `https://dashboard.render.com/web/srv-XXXXXXXXXX` — the
> `srv-XXXXXXXXXX` part is the service ID.

---

## How the script works (`scripts/rotate-render-db.sh`)

```
1. DISCOVER old DB
   GET /v1/postgres?ownerId=$RENDER_OWNER_ID&limit=20
   → find entry where .postgres.name starts with "fintrackpro-db"
   → extract OLD_DB_ID, OLD_EXTERNAL_URL

2. CREATE new DB
   POST /v1/postgres
   body: { name: "fintrackpro-db-YYYY-MM", plan: "free",
           region: "oregon", version: "18", ownerId: $RENDER_OWNER_ID }
   → extract NEW_DB_ID

3. WAIT for new DB to be ready
   Poll GET /v1/postgres/$NEW_DB_ID every 10s
   → wait until .postgres.status == "available" (timeout: 5 min)
   → extract NEW_INTERNAL_URL, NEW_EXTERNAL_URL

4. DUMP + RESTORE
   pg_dump $OLD_EXTERNAL_URL | pg_restore --no-owner --no-acl -d $NEW_EXTERNAL_URL
   (Full schema + data copy; EF migrations history is included — no dotnet ef needed)

5. UPDATE service env var
   a. GET current env vars for the API service
   b. Replace ConnectionStrings__DefaultConnection → NEW_INTERNAL_URL
   c. PUT full updated env var list back
      (Render API requires the complete list, not a single-var patch)

6. VERIFY API health
   Poll $API_HEALTH_URL every 15s until HTTP 200 (timeout: 3 min)
   → service restarts automatically after env var update

7. PRINT summary
   Old DB ID + Render dashboard link, new DB ID + name, API health status
   Reminder to delete old DB manually after verification
```

### Error handling
- All `curl` calls check HTTP status; non-2xx exits with a clear error message
- If health check fails → both old and new DBs remain intact; revert the env var
  manually via the Render dashboard to restore service immediately
- Script uses `set -euo pipefail`

---

## Cron schedule

The workflow runs at **2 AM UTC on the 1st and 26th of each month** (~25-day cycle),
safely before the 30-day expiry. It can also be triggered manually.

```
0 2 1,26 * *
```

---

## After the workflow succeeds

The old DB is **intentionally kept alive**. Before deleting it:

1. Open the app and verify data looks correct
2. Optionally run a quick query against the new external URL
3. Delete the old DB from the Render dashboard — or run the curl one-liner
   printed at the end of the workflow logs:

```bash
curl -s -X DELETE \
  -H "Authorization: Bearer $RENDER_API_KEY" \
  "https://api.render.com/v1/postgres/<OLD_DB_ID>"
```

---

## Manual trigger

Go to **GitHub Actions → Rotate Render PostgreSQL → Run workflow** to run on demand
(e.g., when the DB is close to expiry and the next scheduled run is too far away).

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Step 1 fails (no DB found) | `fintrackpro-db` name changed in Render | Update the name prefix filter in the script |
| Step 3 times out | Render provisioning slow | Re-run; increase `PROVISION_TIMEOUT` in script |
| Step 4 fails (pg_dump error) | Old DB external URL unreachable | Check Render free DB external access; retry |
| Step 6 fails (health check timeout) | Service cold start on free tier | Increase `HEALTH_TIMEOUT`; check Render logs |
| App shows no data | Restore succeeded but wrong DB pointed to | Check env var in Render dashboard; revert if needed |
