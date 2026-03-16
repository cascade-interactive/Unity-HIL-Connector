using UnityEngine;

public class VelocityTracker : MonoBehaviour
{
    private Vector3 lastPosition;
    private Vector3 currentVelocity;

    void Start()
    {
        // Record the starting position
        lastPosition = transform.position;
    }

    void Update()
    {
        // Calculate velocity: (Current Position - Old Position) / Time since last frame
        currentVelocity = (transform.position - lastPosition) / Time.deltaTime;

        // Update the old position for the next frame's math
        lastPosition = transform.position;
    }

    // This draws a UI element directly on the game screen so it doesn't spam your console
    void OnGUI()
    {
        // Create a visual box in the top-left corner
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 24;
        style.alignment = TextAnchor.MiddleLeft;
        style.normal.textColor = Color.green;

        // Display the updating velocity
        string text = $"Velocity: {currentVelocity.magnitude:F2} m/s\n" +
                      $"X: {currentVelocity.x:F2}\n" +
                      $"Y: {currentVelocity.y:F2}\n" +
                      $"Z: {currentVelocity.z:F2}";

        GUI.Box(new Rect(10, 10, 300, 120), text, style);
    }
}