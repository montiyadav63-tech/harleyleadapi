# Harley Lead API (.NET 8 Web API)

REST wrapper over the existing `HARLEY_OB` table, reusing the already-created
`sp_InsHarleyLead` and `sp_UpdHarleyLead` stored procedures.

## Setup

1. Open `appsettings.json` and set your real DB connection string under
   `ConnectionStrings:DBCS`.
2. Make sure the following stored procedures already exist on your DB
   (scripts below for reference — skip if already created):

```sql
-- sp_InsHarleyLead and sp_UpdHarleyLead should already exist in your
-- OCSHARLEY database from earlier work. If not, recreate them using the
-- versions discussed with Claude in the ASMX project conversation.
```

3. Restore packages and run:

```bash
dotnet restore
dotnet run
```

4. Open the Swagger UI (auto-launches in Development):
   `https://localhost:{port}/swagger`

## Endpoints

- `POST /api/leads` — create a new lead. Required fields: first_name,
  last_name, phone, pincode, city, state, pre_selected_model, lead_source,
  utm_source, source_campaign.
- `PUT /api/leads/{lead_uid}` — partial update of an existing lead by its
  `lead_uid` (GUID). Only send the fields you want to change; omitted fields
  keep their existing values.

## Deployment (no Docker)

1. `dotnet publish -c Release -o ./publish`
2. Copy the `./publish` folder to the IIS server.
3. Install the **ASP.NET Core Hosting Bundle** on the server if not already
   present.
4. Create an IIS site/application pointing at the published folder, with the
   Application Pool's ".NET CLR Version" set to **No Managed Code**.
"# harleyleadapi" 
