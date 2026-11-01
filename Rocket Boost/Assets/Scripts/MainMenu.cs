using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    void OnGUI()
    {
        float buttonWidth = 320f;
        float centerX = (Screen.width - buttonWidth) / 2f;
        float top = Screen.height * 0.25f;

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(0, top, Screen.width, 70), "ROCKET BOOSTER", titleStyle);

        var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 24 };

        if (GUI.Button(new Rect(centerX, top + 120, buttonWidth, 50), "Play", buttonStyle))
        {
            SceneManager.LoadScene(1);   // first level
        }

#if !UNITY_WEBGL
        // Quitting isn't possible in a browser, so the button only exists in desktop builds
        if (GUI.Button(new Rect(centerX, top + 185, buttonWidth, 50), "Quit", buttonStyle))
        {
            Application.Quit();
        }
#endif

        var noticeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(Screen.width * 0.15f, Screen.height - 110, Screen.width * 0.7f, 80),
            "This game collects anonymous gameplay data (level attempts, crashes, fuel use) " +
            "to improve level design. No personal information is collected.",
            noticeStyle);
    }
}
