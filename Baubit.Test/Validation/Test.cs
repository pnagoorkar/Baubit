using Baubit.Test.Validation.Setup;
using Baubit.Validation;

namespace Baubit.Test.Validation
{
    public class Test
    {
        [Fact]
        public void CanValidateObjects()
        {
            var validatable = new Validatable();
            var result = validatable.GetValidator("default")
                       .Bind(validator => validator.Validate(validatable));
            Assert.True(result.IsSuccess);
        }
    }
}
