using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Config/Color Palette")]
public class ColorPalette : ScriptableObject
{
    [Header("Answer Colors")]
    public Color right = Color.green;
    public Color wrong = Color.red;
    public Color highlighted = Color.yellow;
    public Color background = Color.grey;
}
