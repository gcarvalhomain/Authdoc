using Scalar.AspNetCore;
using Authdoc.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.UseHttpsRedirection();
app.Run();
