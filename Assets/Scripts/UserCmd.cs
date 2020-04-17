using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public struct UserCmd
{
    public Quaternion ViewAngles;

    public float ForwardMove;
    public float SideMove;
    public float UpMove;

    // note(jax): Unused
    //public int Buttons;
    //public short MouseDX;
    //public short MouseDY;
}
