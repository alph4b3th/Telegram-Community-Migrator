public enum OperationState
{
    Running,
    Suspended,
    WaitingForResume,
    Completed,
    Stopped
}
public sealed class OperationController
{

    public OperationState State { get; private set; } =
        OperationState.Running;

    public void Suspend()
        => State = OperationState.Suspended;

    public void WaitForResume()
        => State = OperationState.WaitingForResume;

    public void Resume()
        => State = OperationState.Running;

    public void Complete()
        => State = OperationState.Completed;

    public void Stop()
        => State = OperationState.Stopped;

    public async Task<bool> WaitForResumeAsync(Func<Task<bool>> recoveryCheck)
    {
        while (State == OperationState.Suspended)
        {

            bool canResume = await recoveryCheck();

            if (!canResume)
                await Task.Delay(TimeSpan.FromMinutes(30));

            else Resume();

        }

        return State == OperationState.Running;
    }
}