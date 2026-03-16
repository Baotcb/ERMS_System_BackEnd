using Microsoft.Extensions.Configuration;
using System.IO;

namespace ERMS.AutomationTests.Core
{
    public static class ConfigurationHelper
    {
        private static IConfiguration _configuration;

        public static IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    _configuration = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .Build();
                }
                return _configuration;
            }
        }

        public static string GetClientUrl()
        {
            return Configuration["ClientSettings:Url"];
        }
    }
}