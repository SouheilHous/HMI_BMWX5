using UnityEngine;

public class VehicleController : MonoBehaviour
{
    [Header("Wheel Settings")]
    public Transform[] wheels; // Assign wheel transforms in Inspector
    public float wheelSpeed = 50f; // Speed of wheel rotation
    private bool isWheelRotating = false;

    [Header("Steering Settings")]
    public Transform[] steeringWheels; // Assign front wheels for steering
    public float steeringAngle = 30f; // Max angle for steering
    public float steeringSpeed = 2f; // Speed of lerping for steering

    [Header("Door Settings")]
    public Transform[] frontDoors; // Assign left and right front door transforms
    public Transform[] backDoors; // Assign left and right back door transforms
    public float doorOpenAngle = 70f; // Angle to open the doors
    public float doorLerpSpeed = 2f; // Speed of lerping for doors

    [Header("Back Door Settings")]
    public Transform backDoor; // Assign back door transform
    public float backDoorOpenAngle = 90f; // Angle to open the back door
    public float backDoorLerpSpeed = 2f;

    private bool areFrontDoorsOpen = false;
    private bool areBackDoorsOpen = false;
    private bool isBackDoorOpen = false;

    private float currentSteerAngle = 0f;


    [Header("Material Settings")]
    public Material carMaterial; // Assign the car material in the inspector
    public GameObject colorPickerPanel; // Drag your color picker panel GameObject here
    public CUIColorPicker colorPicker;

    private bool isColorPickerVisible = false; // Tracks color picker visibility

    private void Start()
    {
        // Initialize the color picker
        if (carMaterial != null && carMaterial.HasProperty("baseColorFactor"))
        {
            Color initialColor = carMaterial.GetColor("baseColorFactor");
            colorPicker.Color = initialColor;
        }

        if (colorPicker != null)
        {
            colorPicker.SetOnValueChangeCallback(OnColorPickerValueChanged);
        }

        // Ensure the panel is hidden initially
        if (colorPickerPanel != null)
        {
            colorPickerPanel.SetActive(false);
        }
    }


    private void Update()
    {
        // Rotate wheels if active
        if (isWheelRotating)
        {
            foreach (var wheel in wheels)
            {
                wheel.Rotate(Vector3.forward * wheelSpeed * Time.deltaTime, Space.Self);
            }
        }
    }

    // Method 1: Toggle wheel rotation
    public void ToggleWheelRotation()
    {
        isWheelRotating = !isWheelRotating;
    }

    // Method 2: Steer wheels
    public void SteerWheels(string direction)
    {
        float targetAngle = 0f;

        if (direction == "left")
            targetAngle = steeringAngle;
        else if (direction == "right")
            targetAngle = -steeringAngle;
        else if (direction == "center")
            targetAngle = 0f;

        StopAllCoroutines();
        StartCoroutine(LerpSteer(targetAngle));
    }

    private System.Collections.IEnumerator LerpSteer(float targetAngle)
    {
        while (Mathf.Abs(currentSteerAngle - targetAngle) > 0.01f)
        {
            currentSteerAngle = Mathf.Lerp(currentSteerAngle, targetAngle, Time.deltaTime * steeringSpeed);

            for (int i = 0; i < steeringWheels.Length; i++)
            {
                // Reverse the angle for back wheels (assuming back wheels are in the second half of the array)
                float adjustedAngle = i < steeringWheels.Length / 2 ? currentSteerAngle : -currentSteerAngle/2;
                steeringWheels[i].localRotation = Quaternion.Euler(0, adjustedAngle, 0);
            }

            yield return null;
        }

        // Ensure final angle is applied
        for (int i = 0; i < steeringWheels.Length; i++)
        {
            float adjustedAngle = i < steeringWheels.Length / 2 ? targetAngle : -targetAngle/2;
            steeringWheels[i].localRotation = Quaternion.Euler(0, adjustedAngle, 0);
        }
    }


    // Method 3: Toggle front doors
    public void ToggleFrontDoors()
    {
        areFrontDoorsOpen = !areFrontDoorsOpen;
        StopAllCoroutines();
        StartCoroutine(LerpDoors(frontDoors, areFrontDoorsOpen ? doorOpenAngle : 0));
    }

    // Method 4: Toggle back doors
    public void ToggleBackDoors()
    {
        areBackDoorsOpen = !areBackDoorsOpen;
        StopAllCoroutines();
        StartCoroutine(LerpDoors(backDoors, areBackDoorsOpen ? doorOpenAngle : 0));
    }

    // Method 5: Open/close back door
    public void ToggleBackDoor()
    {
        isBackDoorOpen = !isBackDoorOpen;
        StopAllCoroutines();
        StartCoroutine(LerpBackDoor(backDoor, isBackDoorOpen ? backDoorOpenAngle : 0));
    }

    private System.Collections.IEnumerator LerpDoors(Transform[] doors, float targetAngle)
    {
        float elapsedTime = 0;
        float duration = 1f / doorLerpSpeed;

        // Calculate the start and target angles for both doors
        float[] startAngles = new float[doors.Length];
        float[] targetAngles = new float[doors.Length];

        for (int i = 0; i < doors.Length; i++)
        {
            startAngles[i] = doors[i].localEulerAngles.y;
            targetAngles[i] = i == 0 ? targetAngle : -targetAngle; // Reverse angle for the second door
        }

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            for (int i = 0; i < doors.Length; i++)
            {
                float currentAngle = Mathf.LerpAngle(startAngles[i], targetAngles[i], t);
                doors[i].localRotation = Quaternion.Euler(0, currentAngle, 0);
            }

            yield return null;
        }

        // Ensure final rotation is applied
        for (int i = 0; i < doors.Length; i++)
        {
            doors[i].localRotation = Quaternion.Euler(0, targetAngles[i], 0);
        }
    }

    private System.Collections.IEnumerator LerpDoor(Transform door, float targetAngle)
    {
        float startAngle = door.localEulerAngles.y;
        float elapsedTime = 0;

        while (elapsedTime < 1f)
        {
            float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, elapsedTime * backDoorLerpSpeed);
            door.localRotation = Quaternion.Euler(0, currentAngle, 0);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        door.localRotation = Quaternion.Euler(0, targetAngle, 0);
    }

    private System.Collections.IEnumerator LerpBackDoor(Transform door, float targetAngle)
    {
        float startAngle = door.localEulerAngles.z;
        float elapsedTime = 0;

        while (elapsedTime < 1f)
        {
            float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, elapsedTime * backDoorLerpSpeed);
            door.localRotation = Quaternion.Euler(0, 0,currentAngle);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        door.localRotation = Quaternion.Euler(0, 0, targetAngle);
    }

    public void ToggleColorPicker()
    {
        isColorPickerVisible = !isColorPickerVisible;

        if (colorPickerPanel != null)
        {
            colorPickerPanel.SetActive(isColorPickerVisible);
        }
    }

    private void OnColorPickerValueChanged(Color newColor)
    {
        if (carMaterial != null && carMaterial.HasProperty("baseColorFactor"))
        {
            carMaterial.SetColor("baseColorFactor", newColor);
        }
    }

    private void OnGUI()
    {
        // Calculate margins and button size
        float margin = Screen.width * 0.05f; // 5% margin from the left
        float buttonWidth = Screen.width * 0.12f; // 20% of the screen width
        float buttonHeight = Screen.height * 0.05f; // 5% of the screen height
        float spacing = buttonHeight * 0.2f; // 20% of button height as spacing

        // Adaptive font size
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.02f); // Font size is 2% of screen height
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        // Starting position for the first button
        float x = margin;
        float y = margin;

        // Generate GUI buttons for testing
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Toggle Wheel Rotation", buttonStyle))
            ToggleWheelRotation();

        y += buttonHeight + spacing;
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Steer Left", buttonStyle))
            SteerWheels("left");

        y += buttonHeight + spacing;
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Steer Right", buttonStyle))
            SteerWheels("right");

        y += buttonHeight + spacing;
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Steer Center", buttonStyle))
            SteerWheels("center");

        y += buttonHeight + spacing;
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Toggle Front Doors", buttonStyle))
            ToggleFrontDoors();

        y += buttonHeight + spacing;
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Toggle Back Doors", buttonStyle))
            ToggleBackDoors();

        y += buttonHeight + spacing;
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Toggle Back Door", buttonStyle))
            ToggleBackDoor();

        y += buttonHeight + spacing;
        if (GUI.Button(new Rect(x, y, buttonWidth, buttonHeight), "Toggle Color Picker", buttonStyle))
            ToggleColorPicker();
    }

}
