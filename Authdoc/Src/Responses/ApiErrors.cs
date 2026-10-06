namespace Authdoc.Responses;

public static class ApiErrors
{
    // Validation (400)
    public static readonly ErrorResponse NameRequired = new() { Code = "NAME_REQUIRED", Message = "Name is required" };
    public static readonly ErrorResponse GenderRequired = new() { Code = "GENDER_REQUIRED", Message = "Gender is required" };
    public static readonly ErrorResponse AgeTooLow = new() { Code = "AGE_TOO_LOW", Message = "Age must be 18 years of age or older" };
    public static readonly ErrorResponse EmailRequired = new() { Code = "EMAIL_REQUIRED", Message = "Email is required" };
    public static readonly ErrorResponse EmailInvalid = new() { Code = "EMAIL_INVALID", Message = "Email is invalid or its domain is not allowed" };
    public static readonly ErrorResponse PasswordRequired = new() { Code = "PASSWORD_REQUIRED", Message = "Password is required" };
    public static readonly ErrorResponse PasswordTooShort = new() { Code = "PASSWORD_TOO_SHORT", Message = "Password must be at least 6 characters long" };
    public static readonly ErrorResponse PasswordsDoNotMatch = new() { Code = "PASSWORDS_DO_NOT_MATCH", Message = "Passwords do not match" };
    public static readonly ErrorResponse RoleInvalid = new() { Code = "ROLE_INVALID", Message = "Role must be Admin or User" };
    public static readonly ErrorResponse CannotChangeOwnRole = new() { Code = "CANNOT_CHANGE_OWN_ROLE", Message = "You cannot change your own role" };
    public static readonly ErrorResponse CannotDeleteOwnUser = new() { Code = "CANNOT_DELETE_OWN_USER", Message = "You cannot delete your own user" };

    // Authentication (401)
    public static readonly ErrorResponse InvalidCredentials = new() { Code = "INVALID_CREDENTIALS", Message = "Email or password is incorrect" };

    // Not found (404)
    public static readonly ErrorResponse UserNotFound = new() { Code = "USER_NOT_FOUND", Message = "User not found" };

    // Conflict (409)
    public static readonly ErrorResponse EmailAlreadyInUse = new() { Code = "EMAIL_ALREADY_IN_USE", Message = "Email is already in use" };
    public static readonly ErrorResponse LastAdmin = new() { Code = "LAST_ADMIN", Message = "Cannot remove the last admin" };
}
