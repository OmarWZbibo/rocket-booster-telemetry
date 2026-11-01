using System;
using System.Collections;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Sends gameplay events to AWS (API Gateway -> Lambda -> S3).
// Creates itself automatically when the game starts and survives scene reloads,
// so it does NOT need to be added to any scene.
public class TelemetryManager : MonoBehaviour
{
    public static TelemetryManager Instance { get; private set; }

    const string PlayerIdKey = "telemetry_player_id";

    string endpointUrl;
    string playerId;
    string sessionId;
    bool sessionStarted;

    [Serializable]
    class TelemetryEvent
    {
        public string event_id;
        public string event_type;
        public int event_version;
        public string player_id;
        public string session_id;
        public string level_id;
        public string event_timestamp;
        public string game_version;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("TelemetryManager");
        Instance = go.AddComponent<TelemetryManager>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        // The API URL lives in Assets/Resources/TelemetryConfig.txt, not in the code
        var config = Resources.Load<TextAsset>("TelemetryConfig");
        if (config != null)
        {
            endpointUrl = config.text.Trim();
        }
        else
        {
            Debug.LogWarning("[Telemetry] Resources/TelemetryConfig.txt not found. Events will only be logged to the Console.");
        }

        // Same player ID every time the game is played on this computer
        playerId = PlayerPrefs.GetString(PlayerIdKey, "");
        if (string.IsNullOrEmpty(playerId))
        {
            playerId = "player-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            PlayerPrefs.SetString(PlayerIdKey, playerId);
            PlayerPrefs.Save();
        }

        // New session ID every time the game is launched
        sessionId = "session-" + Guid.NewGuid().ToString("N");

    }

    void Start()
    {
        // The first scene is already loaded by now, so handle it directly,
        // then listen for every scene load after it (next level, reload after crash)
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Stop listening when this object is destroyed (e.g. leaving Play mode)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!sessionStarted)
        {
            sessionStarted = true;
            Send("session_started");
        }

        // The main menu (coming later today) is not a level
        if (scene.name != "MainMenu")
        {
            Send("level_started");
        }
    }

    public void Send(string eventType)
    {
        var e = new TelemetryEvent
        {
            event_id = Guid.NewGuid().ToString(),
            event_type = eventType,
            event_version = 1,
            player_id = playerId,
            session_id = sessionId,
            level_id = SceneManager.GetActiveScene().name,
            event_timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture),
                        game_version = Application.version
        };

        string json = JsonUtility.ToJson(e);
        Debug.Log("[Telemetry] Event: " + json);

        if (!string.IsNullOrEmpty(endpointUrl))
        {
            StartCoroutine(Post(json));
        }
    }

    IEnumerator Post(string json)
    {
        using (var request = new UnityWebRequest(endpointUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[Telemetry] Sent: " + request.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"[Telemetry] Failed ({request.responseCode}): {request.error} {request.downloadHandler.text}");
            }
        }
    }
}
