public abstract class BaseStartup
{
    protected readonly IConfiguration Configuration;

    protected BaseStartup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public virtual void ConfigureServices(IServiceCollection services)
    {
        // Common service configurations, such as authentication and logging
        services.AddControllers();
        services.AddAuthentication();
        services.AddAuthorization();

        // Register limited mode monitor service
        services.AddHostedService<LimitedModeMonitorService>();
    }

    public virtual void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/reinit", async context =>
            {
                // Limited mode reinit endpoint logic
            });
            endpoints.MapGet("/restart", async context =>
            {
                // Restart endpoint logic
            });
        });
    }
}
