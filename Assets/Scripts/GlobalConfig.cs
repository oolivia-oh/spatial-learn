using UnityEngine;

public static class GlobalConfig {
    public static ColorPalette colors;
    
    public static void Init(ColorPalette i_colors) {
        colors = i_colors;
    }
}