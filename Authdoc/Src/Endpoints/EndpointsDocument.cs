using System;
using System.Security.Claims;
using Authdoc.Application.DTOs;
using Authdoc.Application.Services;
using Authdoc.Models.Entities;
using Authdoc.Responses;
using Authdoc.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;


namespace Authdoc.Endpoints;

public static class EndpointsDocument
{
    public static void MapDocumentsEndpoints(this WebApplication app)
    {
        app.MapPost("/api/documents", async (CreateDocumentRequest createDocumentRequest) =>
        {

        })
        .RequireAuthorization();
    }
}