using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Chair {
    private float x;
    private float y;
    public static Vector2 s_startPoint;
    public static float s_spacingMultiplier;
    public static float s_width;
    public static float s_height;
    public static string s_primaryKey;
    public Dictionary<string, LearningHistory> histories;
    public Dictionary<string, string> attributes;
    public List<string> idAttributes;
    public Button button;

    public float X {
        get { return x; }
        set {
            x = value;
            button.style.left = x * s_spacingMultiplier + s_startPoint.x;
        }
    }

    public float Y {
        get { return y; }
        set {
            y = value;
            button.style.top = y * s_spacingMultiplier + s_startPoint.y;
        }
    }

    public static void Init(Vector2 startPoint, float spacingMultiplier, float width, float height, string primaryKey) {
        s_startPoint = startPoint;
        s_spacingMultiplier = spacingMultiplier;
        s_width = width;
        s_height = height;
        s_primaryKey = primaryKey;
    }
    
    public Chair(float x_i, float y_i, Dictionary<string, string> attributes_i, VisualElement root) {
        x = x_i;
        y = y_i;
        attributes = attributes_i;
        histories = new Dictionary<string, LearningHistory>();
        foreach (string key in attributes.Keys) {
            histories.Add(key, new LearningHistory(attributes[key]));
        }
        idAttributes = new List<string>();
        SpawnButton(root, s_width, s_height);
    }

    public void SpawnButton(VisualElement root, float width, float height) {
        button = new Button();
        button.style.position = Position.Absolute;
        button.style.left = x * s_spacingMultiplier + s_startPoint.x;
        button.style.top  = y * s_spacingMultiplier + s_startPoint.y;
        button.style.width = width;
        button.style.height = height;
        button.style.backgroundColor = GlobalConfig.colors.background;
        root.Add(button);
    }

    public void ShowAttribute(string key, bool showPrimary=true) {
        if (showPrimary) {
            if (attributes[key] == "t")   button.text = $"{attributes[s_primaryKey]}:\n{key}";
            else if (key != s_primaryKey) button.text = $"{attributes[s_primaryKey]}\n{attributes[key]}";
            else                          button.text = attributes[key];
        } else {
            if (attributes[key] == "t")   button.text = key;
            else                          button.text = attributes[key];
        }
    }

    public void RevealAnswer(string key, bool right) {
        if (right) {
            button.style.backgroundColor = GlobalConfig.colors.right;
            ShowAttribute(key);
        } else {
            button.style.backgroundColor = GlobalConfig.colors.wrong;
        }
    }
}

public class ChairXComparer : IComparer<Chair> {
    public int Compare(Chair cA, Chair cB) {
        if (cA.Y == cB.Y) {
            return cA.X.CompareTo(cB.X);
        } else {
            return cA.Y.CompareTo(cB.Y);
        }
    }
}
