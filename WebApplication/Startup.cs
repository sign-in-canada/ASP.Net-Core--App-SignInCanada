using IdentityModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApplication.Middlewares;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace WebApplication
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization(options =>
            {
                options.ResourcesPath = "Resources";
            });
            
            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.SetDefaultCulture("en-CA");
                options.AddSupportedUICultures("en-CA", "fr-CA", "fr-FR");
                options.FallBackToParentUICultures = true;

                options
                    .RequestCultureProviders
                    .Remove(typeof(AcceptLanguageHeaderRequestCultureProvider));
            });
            
            services
                .AddRazorPages()
                .AddViewLocalization();
            
            services.AddScoped<RequestLocalizationCookiesMiddleware>();


            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "Cookies";
                options.DefaultChallengeScheme = "oidc";
            })
               .AddCookie(options =>
               {
                   options.Cookie.Name = "mvccode";
               })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.Authority = "https://xxxxxxxxxx.canada.ca";
                    options.RequireHttpsMetadata = false;

                    options.ClientId = "xxx-yyyyy-aaaaaa-bbbbbb";
                    options.ClientSecret = "clientsecret";

                    // code flow + PKCE (PKCE is turned on by default)
                    options.ResponseType = "code";
                    options.UsePkce = true;

                    options.Scope.Clear();
                    options.Scope.Add("openid");

                    // not mapped by default
                    options.ClaimActions.MapJsonKey("website", "website");

                    // UI locales passing
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = context => { 
                            // provide the ui_locale to Sign In Canada  
                            context.ProtocolMessage.UiLocales = CultureInfo.CurrentUICulture.Name; // en-CA, fr-CA, ... etc
                            return Task.CompletedTask;
                        },
                        OnUserInformationReceived = context =>
                        {
                            // After signing in, update the ui culture if different (locale attribute from UserInfo endpoint)
                            var culture = context.User.RootElement.GetString("locale");
                            if (culture != null && CultureInfo.CurrentUICulture.Name != culture)
                            {
                                context.HttpContext.Response.Cookies.Append(
                                    CookieRequestCultureProvider.DefaultCookieName,
                                    CookieRequestCultureProvider.MakeCookieValue(
                                        new RequestCulture(culture, culture)),
                                        new Microsoft.AspNetCore.Http.CookieOptions
                                        { Expires = System.DateTimeOffset.UtcNow.AddYears(1) }
                                );
                            }
                            return Task.CompletedTask;
                        }
                    };

                    // keeps id_token smaller
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.SaveTokens = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = JwtClaimTypes.Name,
                        RoleClaimType = JwtClaimTypes.Role,
                    };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRequestLocalization();

            // will remember to write the cookie 
            app.UseRequestLocalizationCookies();
            
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });
        }
    }
}