using Authdoc.Application.Services;
using Authdoc.Data;
using Scalar.AspNetCore;
using Authdoc.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<UserService>();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddDbContext<ManagerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")) );

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapAuthEndpoints();
    app.MapUserEndpoints();
    app.UseHttpsRedirection();
    app.Run();
}
