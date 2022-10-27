using Microsoft.Extensions.Configuration;

namespace Common;

public class AppSettingReader
{
    public T ReadSection<T>(string sectionName)
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables();
        var configurationRoot = builder.Build();

        return configurationRoot.GetSection(sectionName).Get<T>();
    }
}