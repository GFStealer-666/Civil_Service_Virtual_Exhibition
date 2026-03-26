using System;

public interface IAE_ProjectVideoPlaybackController
{
    event Action Prepared;
    event Action<string> Failed;
    event Action<bool> PlayStateChanged;
    event Action<double, double> TimeChanged;
    event Action Finished;

    bool IsPrepared { get; }
    bool IsPreparing { get; }
    bool IsPlaying { get; }
    double CurrentTime { get; }
    double Duration { get; }

    void Prepare(string url);
    void Play();
    void Pause();
    void CancelPrepare();
    void StopPlayback();
    void SeekNormalized(float normalizedValue);
}