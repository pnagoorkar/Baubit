using Baubit.Configuration;
using Baubit.DI;
using Baubit.States;
using Baubit.Test.States.State.Setup;
using Microsoft.Extensions.DependencyInjection;

namespace Baubit.Test.States.State
{
    public class Test
    {
        static IModule[] requiredModules =
        [
            new Baubit.States.DI.Module<MyStatefulType.States>(ConfigurationSource.Empty),
            new Baubit.Caching.Default.DI.Module<MyStatefulType.States>(ConfigurationSource.Empty),
            new Baubit.Caching.Default.DI.Module<StateChanged<MyStatefulType.States>>(ConfigurationSource.Empty),
            new Baubit.Logging.DI.Default.Module(ConfigurationSource.Empty)
        ];

        [Fact]
        public void CanInitializeState()
        {
            var typeBuildResult = ComponentBuilder<MyStatefulType>.Create()
                                                                  .Bind(componentBuilder => componentBuilder.WithRegistrationHandler(services => services.AddSingleton<MyStatefulType>()))
                                                                  .Bind(componentBuilder => componentBuilder.UsingModules(requiredModules))
                                                                  .Bind(componentBuilder => componentBuilder.Build(true));

            Assert.True(typeBuildResult.IsSuccess);
            Assert.NotNull(typeBuildResult.Value);
            Assert.Equal(MyStatefulType.States.Default, typeBuildResult.Value.State.Current);
        }

        [Fact]
        public async Task CanSubscribeToStateChanges()
        {
            var typeBuildResult = ComponentBuilder<MyStatefulType>.Create()
                                                                  .Bind(componentBuilder => componentBuilder.WithRegistrationHandler(services => services.AddSingleton<MyStatefulType>()))
                                                                  .Bind(componentBuilder => componentBuilder.UsingModules(requiredModules))
                                                                  .Bind(componentBuilder => componentBuilder.Build(true));

            Assert.True(typeBuildResult.IsSuccess);
            Assert.NotNull(typeBuildResult.Value);
            Assert.Equal(MyStatefulType.States.Default, typeBuildResult.Value.State.Current);

            var myObserver = new MyStateObserver();
            var subscription = typeBuildResult.Value.State.Subscribe(myObserver);
            Assert.Empty(myObserver.ChangeEvents);
            typeBuildResult.Value.State.Set(MyStatefulType.States.State1);
            var stateChangeResult = await typeBuildResult.Value.State.AwaitAsync(MyStatefulType.States.State1);
            Assert.NotEmpty(myObserver.ChangeEvents);
        }

        [Fact]
        public async Task CanDisposeStateGracefully()
        {
            var typeBuildResult = ComponentBuilder<StateFactory<MyStatefulType.States>>.Create()
                                                                                       .Bind(componentBuilder => componentBuilder.WithRegistrationHandler(services => services.AddSingleton<MyStatefulType>()))
                                                                                       .Bind(componentBuilder => componentBuilder.UsingModules(requiredModules))
                                                                                       .Bind(componentBuilder => componentBuilder.Build(true));

            Assert.True(typeBuildResult.IsSuccess);
            Assert.NotNull(typeBuildResult.Value);
            var state = typeBuildResult.Value();
            Assert.Equal(MyStatefulType.States.Default, state.Current);

            state.Set(MyStatefulType.States.State1);
            var stateChangeResult = await state.AwaitAsync(MyStatefulType.States.State1);

            state.Dispose();

        }
    }
}
