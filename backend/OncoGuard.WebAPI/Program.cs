using Microsoft.EntityFrameworkCore;
using OncoGuard.Application.Interfaces.Auth;
using OncoGuard.Application.Interfaces.ClinicalReports;
using OncoGuard.Application.Interfaces.DailyLogs;
using OncoGuard.Application.Interfaces.Features;
using OncoGuard.Application.Interfaces.FoodLogs;
using OncoGuard.Application.Interfaces.Labs;
using OncoGuard.Application.Interfaces.RiskExplanations;
using OncoGuard.Application.Interfaces.Rules;
using OncoGuard.Infrastructure.Persistence;
using OncoGuard.Infrastructure.Services.Auth;
using OncoGuard.Infrastructure.Services.ClinicalReports;
using OncoGuard.Infrastructure.Services.DailyLogs;
using OncoGuard.Infrastructure.Services.Features;
using OncoGuard.Infrastructure.Services.FoodLogs;
using OncoGuard.Infrastructure.Services.Labs;
using OncoGuard.Infrastructure.Services.RiskExplanations;
using OncoGuard.Infrastructure.Services.Rules;

using OncoGuard.Application.Interfaces.RiskPredictions;
using OncoGuard.Infrastructure.Services.RiskPredictions;

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAuthService, AuthService>();

// JWT token uretici servis
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddScoped<ILabService, LabService>();

builder.Services.AddScoped<IDailyLogService, DailyLogService>();

builder.Services.AddScoped<IFoodLogService, FoodLogService>();

builder.Services.AddScoped<IFeatureEngineeringService, FeatureEngineeringService>();

builder.Services.AddScoped<IRuleEngineService, RuleEngineService>();

builder.Services.AddScoped<IRiskExplanationService, RiskExplanationService>();

builder.Services.AddScoped<IClinicalReportService, ClinicalReportService>();

builder.Services.AddHttpClient<IRiskPredictionClient, RiskPredictionClient>(client =>
{
    var baseUrl = builder.Configuration["AiService:BaseUrl"];

    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new Exception("AiService:BaseUrl is not configured.");

    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddScoped<IPatientRiskEvaluationService, PatientRiskEvaluationService>();

// CORS: arayuz (dashboard/Android) baglanabilsin diye
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var securityScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT token girin. Ornek: Bearer {token}",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { securityScheme, new string[] { } }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// GLOBAL ERROR HANDLING: yakalanmamis hatalari duzgun JSON olarak don (500 cokmesi yerine)
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var message = feature?.Error.Message ?? "An unexpected error occurred.";

        await context.Response.WriteAsJsonAsync(new
        {
            status = "error",
            message = message
        });
    });
});

//app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();