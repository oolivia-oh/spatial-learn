using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UIElements;

public class VideoUI : MonoBehaviour {

    public List<RenderTexture> videoTextures;
    public List<VisualElement> videoContainers;
    public List<VideoPlayer> videoPlayers;
    private int index;

    void Start() {
        index = 0;
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

    public VisualElement LoadClip(string name) {
        VisualElement container = videoContainers[index];
        VideoClip clip = Resources.Load<VideoClip>(name);
        if (clip == null) {
            Debug.LogError($"{name} video not found");
        }
        videoPlayers[index].clip = clip;
        index++;
        if (index >= videoPlayers.Count) index = 0;
        return container;
    }
    
    public void LoadClips(List<string> names) {
        if (names.Count > videoPlayers.Count) {
            Debug.LogError("Too many clips");
            return;
        }
        for (int i = 0; i < names.Count; i++) {
            VideoClip clip = Resources.Load<VideoClip>(names[i]);
            if (clip == null) {
                Debug.LogError("video not found");
            }
            videoPlayers[i].clip = clip;
        }
    }
}
