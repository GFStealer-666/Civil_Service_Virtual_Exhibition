using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class LandingVideoLoader : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;

    private void Start()
    {
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = Path.Combine(Application.streamingAssetsPath, "LandingPageVideo.mp4");
        videoPlayer.Prepare();
    }
}