using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private CharacterController CharacterController;

    private GameMovement GameMovement = new GameMovement();

    public InputAction MoveAction;
    public InputAction LookAction;
    public InputAction FireAction;

    UserCmd OutCommand = new UserCmd();

    void Start() {
        CharacterController = GetComponent<CharacterController>();

        MoveAction.Enable();
        LookAction.Enable();
        FireAction.Enable();
    }

    void Update() {
        if (Cursor.lockState != CursorLockMode.Locked) {
            Cursor.lockState = CursorLockMode.Locked;
        }

        Vector2 InputVector = LookAction.ReadValue<Vector2>();
        // 0.022f is m_yaw value from Source
        OutCommand.MouseDX -= (InputVector.y * 0.022f) * 2.14f;
        OutCommand.MouseDY += (InputVector.x * 0.022f) * 2.14f;

        // Clamp the LookVector
        if (OutCommand.MouseDX < -90)
            OutCommand.MouseDX = -90;
        else if (OutCommand.MouseDX > 90)
            OutCommand.MouseDX = 90;

        if (OutCommand.MouseDY < 0.0f)
            OutCommand.MouseDY = 360.0f;
        else if (OutCommand.MouseDY > 360.0f)
            OutCommand.MouseDY = 0.0f;

        Debug.Log("{" + OutCommand.MouseDX + ", " + OutCommand.MouseDY + "}, " + MoveAction.ReadValue<Vector2>());

        UpdateViewAngles();
    }

    void FixedUpdate() {
        Vector2 MoveVector = MoveAction.ReadValue<Vector2>();

        OutCommand.ViewAngles = Camera.main.transform.rotation;
        OutCommand.Buttons = GetButtons();

        OutCommand.ForwardMove = MoveVector.y; // MoveVector.y is W / S
        OutCommand.SideMove = MoveVector.x; // MoveVector.x is A / D

        GameMovement.ProcessMovement(CharacterController, OutCommand);
    }

    private int GetButtons() {
        int Buttons = 0;

        if (Keyboard.current.spaceKey.isPressed || Input.mouseScrollDelta.x != 0.0f || Input.mouseScrollDelta.y != 0.0f) {
            Buttons |= UserCmd.INPUT_SPACE;
        }

        if (Keyboard.current.leftShiftKey.isPressed) {
            Buttons |= UserCmd.INPUT_WALK;
        }

        if (Keyboard.current.leftCtrlKey.isPressed) {
            Buttons |= UserCmd.INPUT_CTRL;
        }

        return Buttons;
    }

    private void UpdateViewAngles() {
        // note(jax): Rotate the collider THEN the camera
        this.transform.rotation = Quaternion.Euler(0.0f, OutCommand.MouseDY, 0.0f);
        Camera.main.transform.rotation = Quaternion.Euler(OutCommand.MouseDX, OutCommand.MouseDY, 0.0f);
    }

    private void OnGUI() {
        // note(jax): We forward this call to GameMovement because the script doesn't attach to any GameObjects
        GameMovement.OnGUI();
    }

}
