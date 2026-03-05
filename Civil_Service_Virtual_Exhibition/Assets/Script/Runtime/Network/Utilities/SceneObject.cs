using Fusion;


public class SceneObject : SimulationBehaviour
{
    private Gameplay _gameplay;
    public Gameplay Gameplay
    {
        get
        {
            if (_gameplay == null && Runner != null && Runner.SceneManager != null && Runner.SceneManager.MainRunnerScene.IsValid())
            {
                var gameplays = Runner.SceneManager.MainRunnerScene.GetComponents<Gameplay>(true);
                if (gameplays.Length > 0)
                {
                    _gameplay = gameplays[0];
                }
            }

            return _gameplay;
        }
    }
}
