//using Baubit.Configuration;
//using Baubit.DI;
//using Baubit.States;
//using Baubit.Test.States.State.Setup;
//using Microsoft.Extensions.DependencyInjection;

//namespace Baubit.Test.States.State
//{
//    public class Test
//    {
//        static Feature feature = new Feature();

//        [Fact]
//        public void CanInitializeState()
//        {
//            var typeBuildResult = ComponentBuilder<MyStatefulType>.Create()
//                                                                  .Bind(componentBuilder => componentBuilder.WithRegistrationHandler(services => services.AddSingleton<MyStatefulType>()))
//                                                                  .Bind(componentBuilder => componentBuilder.WithFeatures(feature))
//                                                                  .Bind(componentBuilder => componentBuilder.Build(true));

//            Assert.True(typeBuildResult.IsSuccess);
//            Assert.NotNull(typeBuildResult.Value);
//            Assert.Equal(MyStatefulType.States.Default, typeBuildResult.Value.State.Current);
//        }

//        [Fact]
//        public async Task CanSubscribeToStateChanges()
//        {
//            var typeBuildResult = ComponentBuilder<MyStatefulType>.Create()
//                                                                  .Bind(componentBuilder => componentBuilder.WithRegistrationHandler(services => services.AddSingleton<MyStatefulType>()))
//                                                                  .Bind(componentBuilder => componentBuilder.WithFeatures(feature))
//                                                                  .Bind(componentBuilder => componentBuilder.Build(true));

//            Assert.True(typeBuildResult.IsSuccess);
//            Assert.NotNull(typeBuildResult.Value);
//            Assert.Equal(MyStatefulType.States.Default, typeBuildResult.Value.State.Current);

//            var myObserver = new MyStateObserver();
//            var subscription = typeBuildResult.Value.State.Subscribe(myObserver);
//            Assert.Empty(myObserver.ChangeEvents);
//            typeBuildResult.Value.State.Set(MyStatefulType.States.State1);
//            var stateChangeResult = await typeBuildResult.Value.State.AwaitAsync(MyStatefulType.States.State1);
//            await Task.Delay(100);
//            Assert.NotEmpty(myObserver.ChangeEvents);
//        }

//        [Fact]
//        public async Task CanDisposeStateGracefully()
//        {
//            var typeBuildResult = ComponentBuilder<StateFactory<MyStatefulType.States>>.Create()
//                                                                                       .Bind(componentBuilder => componentBuilder.WithRegistrationHandler(services => services.AddSingleton<MyStatefulType>()))
//                                                                                       .Bind(componentBuilder => componentBuilder.WithFeatures(feature))
//                                                                                       .Bind(componentBuilder => componentBuilder.Build(true));

//            Assert.True(typeBuildResult.IsSuccess);
//            Assert.NotNull(typeBuildResult.Value);
//            var state = typeBuildResult.Value();
//            Assert.Equal(MyStatefulType.States.Default, state.Current);

//            state.Set(MyStatefulType.States.State1);
//            var stateChangeResult = await state.AwaitAsync(MyStatefulType.States.State1);

//            state.Dispose();

//        }
//    }
//}
