"""
Rocket Booster - synthetic player simulator.
Sends realistic gameplay events for simulated players through the real API,
so the analytics pipeline and dashboard can be tested at scale.
All simulated players have IDs starting with "sim-".
"""
import json
import random
import time
import uuid
import urllib.error
import urllib.request
from datetime import datetime, timedelta, timezone

import os
API_URL = os.environ.get("ROCKET_BOOSTER_API_URL", "")
GAME_VERSION = "1.0.0"
NUM_PLAYERS = 100
DAYS_BACK = 14   # first plays are spread over the last 14 days
SEED = 42        # same seed = same simulated players every run

# Levels in play order, with difficulty (0 = easy, 1 = brutal)
LEVELS = [("Over", 0.55), ("Under", 0.65), ("Through", 0.45)]
# Base chance an attempt runs the tank dry, per level
FUEL_RISK = {"Over": 0.10, "Under": 0.30, "Through": 0.15}

random.seed(SEED)
sent = 0
failed = 0


def iso(t):
    return t.strftime("%Y-%m-%dT%H:%M:%S.") + f"{t.microsecond // 1000:03d}Z"


def send(event_type, player_id, session_id, level_id, t):
    global sent, failed
    event = {
        "event_id": str(uuid.uuid4()),
        "event_type": event_type,
        "event_version": 1,
        "player_id": player_id,
        "session_id": session_id,
        "level_id": level_id,
        "event_timestamp": iso(t),
        "game_version": GAME_VERSION,
    }
    data = json.dumps(event).encode("utf-8")

    for attempt in range(5):
        request = urllib.request.Request(API_URL, data=data, method="POST",
                                         headers={"Content-Type": "application/json"})
        try:
            with urllib.request.urlopen(request, timeout=10):
                sent += 1
                return
        except urllib.error.HTTPError as e:
            if e.code == 429:  # throttled by API Gateway: wait, then retry
                time.sleep(0.5 * (2 ** attempt))
                continue
            print(f"  {event_type} rejected ({e.code}): {e.read().decode()}")
            break
        except urllib.error.URLError as e:
            print(f"  network error: {e.reason}")
            time.sleep(1)
    failed += 1


def play_session(player_id, skill, start):
    """Simulates one play session. Returns True if the player beat every level."""
    session_id = "session-" + uuid.uuid4().hex
    t = start
    send("session_started", player_id, session_id, "MainMenu", t)
    t += timedelta(seconds=random.uniform(2, 8))

    for level_id, difficulty in LEVELS:
        crashes = 0
        while True:
            send("level_started", player_id, session_id, level_id, t)
            duration = random.uniform(4, 15) + difficulty * 20

            if random.random() < 0.5:
                send("fuel_collected", player_id, session_id, level_id,
                     t + timedelta(seconds=duration * random.uniform(0.3, 0.7)))

            ran_dry = random.random() < FUEL_RISK[level_id] * (1.5 - skill)
            if ran_dry:
                send("fuel_empty", player_id, session_id, level_id,
                     t + timedelta(seconds=duration * 0.8))

            t += timedelta(seconds=duration)

            success_chance = skill * (1 - difficulty) + 0.15
            if ran_dry:
                success_chance *= 0.3

            if random.random() < success_chance:
                send("level_completed", player_id, session_id, level_id, t)
                t += timedelta(seconds=2)
                break

            # Player quits mid-attempt (abandon); more likely after repeated crashes
            if random.random() < 0.05 + 0.08 * crashes:
                return False

            send("player_crashed", player_id, session_id, level_id, t)
            crashes += 1
            t += timedelta(seconds=2)

    return True


def main():
    if not API_URL:
        print('Set ROCKET_BOOSTER_API_URL first.')
        return
    now = datetime.now(timezone.utc)
    clock = time.time()

    for i in range(NUM_PLAYERS):
        player_id = f"sim-{i:04d}"
        skill = random.betavariate(2.5, 2.5)  # most players average, few extremes
        day = now - timedelta(days=random.uniform(1, DAYS_BACK), hours=random.uniform(0, 12))
        sessions = 0

        while day < now and sessions < 6:
            beat_game = play_session(player_id, skill, day)
            sessions += 1

            # Does the player come back another day?
            return_chance = 0.25 + 0.35 * skill + (0.15 if beat_game else 0)
            if random.random() > return_chance:
                break
            day += timedelta(days=random.choice([1, 1, 2, 3, 7]), hours=random.uniform(-3, 3))

        print(f"player {i + 1}/{NUM_PLAYERS} ({sessions} sessions) | sent {sent}, failed {failed}")

    print(f"Done in {time.time() - clock:.0f}s: {sent} events sent, {failed} failed")


if __name__ == "__main__":
    main()