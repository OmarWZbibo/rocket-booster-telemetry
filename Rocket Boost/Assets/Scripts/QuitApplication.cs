using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Esc returns to the main menu from any level.
// (Application.Quit does nothing in a browser, so we never rely on it in WebGL.)
public class QuitApplication : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame &&
            SceneManager.GetActiveScene().buildIndex != 0)
        {
            SceneManager.LoadScene(0);
        }
    }
}
