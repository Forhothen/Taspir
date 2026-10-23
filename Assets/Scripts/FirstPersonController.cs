using UnityEngine;
using UnityEngine.InputSystem;

// CharacterController handles collisions; this script adds movement and gravity.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float mouseSensitivity = 0.12f;

    private CharacterController controller;
    private float verticalSpeed;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>().transform;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        // A click is required to capture the mouse in a browser.
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            ReleaseCursor();
            return;
        }
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return; // Ignore the mouse delta from the initial click.
        }

        var movement = Vector3.zero;
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            var look = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            float x = (keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0);
            float z = (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0);
            movement = Vector3.ClampMagnitude(transform.right * x + transform.forward * z, 1f) * moveSpeed;
        }

        // Move does not apply gravity automatically.
        if (controller.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
        verticalSpeed += Physics.gravity.y * Time.deltaTime;
        movement.y = verticalSpeed;
        controller.Move(movement * Time.deltaTime);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) ReleaseCursor();
    }

    private void OnDisable() => ReleaseCursor();

    private static void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnGUI()
    {
        var style = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        string hint = Cursor.lockState == CursorLockMode.Locked
            ? "WASD - walk  |  Mouse - look  |  Esc - release mouse"
            : "Click to explore  |  WASD - walk  |  Mouse - look";
        GUI.Box(new Rect(12, 12, Mathf.Min(580, Screen.width - 24), 42), hint, style);
    }
}
