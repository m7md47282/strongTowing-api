namespace StrongTowing.Core.Enums
{
    public enum JobStatus
    {
        Waiting,   // Waiting for driver / dispatch
        Dispatch,  // Driver assigned, dispatched
        OnRoute,   // En route to scene
        OnScene,   // On scene / work in progress
        Loaded,    // Vehicle loaded, ready to complete
        Completed, // Closed
        Cancelled
    }
}
