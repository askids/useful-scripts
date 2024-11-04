public class Startup : BaseStartup
{
    public Startup(IConfiguration configuration) : base(configuration) { }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        // Add additional API-specific services here
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        WebApiHost.RunWithStartup<Startup>(args);
    }
}
