using Baubit.Configuration;
using Baubit.Test.DI.Setup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Baubit.Test.DI.ServiceProviderFactoryRegistrar
{
    public class Test
    {
        private const string UserSecretsId = "0657aef1-6dc5-48f1-8ae4-172674291be0";
        private static readonly string SecretsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) ?? 
                                                                  Environment.GetEnvironmentVariable("HOME") ?? 
                                                                  Environment.GetEnvironmentVariable("USERPROFILE") ?? 
                                                                  throw new InvalidOperationException("Could not determine user home directory"),
                                                                  ".microsoft", "usersecrets", UserSecretsId, "secrets.json");
        static Test()
        {
            EnsureUserSecretsExist();
        }

        private static void EnsureUserSecretsExist()
        {
            try
            {
                // Ensure the directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(SecretsPath)!);

                // If the file does not exist, create it
                if (!File.Exists(SecretsPath))
                {
                    var jsonContent = @"{
                  ""someSecretString"": ""shhhh.. 🤫""
                }";

                    File.WriteAllText(SecretsPath, jsonContent);
                    Console.WriteLine("User secrets JSON created at: " + SecretsPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create user secrets: " + ex.Message);
            }
        }
        [Theory]
        [InlineData("config.json")]
        public void CanLoadModulesFromJson(string fileName)
        {
            var hostAppBuilder = new HostApplicationBuilder();
            var configurationSource = new ConfigurationSource { EmbeddedJsonResources = [$"{this.GetType().Assembly.GetName().Name};DI.ServiceProviderFactoryRegistrar.{fileName}"] };
            hostAppBuilder.Configuration.AddConfiguration(configurationSource.Load());
            new Baubit.DI.ServiceProviderFactoryRegistrar().UseDefaultServiceProviderFactory(hostAppBuilder);

            var host = hostAppBuilder.Build();

            var component = host.Services.GetRequiredService<Component>();

            Assert.NotNull(component);
            Assert.False(string.IsNullOrEmpty(component.SomeString));
        }

        [Theory]
        [InlineData("configWithSecrets.json")]
        public void CanLoadModulesWithSecretsFromJson(string fileName)
        {
            var hostAppBuilder = new HostApplicationBuilder();
            var configurationSource = new ConfigurationSource { EmbeddedJsonResources = [$"{this.GetType().Assembly.GetName().Name};DI.ServiceProviderFactoryRegistrar.{fileName}"] };
            hostAppBuilder.Configuration.AddConfiguration(configurationSource.Load());
            new Baubit.DI.ServiceProviderFactoryRegistrar().UseDefaultServiceProviderFactory(hostAppBuilder);

            var host = hostAppBuilder.Build();

            var component = host.Services.GetRequiredService<Component>();

            Assert.NotNull(component);
            Assert.False(string.IsNullOrEmpty(component.SomeString));
            Assert.False(string.IsNullOrEmpty(component.SomeSecretString));
        }
    }
}
