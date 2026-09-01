using Authdoc.Application.Services;
using Authdoc.Data;

namespace Authdoc.Endpoints;

public static class EndpointsAuth
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapGet("Api/Auth/{id}", async (int id, UserService userService) =>
        {
            var user = await userService.GetByIdAsync(id);
            if (user == null)
            {
                
            }
        });
    }
}