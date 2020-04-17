using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
    note(jax): This is very basic FPS Movement code!

     - Jumping
     - Crouching
     - Surfing (sliding of steep surfaces)
     - Research HL2 movement glitches? Could be fun to implement / understand!
     - Longjumping
     - Circle-strafe
     - Proper ground velocity clipping

    Just a small list of things todo
*/

[SerializeField]
public class GameMovementSettings {
    public float Gravity = 800.0f; // Source World gravity
    public float Accelerate = 5.5f; // CSGO Acceleration speed
    public float Friction = 5.2f; // CSGO World friction

    public float StopSpeed = 80.0f; // CSGO Stop speed

    public float MaxVelocity = 3500.0f;
    public float MaxSpeed = 400.0f; // 320.0f
}

public class GameMovementState {
    public CharacterController CharacterController;

    public Vector3 VelocityVector;
    public Quaternion ViewAngles;

    public bool OnGround;
    public float SurfaceFriction = 1.0f;

    public Vector3 OutWishVelocity; // Where the player tried to move
    public Vector3 OutJumpVelocity; // Velocity of the players jump
};

public class GameMovement : MonoBehaviour {
    private GameMovementSettings Settings = new GameMovementSettings();
    private GameMovementState State;
    private UserCmd Command;

    private Vector3 ForwardVector; 
    private Vector3 RightVector; 
    private Vector3 UpVector;

    // note(jax): We shouldn't be using this, but it's Debug only code for now.
    public GUIStyle GUIStyle = new GUIStyle();

    void OnEnable() {
        GUIStyle.fontSize = 14;
    }

    public void ProcessMovement(GameMovementState State, UserCmd Command) {
        this.State = State;
        this.Command = Command;

        this.PlayerMove();

        Debug.Log("ViewAngles: " + Command.ViewAngles + ", Forward: " + Command.ForwardMove + ", Side: " + Command.SideMove);
    }

    public void PlayerMove() {
        this.State.OutWishVelocity = Vector3.zero;
        this.State.OutJumpVelocity = Vector3.zero;

        this.ForwardVector = Command.ViewAngles * Vector3.forward;
        this.RightVector = Command.ViewAngles * Vector3.right;
        this.UpVector = Command.ViewAngles * Vector3.up;

        // todo(jax): Make this based on State.CharacterController.isGrounded;
        // That way we can interface with Unitys collision system! x)
        this.State.OnGround = true; 

        this.FullWalkMove();
    }

    public void FullWalkMove() {
        this.StartGravity();
        this.CheckVelocity();

        if (this.State.OnGround) {
            this.State.VelocityVector.y = 0.0f;
            this.Friction();
        }

        if (this.State.OnGround) {
            this.WalkMove();
        } else {
            // AirMove()
        }

        this.CheckVelocity();
        this.FinishGravity();

        if (this.State.OnGround) {
            this.State.VelocityVector.y = 0.0f;
        }
    }

    public void WalkMove() {
        float ForwardMove = this.Command.ForwardMove;
        float RightMove = this.Command.SideMove;

        ForwardVector.y = 0.0f;
        RightVector.y = 0.0f;
        ForwardVector.Normalize();
        RightVector.Normalize();

        Vector3 WishVelocity = Vector3.zero;
        WishVelocity.x = (ForwardVector.x * ForwardMove) + (RightVector.x * RightMove);
        WishVelocity.z = (ForwardVector.z * ForwardMove) + (RightVector.z * RightMove);

        Vector3 WishDirection = WishVelocity;
        WishDirection.Normalize();
        float WishSpeed = WishDirection.magnitude;

        // Clamp the WishSpeed
        if (WishSpeed != 0.0f && WishSpeed > this.Settings.MaxSpeed) {
            for (int i = 0; i < 3; ++i) {
                WishVelocity[i] *= this.Settings.MaxSpeed / WishSpeed;
            }

            WishSpeed = this.Settings.MaxSpeed;
        }

        this.State.VelocityVector.y = 0.0f;
        this.Accelerate(WishDirection, WishSpeed, this.Settings.Accelerate);
        this.State.VelocityVector.y = 0.0f;

        this.State.CharacterController.Move(this.State.VelocityVector);
    }

    public void Friction() {
        float NewSpeed, Friction, Control;

        float Speed = this.State.VelocityVector.magnitude;
        if (Speed < 0.1f) {
            return;
        }

        float Drop = 0;
        if (this.State.OnGround) {
            Friction = this.Settings.Friction * this.State.SurfaceFriction;

            // Bleed off some speed, but if we have less than the bleed
            //  threshold, bleed the threshold amount.
            Control = (Speed < this.Settings.StopSpeed) ? this.Settings.StopSpeed : Speed;

            Drop += Control * Friction * Time.fixedDeltaTime;
        }

        NewSpeed = Speed - Drop;
        if (NewSpeed < 0)
            NewSpeed = 0;

        if (NewSpeed != Speed) {
            // Determine proportion of old speed we are using.
            NewSpeed /= Speed;

            for (int i = 0; i < 3; ++i) {
                this.State.VelocityVector[i] *= NewSpeed;
            }
        }

        this.State.OutWishVelocity -= (1.0f - NewSpeed) * this.State.VelocityVector;
    }

    public void Accelerate(Vector3 WishDirection, float WishSpeed, float Accelerate) {
        // See if we are changing direction a bit
        float CurrentSpeed = Vector3.Dot(this.State.VelocityVector, WishDirection);

        // Reduce wishspeed by the amount of veer.
        float AddSpeed = WishSpeed - CurrentSpeed;
        if (AddSpeed <= 0)
            return;

        float AccelSpeed = Accelerate * Time.fixedDeltaTime * WishSpeed * this.State.SurfaceFriction;
        if (AccelSpeed > AddSpeed) {
            AccelSpeed = AddSpeed;
        }

        for (int i = 0; i < 3; ++i) {
            this.State.VelocityVector[i] += AccelSpeed * WishDirection[i];
        }
    }

    public void CheckVelocity() {
        for (int i = 0; i < 3; ++i) {
            if (float.IsNaN(this.State.VelocityVector[i])) {
                Debug.Log("Got a NaN velocity " + this.State.VelocityVector[i]);
                this.State.VelocityVector[i] = 0;
            }

            if (this.State.VelocityVector[i] > this.Settings.MaxVelocity) {
                this.State.VelocityVector[i] = this.Settings.MaxVelocity;
            } else if (this.State.VelocityVector[i] < -this.Settings.MaxVelocity) {
                this.State.VelocityVector[i] = -this.Settings.MaxVelocity;
            }
        }
    }

    public void StartGravity() {
        float Gravity = 1.0f;
        this.State.VelocityVector.y -= ((this.Settings.Gravity * Gravity) * 0.5f * Time.fixedDeltaTime);

        CheckVelocity();
    }

    public void FinishGravity() {
        float Gravity = 1.0f;
        this.State.VelocityVector.y -= ((this.Settings.Gravity * Gravity) * 0.5f * Time.fixedDeltaTime);

        CheckVelocity();
    }

    // not the right way to handle gui, but its for debug purposes atm ¯\_(ツ)_/¯
    public void OnGUI() {
        if (Command.SideMove == 1)
            GUI.Label(new Rect(Screen.width / 2 + 20, Screen.height / 2 + 40, 400, 100), "D", GUIStyle);
        if (Command.SideMove == -1)
            GUI.Label(new Rect(Screen.width / 2 - 20, Screen.height / 2 + 40, 400, 100), "A", GUIStyle);
        
        if (Command.ForwardMove == 1)
            GUI.Label(new Rect(Screen.width / 2, Screen.height / 2 + 20, 400, 100), "W", GUIStyle);

        if (Command.ForwardMove == -1)
            GUI.Label(new Rect(Screen.width / 2, Screen.height / 2 + 40, 400, 100), "S", GUIStyle);
    }
}
