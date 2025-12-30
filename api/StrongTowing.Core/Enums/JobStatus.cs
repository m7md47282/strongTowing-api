namespace StrongTowing.Core.Enums
{
    public enum JobStatus
    {
        Pending,        // Job created, waiting for driver
        Assigned,       // Driver assigned
        OnRoute,        // Driver moving to location
        InProgress,     // Driver arrived, towing started
        ReadyToRelease, // Job done, photos uploaded
        Completed       // Admin verified and closed
    }
}

