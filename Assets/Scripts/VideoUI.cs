using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UIElements;

public class VideoUI : MonoBehaviour {

    public List<RenderTexture> videoTextures;
    public List<VisualElement> videoContainers;
    public List<VideoPlayer> videoPlayers;

    void Start() {
        if (videoTextures.Count != videoPlayers.Count) {
            Debug.LogError("Textures and players count mismatch");
            return;
        }
        videoContainers = new List<VisualElement>();
        for (int i = 0; i < videoPlayers.Count; i++) {
            videoContainers.Add(new VisualElement());
            videoContainers[i].style.width = 100;
            videoContainers[i].style.height = 100;
            videoContainers[i].style.backgroundImage = new StyleBackground(Background.FromRenderTexture(videoTextures[i]));
        }
    }
    
    public void LoadClips(List<string> clipNames) {
        if (clipNames.Count > videoPlayers.Count) {
            Debug.LogError("Too many clips");
            return;
        }
        for (int i = 0; i < clipNames.Count; i++) {
            VideoClip clip = Resources.Load<VideoClip>(clipNames[i]);
            if (clip == null) {
                Debug.LogError("video not found");
            }
            videoPlayers[i].clip = clip;
        }
    }
}
