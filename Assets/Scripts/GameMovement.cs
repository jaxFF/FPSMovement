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
    public static float UnitConversionScale = 0.03f;

    public float Gravity = 800.0f * UnitConversionScale; // Source World gravity
    public float Accelerate = 5.5f; // CSGO Acceleration speed
    public float AirAccelerate = 12.0f; // CSGO Acceleration speed
    public float Friction = 5.2f; // CSGO World friction

    public float StopSpeed = 80.0f * UnitConversionScale; // CSGO Stop speed
    public float JumpHeight = 21.0f;
    public float JumpImpulse = 21.0f * UnitConversionScale;//301.993377f * UnitConversionScale;

    public float MaxVelocity = 3500.0f * UnitConversionScale;
    public float MaxSpeed = 400.0f * UnitConversionScale; // 320.0f
}

public class GameMovementState {
    public CharacterController CharacterController;

    public Vector3 VelocityVector;
    public Quaternion ViewAngles;
    public int OldButtons;

    public bool OnGround;
    public float SurfaceFriction = 1.0f;

    public Vector3 OutWishVelocity; // Where the player tried to move
    public Vector3 OutJumpVelocity; // Velocity of the players jump
};

public class GameMovement : MonoBehaviour {
    private GameMovementSettings Settings = new GameMovementSettings();
    private GameMovementState State = new GameMovementState();
    private UserCmd Command;

    private Vector3 ForwardVector; 
    private Vector3 RightVector; 
    private Vector3 UpVector;

    // note(jax): We shouldn't be using this, but it's Debug only code for now.
    public GUIStyle GUIStyle = new GUIStyle();

    void OnEnable() {
        GUIStyle.fontSize = 14;
    }

    public void ProcessMovement(CharacterController Controller, UserCmd CommandIn) {
        State.CharacterController = Controller;
        Command = CommandIn;

        PlayerMove();

        Debug.Log("ViewAngles: " + Command.ViewAngles + ", Forward: " + Command.ForwardMove + ", Side: " + Command.SideMove);
    }

    public void PlayerMove() {
        State.OutWishVelocity = Vector3.zero;
        State.OutJumpVelocity = Vector3.zero;

        this.ForwardVector = this.Command.ViewAngles * Vector3.forward;
        this.RightVector = this.Command.ViewAngles * Vector3.right;
        this.UpVector = this.Command.ViewAngles * Vector3.up;

        FullWalkMove();
        FinishPlayerMove();
    }

    public bool CheckJumpButton() {
        // todo (jax): Consider CSGO Stamina recovery
        // https://github.com/click4dylan/CSGO_GameMovement_Reversed/blob/master/IGameMovement.cpp#L2427
        // https://github.com/click4dylan/CSGO_GameMovement_Reversed/blob/master/IGameMovement.cpp#L2055
        if (!State.CharacterController.isGrounded) {
            State.OldButtons |= UserCmd.INPUT_SPACE;
            return false;
        }

        if ((State.OldButtons &= UserCmd.INPUT_SPACE) == UserCmd.INPUT_SPACE) {
            return false;
        }

        // In the air now
        State.OnGround = false;

        if (State.OnGround) {
            State.SurfaceFriction = 1.0f;
        }

        float GroundFactor = 1.0f;
        float JumpMultiplier = Mathf.Sqrt(2.0f * Settings.Gravity * Settings.JumpImpulse);

        float InitalY = State.VelocityVector.y;
        State.VelocityVector.y += (GroundFactor * JumpMultiplier);

        FinishGravity();

        State.OutJumpVelocity.y += State.VelocityVector.y - InitalY;
        State.OldButtons |= UserCmd.INPUT_SPACE;
        return true;
    }

    public void FullWalkMove() {
        // note(jax): Friction actually isn't being calculated because
        // Friction acts on State.OutWishVelocity, not State.VelocityVector

        // todo(jax): Handle UserCmd.INPUT_SPACE, UserCmd.INPUT_WALK, UserCmd.INPUT_CTRL
        StartGravity();

        // note(jax): Calculate Jump
        if ((Command.Buttons & UserCmd.INPUT_SPACE) == UserCmd.INPUT_SPACE) {
            CheckJumpButton();
        } else {
            State.OldButtons &= ~UserCmd.INPUT_SPACE;
        }

        if (State.OnGround) {
            State.VelocityVector.y = 0.0f;
            Friction();
        }

        CheckVelocity();

        if (State.OnGround)
            WalkMove();
        else
            AirMove();

        CategorizePosition();

        CheckVelocity();

        FinishGravity();

        Vector3 Temp = GetOrigin();
        Temp += State.VelocityVector * Time.fixedDeltaTime;
        if (State.OnGround) {
            float Gravity = 1.0f;

            Vector3 NewOrigin = ScaleOriginReverse(Temp - GetOrigin());
            NewOrigin.y -= ((Gravity * this.Settings.Gravity * 10.0f * Time.fixedDeltaTime) * Time.fixedDeltaTime) * GameMovementSettings.UnitConversionScale;
            State.CharacterController.Move(NewOrigin);
        } else {
            State.CharacterController.Move(ScaleOriginReverse(Temp - GetOrigin()));
        }
    }

    public void CategorizePosition() {
        State.OnGround = State.CharacterController.isGrounded;
        if (State.OnGround)
            State.SurfaceFriction = 1.0f;
    }

    public void FinishPlayerMove() {
        State.OldButtons = Command.Buttons;
    }

    public void WalkMove() {
        float ForwardMove = Command.ForwardMove;
        float SideMove = Command.SideMove;

        ForwardVector.y = 0.0f;
        RightVector.y = 0.0f;
        ForwardVector.Normalize();
        RightVector.Normalize();

        Vector3 WishVelocity = Vector3.zero;
        WishVelocity.x = (ForwardVector.x * ForwardMove) + (RightVector.x * SideMove);
        WishVelocity.z = (ForwardVector.z * ForwardMove) + (RightVector.z * SideMove);

        Vector3 WishDirection = WishVelocity;
        float WishSpeed = WishDirection.magnitude;
        WishDirection.Normalize();

        // Clamp the WishSpeed
        if (WishSpeed != 0.0f && WishSpeed > Settings.MaxSpeed) {
            for (int i = 0; i < 3; ++i)
                WishVelocity[i] *= (Settings.MaxSpeed / WishSpeed);

            WishSpeed = Settings.MaxSpeed;
        }

        this.State.VelocityVector.y = 0.0f;
        this.Accelerate(WishDirection, WishSpeed, Settings.Accelerate);
        this.State.VelocityVector.y = 0.0f;

        if (State.VelocityVector.sqrMagnitude > (Settings.MaxSpeed * Settings.MaxSpeed)) {
            float Scale = Settings.MaxSpeed / State.VelocityVector.magnitude;
            State.VelocityVector *= Scale;
        }

        State.OutWishVelocity += WishDirection * WishSpeed;
    }

    public void AirMove() {
        float ForwardMove = Command.ForwardMove;
        float SideMove = Command.SideMove;

        ForwardVector.y = 0.0f;
        RightVector.y = 0.0f;
        ForwardVector.Normalize();
        RightVector.Normalize();

        Vector3 WishVelocity = Vector3.zero;
        WishVelocity.x = (ForwardVector.x * ForwardMove) + (RightVector.x * SideMove);
        WishVelocity.z = (ForwardVector.z * ForwardMove) + (RightVector.z * SideMove);

        Vector3 WishDirection = WishVelocity;
        float WishSpeed = WishDirection.magnitude;
        WishDirection.Normalize();

        // Clamp the WishSpeed
        if (WishSpeed != 0.0f && WishSpeed > Settings.MaxSpeed) {
            for (int i = 0; i < 3; ++i)
                WishVelocity[i] *= Settings.MaxSpeed / WishSpeed;

            WishSpeed = Settings.MaxSpeed;
        }

        AirAccelerate(WishDirection, WishSpeed, Settings.AirAccelerate);
    }

    public void Friction() {
        float Speed = State.VelocityVector.magnitude;

        if (Speed >= 0.1f) {
            float Drop = 0.0f;
            if (State.OnGround) {
                float Friction = Settings.Friction * State.SurfaceFriction;

                // Bleed off some speed, but if we have less than the bleed
                //  threshold, bleed the threshold amount.
                float Control = (Speed < Settings.StopSpeed) ? Settings.StopSpeed : Speed;

                Drop += (Control * Friction) * Time.fixedDeltaTime;
            }

            float NewSpeed = Mathf.Max(Speed - Drop, 0.0f);
            if (NewSpeed < 0)
                NewSpeed = 0;

            if (NewSpeed != Speed) {
                // Determine proportion of old speed we are using.
                NewSpeed /= Speed;

                for (int i = 0; i < 3; ++i)
                    State.VelocityVector[i] *= NewSpeed;
            }
        }
    }

    public void Accelerate(Vector3 WishDirection, float WishSpeed, float Accelerate) {
        float AddSpeed = WishSpeed - Vector3.Dot(State.VelocityVector, WishDirection);

        if (AddSpeed > 0.0f) {
            float AccelSpeed = Accelerate * Time.fixedDeltaTime * WishSpeed;

            if (AccelSpeed > AddSpeed)
                AccelSpeed = AddSpeed;

            for (int i = 0; i < 3; ++i)
                State.VelocityVector[i] += AccelSpeed * WishDirection[i];
        }
    }

    public void AirAccelerate(Vector3 WishDirection, float WishSpeed, float Accelerate) {
        float AddSpeed = WishSpeed - Vector3.Dot(State.VelocityVector, WishDirection);

        if (AddSpeed > 0.0f) {
            float AccelSpeed = Accelerate * Time.fixedDeltaTime * WishSpeed;

            if (AccelSpeed > AddSpeed)
                AccelSpeed = AddSpeed;

            for (int i = 0; i < 3; ++i)
                State.VelocityVector[i] += AccelSpeed * WishDirection[i];
        }
    }

    public void CheckVelocity() {
        for (int i = 0; i < 3; ++i) {
            if (float.IsNaN(State.VelocityVector[i])) {
                Debug.Log("Got a NaN velocity " + State.VelocityVector[i]);
                State.VelocityVector[i] = 0;
            }

            if (State.VelocityVector[i] > Settings.MaxVelocity) {
                State.VelocityVector[i] = Settings.MaxVelocity;
            } else if (State.VelocityVector[i] < -Settings.MaxVelocity) {
                State.VelocityVector[i] = -Settings.MaxVelocity;
            }
        }
    }

    protected Vector3 GetOrigin() {
        return ScaleOriginReverse(State.CharacterController.center);
    }

    // source engine to unity conversion
    protected Vector3 ScaleOrigin(Vector3 origin) {
        return origin * GameMovementSettings.UnitConversionScale;
    }

    // unity to source engine conversion
    protected Vector3 ScaleOriginReverse(Vector3 origin) {
        return origin / GameMovementSettings.UnitConversionScale;
    }

    public void StartGravity() {
        float Gravity = 1.0f;
        State.VelocityVector.y -= ((Settings.Gravity * Gravity) * 0.5f * Time.fixedDeltaTime);// * GameMovementSettings.UnitConversionScale;

        CheckVelocity();
    }

    public void FinishGravity() {
        float Gravity = 1.0f;
        State.VelocityVector.y -= ((Settings.Gravity * Gravity) * 0.5f * Time.fixedDeltaTime);// * GameMovementSettings.UnitConversionScale;

        CheckVelocity();
    }

    // not the right way to handle gui, but its for debug purposes atm ¯\_(ツ)_/¯
    public void OnGUI() {
        var ups = State.CharacterController.velocity;
        ups.y = 0;
        GUI.Label(new Rect(15, 15, 400, 100), "velocity (source units): " + System.Math.Round((ups.magnitude / GameMovementSettings.UnitConversionScale), 0), GUIStyle);
        GUI.Label(new Rect(15, 30, 400, 100), "onGround: " + State.OnGround, GUIStyle);
        GUI.Label(new Rect(15, 45, 400, 100), "fmove: " + Command.ForwardMove + ", smove: " + Command.SideMove, GUIStyle);
    }
}
