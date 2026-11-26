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

    public async Task<bool> WaitForResumeAsync()
    {
        while (State == OperationState.Suspended)
            await Task.Delay(60*1000);

        return State == OperationState.Running;
    }
}