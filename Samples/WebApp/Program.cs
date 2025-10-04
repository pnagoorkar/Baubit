using Baubit.DI;
using WebApp;

var app = WebApplication.CreateBuilder()
                        .UseConfiguredServiceProviderFactory()
                        .Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/myComponent", (MyComponent myComponent) => myComponent.DoSomething());

await app.RunAsync().ConfigureAwait(false);
