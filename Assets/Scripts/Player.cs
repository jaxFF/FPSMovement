using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private CharacterController CharacterController;

    private GameMovement GameMovement = new GameMovement();
    private GameMovementState GameMovementState = new GameMovementState();

    public InputAction MoveAction;
    public InputAction LookAction;
    public InputAction FireAction;

    void Start() {
        this.CharacterController = GetComponent<CharacterController>();
        this.GameMovementState.CharacterController = CharacterController;

        MoveAction.Enable();
        LookAction.Enable();
        FireAction.Enable();
    }

    void Update() {
        if (Cursor.lockState != CursorLockMode.Locked) {
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        // note(jax): Camera code should be calculated in Update()
        // Things go wrong in FixedUpdate() (slower camera sensitivity??)

        Vector2 InputVector = LookAction.ReadValue<Vector2>();
        Vector2 LookVector = new Vector3();
        // 0.022f is m_yaw value from Source
        LookVector.x += -InputVector.y * 0.022f * 2;
        LookVector.y += InputVector.x * 0.022f * 2;

        // Clamp the LookVector X
        if (LookVector.x < -90)
            LookVector.x = -90;
        else if (LookVector.x > 90)
            LookVector.x = 90;

        this.UpdateViewAngles(LookVector);
    }

    void FixedUpdate() {
        Vector2 MoveVector = MoveAction.ReadValue<Vector2>();

        UserCmd Command = new UserCmd();
        Command.ViewAngles = Camera.main.transform.rotation;

        Command.ForwardMove = MoveVector.y; // MoveVector.y is W / S
        Command.SideMove = MoveVector.x; // MoveVector.x is A / D

        GameMovement.ProcessMovement(GameMovementState, Command);
    }

    private void UpdateViewAngles(Vector2 ViewAngles) {
        // note(jax): Rotate the collider THEN the camera
        this.transform.rotation *= Quaternion.Euler(0.0f, ViewAngles.y, 0.0f);
        Camera.main.transform.rotation *= Quaternion.Euler(ViewAngles.x, 0.0f, 0.0f);
    }

    private void OnGUI() {
        // note(jax): We forward this call to GameMovement because the script doesn't attach to any GameObjects
        GameMovement.OnGUI();
    }

}
