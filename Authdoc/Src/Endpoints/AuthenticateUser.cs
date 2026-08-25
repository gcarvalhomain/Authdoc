using Authdoc.Application.Services;
using Authdoc.Responses;


namespace Authdoc.Endpoints;

public static class AuthenticateUser
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("Api/Auth/Post{id}", async (int id, ServiceUser serviceUser) =>
        {

        });
    }
    
}