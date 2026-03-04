using System.Text;
using K1Idea.API.GraphQL;
using K1Idea.API.GraphQL.Errors;
using K1Idea.API.GraphQL.Mutations;
using K1Idea.API.GraphQL.Queries;
using K1Idea.API.GraphQL.Subscriptions;
using K1Idea.API.GraphQL.Types;
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
    // Root types (empty shells — slices add the fields via [QueryType]/[MutationType]/[SubscriptionType])
    .AddQueryType()
    .AddType(typeof(TicketQueries))
    .AddType(typeof(CommentQueries))
    .AddType(typeof(OrgQueries))
    .AddMutationType()
    .AddType(typeof(AuthMutations))
    .AddType(typeof(TicketMutations))
    .AddType(typeof(CommentMutations))
    .AddSubscriptionType()
    .AddType(typeof(CommentSubscriptions))
    // Non-root type extensions
    .AddTypeExtension<TicketGqlType>()
    .AddTypeExtension<CommentGqlType>()
    // DataLoaders
    .AddDataLoader<UserByIdDataLoader>()
    .AddDataLoader<BusinessUnitByIdDataLoader>()
    .AddDataLoader<TicketByIdDataLoader>()
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