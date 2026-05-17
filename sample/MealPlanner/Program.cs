using System.Text;
using API.Data;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using API.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().WithOrigins("http://localhost:4200", "https://localhost:4200"));
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Request path: {Path}, Method: {Method}", 
        context.Request.Path, context.Request.Method);
    
    if (context.Request.Path.ToString().Contains("google-auth"))
    {
        logger.LogInformation("Google auth request detected");
        
        // Enable buffering to allow reading request body multiple times
        context.Request.EnableBuffering();
        
        // Read the request body
        using (var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true))
        {
            var body = await reader.ReadToEndAsync();
            logger.LogInformation("Google auth request body: {Body}", body);
            
            // Reset the request body position
            context.Request.Body.Position = 0;
        }
    }
    
    await next();
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

try
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DataContext>();
    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
    var unitOfServices = services.GetRequiredService<IUnitOfServices>();
    await context.Database.MigrateAsync();
    await Seed.SeedRoles(roleManager);
    await Seed.SeedDietTypes(unitOfServices);
    await Seed.SeedServingTypes(unitOfServices);
    await Seed.SeedAllergies(unitOfServices);
    await Seed.SeedIngredientCategories(unitOfServices);
    await Seed.SeedIngredients(unitOfServices);
    await Seed.SeedCookware(unitOfServices);
    await Seed.SeedRecipes(unitOfServices);
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred during seeding");
}

app.Run();
