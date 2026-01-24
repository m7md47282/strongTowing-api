# Backend Requirements for Users Management

## Overview
This document outlines the backend changes needed to support the Users Management page functionality, including temporary passwords and first-login password changes.

## Required Backend Changes

### 1. UserDto Schema Update
Add a new field to the `UserDto` schema:
- **Field**: `mustChangePassword`
- **Type**: `boolean`
- **Default**: `false`
- **Description**: Indicates if the user must change their password on the next login. Should be automatically set to `true` when an admin creates a user with a temporary password.

### 2. User Creation Endpoint (`POST /users`)
When creating a user via the `/users` endpoint (admin-only):
- Accept the temporary password in the `RegisterRequest`
- Automatically set `mustChangePassword = true` for newly created users
- Return the created user with `mustChangePassword: true` in the response

### 3. Login Response Update
Update the `LoginResponse` to include the `mustChangePassword` flag from the user object:
- The frontend will check `user.mustChangePassword` after login
- If `true`, the user should be redirected to a password change page

### 4. Change Password on First Login Endpoint
**New Endpoint**: `POST /auth/change-password-first-login`

**Request Body**:
```json
{
  "newPassword": "string",
  "confirmPassword": "string"
}
```

**Requirements**:
- Verify that `newPassword` and `confirmPassword` match
- Verify password strength requirements
- Check that the authenticated user has `mustChangePassword = true`
- Update the user's password
- Set `mustChangePassword = false` after successful password change
- Return success response

**Security**:
- Requires Bearer token authentication
- Should only work if `user.mustChangePassword === true`
- Should validate password strength (minimum length, complexity, etc.)

### 5. Update User Endpoint (`PUT /users/{id}`)
The existing endpoint should continue to support updating `isActive` status as currently implemented.

## API Endpoints Summary

### Existing Endpoints (No Changes Required)
- `GET /users` - Get all users (Admin only)
- `PUT /users/{id}` - Update user (can update `isActive` and `fullName`)
- `POST /auth/login` - Login (should return `mustChangePassword` in user object)

### Modified Endpoints
- `POST /users` - Create user (should set `mustChangePassword = true`)

### New Endpoints
- `POST /auth/change-password-first-login` - Change password on first login

## Frontend Implementation Notes

1. **User Creation**: 
   - Frontend generates a random temporary password
   - Displays the temporary password to the admin after successful creation
   - Admin can share this password with the new user

2. **First Login Flow**:
   - After login, check `user.mustChangePassword`
   - If `true`, redirect to password change page
   - User must change password before accessing the dashboard
   - After password change, `mustChangePassword` is set to `false` and user can proceed

3. **Status Toggle**:
   - Uses existing `PUT /users/{id}` endpoint with `isActive` field
   - No backend changes needed

## Testing Checklist

- [ ] Create user with temporary password sets `mustChangePassword = true`
- [ ] Login with temporary password returns `mustChangePassword = true` in response
- [ ] User with `mustChangePassword = true` can change password via new endpoint
- [ ] After password change, `mustChangePassword` is set to `false`
- [ ] User cannot access dashboard until password is changed (if `mustChangePassword = true`)
- [ ] Regular password change endpoint still works for users with `mustChangePassword = false`
- [ ] Status toggle (activate/deactivate) works correctly

## Security Considerations

1. **Temporary Password Strength**: Ensure temporary passwords meet security requirements
2. **Password Change Validation**: Enforce strong password requirements on first login
3. **Token Validation**: Verify user is authenticated and has permission to change password
4. **Rate Limiting**: Consider rate limiting on password change endpoint to prevent brute force
5. **Password History**: Consider preventing reuse of recent passwords


