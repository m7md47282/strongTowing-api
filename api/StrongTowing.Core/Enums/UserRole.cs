namespace StrongTowing.Core.Enums;

public enum UserRole
{
    SuperAdmin,     // Can create Admins, Dispatchers, and Drivers
    Administrator,  // Can create Dispatchers and Drivers
    Dispatcher,     // Can manage jobs and assign drivers
    Driver,         // Can view assigned jobs and update status
    User            // Regular user role
}

