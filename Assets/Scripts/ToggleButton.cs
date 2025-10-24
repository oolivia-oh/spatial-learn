using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ToggleButton : Button {
    private bool on;
    public bool On {
        get { return on; }
        set {
            on = value;
            if (on) style.backgroundColor = GlobalConfig.colors.right;
            else    style.backgroundColor = GlobalConfig.colors.wrong;
        }
    }
    public ToggleButton(bool on=false) : base() {
        clicked += Toggle;
        On = on;
    }

    public void Toggle() {
        if (On) On = false;
        else    On = true;
    }
}