-- Athena partition projection for the Glue table `events`.
-- New year=/month=/day= folders are found automatically; no crawler or MSCK REPAIR needed.
-- Replace <YOUR-BUCKET> with your bucket name.
ALTER TABLE events SET TBLPROPERTIES (
  'projection.enabled'          = 'true',
  'projection.year.type'        = 'integer',
  'projection.year.range'       = '2026,2027',
  'projection.month.type'       = 'integer',
  'projection.month.range'      = '1,12',
  'projection.month.digits'     = '2',
  'projection.day.type'         = 'integer',
  'projection.day.range'        = '1,31',
  'projection.day.digits'       = '2',
  'storage.location.template'   = 's3://<YOUR-BUCKET>/raw/events/year=${year}/month=${month}/day=${day}'
);
