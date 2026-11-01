using UnityEngine;

// Holds the rocket's fuel, burns it while thrusting, refills at pickups,
// and draws a simple fuel bar in the top-left corner.
public class FuelTank : MonoBehaviour
{
    [SerializeField] float maxFuel = 100f;
    [SerializeField] float burnRate = 15f;      // fuel used per second of thrust
    [SerializeField] float refillAmount = 50f;  // fuel gained per pickup

    float currentFuel;
    bool emptyReported;

    public bool HasFuel => currentFuel > 0f;
    public float Fraction => currentFuel / maxFuel;

    void Start()
    {
        currentFuel = maxFuel;
    }

    public void Burn(float deltaTime)
    {
        if (!HasFuel) return;

        currentFuel = Mathf.Max(0f, currentFuel - burnRate * deltaTime);

        if (currentFuel == 0f && !emptyReported)
        {
            emptyReported = true;
            TelemetryManager.Instance.Send("fuel_empty");
        }
    }

    public void Refill()
    {
        currentFuel = Mathf.Min(maxFuel, currentFuel + refillAmount);
        emptyReported = false;
    }

    void OnGUI()
    {
        const float x = 20f, y = 20f, width = 220f, height = 22f;

        GUI.Box(new Rect(x, y, width, height), GUIContent.none);

        Color previous = GUI.color;
        GUI.color = Fraction > 0.25f ? Color.green : Color.red;
        GUI.DrawTexture(new Rect(x + 2f, y + 2f, (width - 4f) * Fraction, height - 4f), Texture2D.whiteTexture);
        GUI.color = previous;

        GUI.Label(new Rect(x + 8f, y + 2f, width, height), "FUEL " + Mathf.CeilToInt(currentFuel));
    }
}
