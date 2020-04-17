using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct UserCmd {
    public static int INPUT_SPACE = (1 << 3);
    public static int INPUT_WALK = (1 << 6);
    public static int INPUT_CTRL = (1 << 9);

    public Quaternion ViewAngles;
    public int Buttons;

    public float ForwardMove;
    public float SideMove;
    public float UpMove;

    // note(jax): Unused
    public float MouseDX;
    public float MouseDY;
}
