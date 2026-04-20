using UnityEngine;
using UnityEngine.InputSystem;

namespace AOI360.Runtime.Core
{
    public class EditorLookTest : MonoBehaviour
    {
        [SerializeField] private float mouseSensitivity = 0.15f;

        private float pitch = 0f;
        private float yaw = 0f;

        private void Start()
        {
            Vector3 initialRotation = transform.eulerAngles;
            pitch = initialRotation.x;
            yaw = initialRotation.y;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (Mouse.current == null)
            {
                return;
            }

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}