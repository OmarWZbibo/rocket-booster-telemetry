import json
import os
from datetime import datetime, timezone

import boto3

s3 = boto3.client("s3")
BUCKET = os.environ["BUCKET_NAME"]

REQUIRED = ["event_id", "event_type", "event_version", "player_id",
            "session_id", "level_id", "event_timestamp"]
ALLOWED_TYPES = {"player_crashed", "level_completed", "fuel_collected",
                 "session_started", "level_started", "fuel_empty"}


def respond(code, body):
    return {"statusCode": code,
            "headers": {"Content-Type": "application/json"},
            "body": json.dumps(body)}


def lambda_handler(event, context):
    # API Gateway wraps the game's JSON in "body"; console tests send it directly
    if "body" in event:
        try:
            payload = json.loads(event["body"] or "{}")
        except json.JSONDecodeError:
            return respond(400, {"error": "Body is not valid JSON"})
    else:
        payload = event

    missing = [f for f in REQUIRED if f not in payload]
    if missing:
        return respond(400, {"error": f"Missing fields: {missing}"})
    if payload["event_type"] not in ALLOWED_TYPES:
        return respond(400, {"error": f"Unknown event_type: {payload['event_type']}"})

    now = datetime.now(timezone.utc)
    payload["received_at"] = now.isoformat()

    key = (f"raw/events/year={now:%Y}/month={now:%m}/day={now:%d}/"
           f"{payload['event_type']}-{payload['event_id']}.json")

    s3.put_object(Bucket=BUCKET, Key=key,
                  Body=json.dumps(payload), ContentType="application/json")
    return respond(200, {"message": "Event stored", "key": key})
