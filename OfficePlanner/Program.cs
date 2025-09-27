using Htmx;
using Htmx.TagHelpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using OfficePlanner.Database;
using System.Security.Claims;

namespace OfficePlanner;

public class Program
{
    private const string IsExternalRedirect = "IsExternalRedirect";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var config = new OPConfig(builder.Configuration);

        builder.Services.AddSingleton(config);
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
            .AddCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                if (builder.Environment.IsProduction())
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                }
                else
                {
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                }
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddOpenIdConnect(options =>
            {
                options.ResponseType = "code";
                options.Authority = config.OidcAuthority;
                options.ClientId = config.OidcClientId;
                options.ClientSecret = config.OidcClientSecret;
                options.UsePkce = true;
                if (builder.Environment.IsDevelopment())
                {
                    options.RequireHttpsMetadata = false;
                }
                options.RefreshInterval = TimeSpan.FromMinutes(5);
                options.AutomaticRefreshInterval = TimeSpan.FromMinutes(5);
                options.UseTokenLifetime = false;
                options.SaveTokens = false;
                options.Events.OnRedirectToIdentityProvider = (context) =>
                {
                    var loc = context.Response.Headers.Location;
                    context.Response.Headers.Append(IsExternalRedirect, "1");
                    return Task.CompletedTask;
                };
                options.Events.OnTokenValidated = async (context) =>
                {
                    var db = context.HttpContext.RequestServices.GetRequiredService<OfficePlannerDatabase>();
                    var sub = context.SecurityToken.Subject;
                    var name = context.SecurityToken.Claims.FirstOrDefault(e => e.Type == "preferred_username") ?? throw new Exception();
                    var user = await db.UpsertUser(context.Scheme.Name, sub, name.Value, context.HttpContext.RequestAborted);
                    var identity = new ClaimsIdentity(
                    [
                        new Claim(Util.USERID_KEY, user.Id.ToString()),
                    ]);
                    context.Principal!.AddIdentity(identity);
                };
            });
        builder.Services.AddControllers();
        builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
        builder.Services.AddRazorPages();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddDbContext<OPDbContext>(options => options.UseNpgsql(config.ConnectionString));
        builder.Services.AddScoped<OfficePlannerDatabase>();
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });
        builder.Services.AddAntiforgery(options =>
        {
            options.SuppressXFrameOptionsHeader = true; // CSP frame-ancestors is defined, so we suppress X-Frame-Options
            if (builder.Environment.IsProduction())
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            }
            else
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            }
        });

        var app = builder.Build();
        if (app.Environment.IsProduction())
        {
            app.UseForwardedHeaders();
            app.UseHttpsRedirection();
        }
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("Content-Security-Policy", new StringValues(
                "default-src 'none'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self'; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; form-action 'self'; upgrade-insecure-requests; base-uri 'none'"));
            context.Response.Headers.Append("Referrer-Policy", new StringValues("no-referrer"));
            context.Response.Headers.Append("X-Content-Type-Options", new StringValues("nosniff"));
            context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", new StringValues("none"));
            await next(context);
        });
        app.UseStaticFiles();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.Use(async (context, next) =>
        {
            context.Response.Headers["Vary"] = "HX-Request";
            await next(context);
            if (context.Response.Headers.ContainsKey(IsExternalRedirect) && context.Request.IsHtmx())
            {
                context.Response.Headers.Remove(IsExternalRedirect);
                context.Response.Headers["HX-Redirect"] = context.Response.Headers.Location;
                context.Response.Headers.Remove("Location");
            }
        });
        app.UseRouting();
        app.UseAuthorization();
        app.MapHtmxAntiforgeryScript();
        app.UseAntiforgery();
        app.MapControllers();
        app.MapRazorPages();

        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<OPDbContext>();
            context.Database.Migrate();
        }

        app.Run();
    }
}
