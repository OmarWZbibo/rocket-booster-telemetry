# API Gateway settings

HTTP API with one route, `POST /events`, integrated with the ingestion Lambda. Stage `$default` with auto-deploy.

**Throttling** (stage default route settings):

```powershell
aws apigatewayv2 update-stage --api-id <API-ID> --stage-name '$default' `
  --default-route-settings ThrottlingRateLimit=10,ThrottlingBurstLimit=20 --region <REGION>
```

Use single quotes around `'$default'` in PowerShell, or it is read as an empty variable.

**CORS** ([`cors.example.json`](cors.example.json)): only the origin itch.io serves embedded games from is allowed. That is `https://html-classic.itch.zone`, not the itch.io page URL.

```powershell
aws apigatewayv2 update-api --api-id <API-ID> --cors-configuration file://cors.example.json --region <REGION>
```

Passing JSON through a file avoids Windows PowerShell 5.1 stripping the inner quotes.

CORS is enforced by browsers only. It stops other websites from using the API through their visitors' browsers, but not scripts; abuse protection comes from throttling, Lambda validation and an AWS Budget alert.
