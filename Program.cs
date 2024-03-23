using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using SimpLeX_Backend.Services; // Ensure this using directive is correctly pointing to where your services are defined.

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the IDocumentService with an HttpClient configured to use the base address of the SimpLeX-Compiler service.
builder.Services.AddHttpClient<IDocumentService, DocumentService>(client =>
{
    // Replace "http://SimpLeX-Compiler/" with the actual base URL of your SimpLeX-Compiler service.
    client.BaseAddress = new Uri("http://simplex-compiler-service/");
});

var app = builder.Build();

// Swagger configuration
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SimpLeX API v1"));

//app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

app.Run();