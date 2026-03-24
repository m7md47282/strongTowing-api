namespace StrongTowing.API.Options;

public class FirebaseOptions
{
    public const string SectionName = "Firebase";

    /// <summary>
    /// Path to the Firebase service account JSON (download from Firebase Console → Project settings → Service accounts).
    /// Required for sending pushes; token registration works without it.
    /// </summary>
    public string? ServiceAccountKeyPath { get; set; }
}
