using Authdoc.Responses;
using Authdoc.Application.Services;


namespace Authdoc.Endpoints;

public static class AuthenticateUser
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("Api/Auth/Get{id}", async (int id, UserService userService) =>
        {
            var user = await userService.GetByIdAsync(id);
            if (user is null)
            {
                return Results.NotFound(new ErrorResponse
                {
                    Message = "User not found",
                });
            }
            return Results.Ok(user);
        });
    }
    
}