namespace Authdoc.Models.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public RefreshTokenRevokeReason? RevokeReason { get; set; } // It says why the token stopped working, so a rotated token that comes back can be told apart.

    public bool IsActive(DateTime now) => RevokedAt is null && ExpiresAt > now;
}

public enum RefreshTokenRevokeReason
{
    // Used in a refresh and replaced by a new token. If it comes back, someone has a copy.
    Rotated = 1,
    Logout = 2,
    PasswordChange = 3,
    // Revoked because a rotated token of the same user came back.
    ReuseDetected = 4
}
