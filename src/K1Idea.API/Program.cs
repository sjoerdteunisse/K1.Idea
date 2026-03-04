using System.Text;
using K1Idea.API.GraphQL.Errors;
using K1Idea.API.Middleware;
using K1Idea.Application;
using K1Idea.Infrastructure;
using K1Idea.Infrastructure.Migrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Infrastructure + Application ─────────────────────────────
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// ── JWT Authentication ────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["JWT:Secret"]
            ?? throw new InvalidOperationException("JWT:Secret is required.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });
builder.Services.AddAuthorization();

// ── Hot Chocolate GraphQL ─────────────────────────────────────
builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
    .AddQueryType()
    .AddMutationType()
    .AddSubscriptionType()
    .AddType<K1Idea.API.GraphQL.Types.UserGqlType>()
    .AddTypeExtension<K1Idea.API.GraphQL.Types.TicketGqlType>()
    .AddTypeExtension<K1Idea.API.GraphQL.Types.CommentGqlType>()
    .AddDataLoader<K1Idea.API.GraphQL.UserByIdDataLoader>()
    .AddDataLoader<K1Idea.API.GraphQL.BusinessUnitByIdDataLoader>()
    .AddDataLoader<K1Idea.API.GraphQL.TicketByIdDataLoader>()
    .AddInMemorySubscriptions()
    .AddErrorFilter<GraphQLErrorFilter>();

var app = builder.Build();

// ── DbUp migrations in Development ───────────────────────────
if (app.Environment.IsDevelopment())
{
    var connStr = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");
    DbUpRunner.Run(connStr);
}

// ── Middleware ────────────────────────────────────────────────
app.UseWebSockets();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<CurrentUserMiddleware>();
app.UseMiddleware<TenantOrgContextMiddleware>();

app.MapGraphQL("/graphql");

app.Run();

public partial class Program;
