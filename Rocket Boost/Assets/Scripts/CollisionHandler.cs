using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class CollisionHandler : MonoBehaviour
{
    [SerializeField] float levelLoadDelay = 2f;
    [SerializeField] AudioClip successSFX;
    [SerializeField] AudioClip crashSFX;
    [SerializeField] ParticleSystem successParticles;
    [SerializeField] ParticleSystem crashParticles;
    
    AudioSource audioSource;

    bool isControllable = true;
    bool isColliadable = true;
    // Fuel objects already counted this attempt, so bouncing on one fuel = one event
    HashSet<int> collectedFuel = new HashSet<int>();
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();   
    }
    
    private void  Update()
    {
        RespondToDebugKeys();
    }
    void RespondToDebugKeys()
    {
        if(Keyboard.current.lKey.wasPressedThisFrame)
        {
            LoadNextLevel();
        }
        else if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            isColliadable = !isColliadable; // toggle collision
            Debug.Log("Collision is now: " + isColliadable);
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (!isControllable || !isColliadable){return;}

        switch (other.gameObject.tag)
        {
            case "Friendly":
                Debug.Log("Everything is looking good!");
                break;
            case "Finish":
                TelemetryManager.Instance.Send("level_completed");
                StartSuccessSequence();
                Debug.Log("You have completed the level!");
                break;
            case "Fuel":
                if (collectedFuel.Add(other.gameObject.GetInstanceID()))
                {
                    GetComponent<FuelTank>().Refill();
                    TelemetryManager.Instance.Send("fuel_collected");
                    other.gameObject.SetActive(false);  // pickup disappears
                }
                Debug.Log("You have collected fuel!");
                break;
            default:
                TelemetryManager.Instance.Send("player_crashed");
                StartCrashSequence();
                Debug.Log("You have crashed!");
                break;
        }
    }
    void StartSuccessSequence()
    {
        // todo add sfx and partcle effects
        isControllable = false;
        audioSource.Stop();
        audioSource.PlayOneShot(successSFX);
        successParticles.Play();
        GetComponent<Movement>().enabled = false;
        Invoke("LoadNextLevel", levelLoadDelay);
    }
    void StartCrashSequence()
    {
        // todo add sfx and partcle effects
        isControllable = false;
        audioSource.Stop();
        audioSource.PlayOneShot(crashSFX);
        crashParticles.Play();
        GetComponent<Movement>().enabled = false;
        Invoke("ReloadLevel", levelLoadDelay);
    }
    void ReloadLevel()
    {
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentScene);
    }
    void LoadNextLevel()
    {
        int currentScene = SceneManager.GetActiveScene().buildIndex;
        int nextScene = currentScene + 1;
        if (nextScene == SceneManager.sceneCountInBuildSettings)
        {
            nextScene = 0;
        }
        SceneManager.LoadScene(nextScene);
    }
    
}
