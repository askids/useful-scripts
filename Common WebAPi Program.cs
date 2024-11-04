public static class WebApiHost
{
    public static void RunWithStartup<TStartup>(string[] args) where TStartup : class
    {
        bool isRestartRequested;
        bool limitedMode = false;
        RestartReason restartReason = RestartReason.Normal;

        do
        {
            isRestartRequested = false;
            limitedMode = false;

            var host = Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<TStartup>();
                })
                .Build();

            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Application failed to start: {ex.Message}");
                limitedMode = true;
            }

            // Set limited mode if there was a startup failure
            if (restartReason == RestartReason.Failure)
            {
                limitedMode = true;
            }
        }
        while (isRestartRequested);
    }
}
