# IAM

[`lambda-write-raw-policy.json`](lambda-write-raw-policy.json) is the inline policy added to the ingestion Lambda's execution role, on top of the default CloudWatch Logs permissions. It allows writing objects **only** under `raw/` in the telemetry bucket: no reads, no deletes, no other prefixes (least privilege).

Replace `<YOUR-BUCKET>` with your bucket name.
