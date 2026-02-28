using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RecipeSugesstionApp.Data;
using RecipeSugesstionApp.DTOs;
using RecipeSugesstionApp.Middleware;
using RecipeSugesstionApp.Repositories;
using RecipeSugesstionApp.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<RecipeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── JWT Authentication ─────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Return JSON error instead of default challenge HTML
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode  = 401;
                ctx.Response.ContentType = "application/json";
                var body = JsonSerializer.Serialize(
                    new ErrorResponse { Message = "Authentication token is missing or invalid." },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                await ctx.Response.WriteAsync(body);
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode  = 403;
                ctx.Response.ContentType = "application/json";
                var body = JsonSerializer.Serialize(
                    new ErrorResponse { Message = "You do not have permission to perform this action." },
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                await ctx.Response.WriteAsync(body);
            }
        };
    });

builder.Services.AddAuthorization();

// ── Services ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,       AuthService>();
builder.Services.AddScoped<IRecipeService,     RecipeService>();
builder.Services.AddScoped<IRecipeRepository,  RecipeRepository>();

// ── Controllers / MVC ─────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // Use camelCase for all JSON responses
        opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Suppress the default automatic 400 model-validation response so controllers
// can return our custom ErrorResponse with structured error fields.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// ── OpenAPI / Swagger ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Recipe Sharing Platform API",
        Version     = "v1",
        Description = """
            Backend API for the Recipe Sharing Platform.
            
            **Features:**
            - JWT-authenticated CRUD for recipes
            - Full-text search by keyword, ingredient name, and category  
            - Pagination and sort control (newest · oldest · top-rated · most-rated)
            - Star ratings with score distribution  
            - Paginated comments with edit and delete
            """,
        Contact = new OpenApiContact
        {
            Name = "Recipe App Team"
        }
    });

    // JWT Bearer auth in Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name        = "Authorization",
        Type        = SecuritySchemeType.Http,
        Scheme      = "Bearer",
        BearerFormat = "JWT",
        In          = ParameterLocation.Header,
        Description = "Enter your JWT token (without the 'Bearer ' prefix)."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ───────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ── Build ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Global Error Handler (first in pipeline) ───────────────────────────────
app.UseMiddleware<ExceptionMiddleware>();

// ── Swagger (dev only) ─────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Recipe API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();   // Serve uploaded images at /uploads/recipes/*
app.UseCors("AngularDev");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Auto-apply migrations on startup ──────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RecipeDbContext>();
    db.Database.Migrate();
}

app.Run();
