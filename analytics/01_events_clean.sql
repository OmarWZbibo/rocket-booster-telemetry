-- Base layer: one row per event, tidy types and labels.
-- event_time is a plain UTC timestamp (BI tools can't use timestamp with time zone).
CREATE OR REPLACE VIEW events_clean AS
SELECT event_id,
       event_type,
       player_id,
       session_id,
       level_id,
       COALESCE(game_version, 'pre-1.0') AS game_version,
       CAST(from_iso8601_timestamp(event_timestamp) AS timestamp) AS event_time,
       CASE WHEN player_id LIKE 'sim-%' THEN 'simulated' ELSE 'real' END AS player_type,
       year, month, day
FROM events
WHERE level_id <> 'level-01';
