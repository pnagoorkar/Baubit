using Baubit.Reflection;

namespace Baubit.Test.Reflection.Type
{
    public class Test
    {
        [Fact]
        public void CanGetBaubitFormattedAssemblyQualifiedName()
        {
            var result = this.GetType().GetBaubitFormattedAssemblyQualifiedName();
            Assert.True(result.IsSuccess);
            Assert.DoesNotContain("Version=", result.Value);
            Assert.DoesNotContain("Culture=", result.Value);
            Assert.DoesNotContain("PublicKeyToken=", result.Value);
            var getTypeResult = TypeResolver.TryResolveType(result.Value);
            Assert.True(getTypeResult.IsSuccess);
            Assert.Equal(this.GetType(), getTypeResult.Value);
        }
        [Fact]
        public void CanGetBaubitFormattedAssemblyQualifiedName_GenericTypes()
        {
            var result = typeof(List<string>).GetBaubitFormattedAssemblyQualifiedName();
            Assert.True(result.IsSuccess);
            Assert.DoesNotContain("Version=", result.Value);
            Assert.DoesNotContain("Culture=", result.Value);
            Assert.DoesNotContain("PublicKeyToken=", result.Value);
            var getTypeResult = TypeResolver.TryResolveType(result.Value);
            Assert.True(getTypeResult.IsSuccess);
            Assert.Equal(typeof(List<string>), getTypeResult.Value);
        }
    }
}
