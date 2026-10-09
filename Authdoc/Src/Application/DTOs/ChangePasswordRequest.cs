namespace Authdoc.Application.DTOs;

public class ChangePasswordRequest
{
    public required string CurrentPassword { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmationPassword { get; set; }
}
