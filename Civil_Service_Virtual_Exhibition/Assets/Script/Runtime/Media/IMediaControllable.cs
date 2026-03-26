public interface IMediaControllable
{
    MediaPlaybackState State { get; }

    void StopMedia();
    void CancelMediaLoading();
}