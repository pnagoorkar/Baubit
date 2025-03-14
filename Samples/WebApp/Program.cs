using Baubit.DI;
using WebApp;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("myConfig.json");
new ServiceProviderFactoryRegistrar().UseDefaultServiceProviderFactory(builder);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/myComponent", (MyComponent myComponent) => myComponent.DoSomething());

app.Run();
