﻿using Baubit.Validation;

namespace Baubit.Configuration
{
    public abstract class AConfigurationValidator<TConfiguration> : AValidator<TConfiguration> where TConfiguration : AConfiguration
    {

    }
}
