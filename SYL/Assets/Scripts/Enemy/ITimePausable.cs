/// <summary>
/// Implemented by objects that can temporarily pause their local gameplay time.
/// </summary>
public interface ITimePausable
{
    bool IsTimePaused { get; }

    void PauseTime();

    void ResumeTime();
}
