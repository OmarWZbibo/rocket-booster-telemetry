-- One row per level attempt.
-- A window function numbers attempts within each session: every level_started begins a new one.
CREATE OR REPLACE VIEW level_attempts AS
WITH numbered AS (
    SELECT *,
           SUM(CASE WHEN event_type = 'level_started' THEN 1 ELSE 0 END)
               OVER (PARTITION BY session_id ORDER BY event_time
                     ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS attempt_no
    FROM events_clean
    WHERE level_id <> 'MainMenu'
)
SELECT session_id,
       player_id,
       player_type,
       game_version,
       level_id,
       attempt_no,
       MIN(event_time) AS attempt_start,
       MAX(event_time) AS attempt_end,
       date_diff('millisecond', MIN(event_time), MAX(event_time)) / 1000.0 AS duration_seconds,
       CASE WHEN COUNT_IF(event_type = 'level_completed') > 0 THEN 'completed'
            WHEN COUNT_IF(event_type = 'player_crashed')  > 0 THEN 'crashed'
            ELSE 'abandoned' END AS outcome,
       COUNT_IF(event_type = 'fuel_empty') > 0 AS ran_out_of_fuel,
       COUNT_IF(event_type = 'fuel_collected') AS fuel_pickups
FROM numbered
WHERE attempt_no > 0
GROUP BY session_id, player_id, player_type, game_version, level_id, attempt_no;
