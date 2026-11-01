-- One row per player: activity, skill band and retention flags.
-- d1_eligible / d7_eligible handle right-censoring: players who started too recently
-- are excluded from that retention measure instead of counted as "didn't return".
CREATE OR REPLACE VIEW players AS
WITH player_days AS (
    SELECT player_id, player_type, CAST(event_time AS date) AS play_date
    FROM events_clean
    GROUP BY player_id, player_type, CAST(event_time AS date)
),
firsts AS (
    SELECT player_id, player_type, MIN(play_date) AS first_play_date
    FROM player_days
    GROUP BY player_id, player_type
),
stats AS (
    SELECT player_id,
           COUNT(*) AS attempts,
           COUNT_IF(outcome = 'completed') AS completions,
           COUNT(DISTINCT session_id) AS sessions_played,
           MAX(CASE WHEN level_id = 'Through' AND outcome = 'completed' THEN 1 ELSE 0 END) AS beat_game
    FROM level_attempts
    GROUP BY player_id
)
SELECT f.player_id,
       f.player_type,
       f.first_play_date,
       COUNT(DISTINCT d.play_date) AS active_days,
       COALESCE(s.attempts, 0)        AS attempts,
       COALESCE(s.completions, 0)     AS completions,
       COALESCE(s.sessions_played, 0) AS sessions_played,
       COALESCE(s.beat_game, 0)       AS beat_game,
       CASE WHEN COALESCE(s.attempts, 0) = 0 THEN 'Unknown'
            WHEN s.completions * 1.0 / s.attempts >= 0.50 THEN 'High'
            WHEN s.completions * 1.0 / s.attempts >= 0.25 THEN 'Medium'
            ELSE 'Low' END AS skill_band,
       CASE WHEN COALESCE(s.attempts, 0) = 0 THEN 4
            WHEN s.completions * 1.0 / s.attempts >= 0.50 THEN 3
            WHEN s.completions * 1.0 / s.attempts >= 0.25 THEN 2
            ELSE 1 END AS skill_band_order,
       MAX(CASE WHEN date_diff('day', f.first_play_date, d.play_date) = 1 THEN 1 ELSE 0 END) AS returned_d1,
       MAX(CASE WHEN date_diff('day', f.first_play_date, d.play_date) BETWEEN 1 AND 7 THEN 1 ELSE 0 END) AS returned_7d,
       CASE WHEN date_diff('day', f.first_play_date, current_date) >= 1 THEN 1 ELSE 0 END AS d1_eligible,
       CASE WHEN date_diff('day', f.first_play_date, current_date) >= 7 THEN 1 ELSE 0 END AS d7_eligible
FROM firsts f
JOIN player_days d ON d.player_id = f.player_id
LEFT JOIN stats s  ON s.player_id = f.player_id
GROUP BY f.player_id, f.player_type, f.first_play_date,
         s.attempts, s.completions, s.sessions_played, s.beat_game;
