using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class FirstPersonCameraController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Base movement speed in units per second.")]
    public float moveSpeed = 10f;

    [Tooltip("Vertical movement speed when using ascend/descend keys (Space/Shift).")]
    public float verticalSpeed = 10f;

    [Header("Mouse Look")]
    [Tooltip("Multiplier applied to mouse delta when rotating the camera.")]
    public float mouseSensitivity = 2.0f;

    [Tooltip("Clamp for looking up and down. Prevents flipping when exceeding the limit.")]
    public float verticalClamp = 85f;

    [Tooltip("Lock and hide the cursor while the script is active.")]
    public bool lockCursor = true;

    private float _yaw;
    private float _pitch;
    private Mouse _mouse;
    private Keyboard _keyboard;

    private void Start()
    {
        Vector3 angles = transform.localEulerAngles;
        _yaw = angles.y;
        _pitch = angles.x;

        _mouse = Mouse.current;
        _keyboard = Keyboard.current;

        if (lockCursor)
        {
            SetCursorState(true);
        }
    }

    private void Update()
    {
        HandleCursorToggle();
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleCursorToggle()
    {
        if (_keyboard == null)
            return;

        if (_keyboard.escapeKey.wasPressedThisFrame)
        {
            SetCursorState(false);
        }

        if (_mouse != null && _mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorState(true);
        }
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked || _mouse == null)
        {
            return;
        }

        Vector2 mouseDelta = _mouse.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * 0.1f;
        float mouseY = mouseDelta.y * mouseSensitivity * 0.1f;

        _yaw += mouseX;
        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, -verticalClamp, verticalClamp);

        transform.localRotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void HandleMovement()
    {
        if (_keyboard == null)
            return;

        float forward = 0f;
        if (_keyboard.wKey.isPressed) forward += 1f;
        if (_keyboard.sKey.isPressed) forward -= 1f;

        float right = 0f;
        if (_keyboard.dKey.isPressed) right += 1f;
        if (_keyboard.aKey.isPressed) right -= 1f;

        Vector3 planarInput = new Vector3(right, 0f, forward);
        planarInput = planarInput.sqrMagnitude > 1f ? planarInput.normalized : planarInput;

        Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
        Vector3 planarDirection = yawRotation * planarInput;
        Vector3 velocity = planarDirection * moveSpeed;

        float verticalDirection = 0f;
        if (_keyboard.spaceKey.isPressed)
        {
            verticalDirection += 1f;
        }
        if (_keyboard.leftShiftKey.isPressed || _keyboard.rightShiftKey.isPressed)
        {
            verticalDirection -= 1f;
        }

        Vector3 verticalVelocity = Vector3.up * verticalDirection * verticalSpeed;

        transform.position += (velocity + verticalVelocity) * Time.deltaTime;
    }

    private void SetCursorState(bool locked)
    {
        lockCursor = locked;
        Cursor.visible = !locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
