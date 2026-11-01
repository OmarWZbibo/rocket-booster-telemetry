-- One row per play session.
CREATE OR REPLACE VIEW sessions AS
SELECT session_id,
       player_id,
       player_type,
       game_version,
       MIN(event_time) AS session_start,
       MAX(event_time) AS session_end,
       date_diff('second', MIN(event_time), MAX(event_time)) AS duration_seconds,
       COUNT_IF(event_type = 'level_started')   AS attempts,
       COUNT_IF(event_type = 'level_completed') AS levels_completed
FROM events_clean
GROUP BY session_id, player_id, player_type, game_version;
