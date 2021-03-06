﻿using System;
using System.Collections.Generic;
using System.Linq;
using Glasswall.PolicyManagement.Common.Configuration;
using Glasswall.PolicyManagement.Common.Configuration.Validation;
using Glasswall.PolicyManagement.Common.Configuration.Validation.Errors;

namespace Glasswall.PolicyManagement.Business.Configuration
{
    public class EnvironmentVariableParser : IConfigurationParser
    {
        private readonly IDictionary<string, IConfigurationItemValidator> _configurationBinders;

        public EnvironmentVariableParser(IDictionary<string, IConfigurationItemValidator> configurationBinders)
        {
            _configurationBinders = configurationBinders ?? throw new ArgumentNullException(nameof(configurationBinders));
        }

        public TConfiguration Parse<TConfiguration>() where TConfiguration : new()
        {
            var errors = new List<ConfigurationParserError>();
            var config = new TConfiguration();

            foreach (var property in typeof(TConfiguration).GetProperties())
            {
                var rawValue = Environment.GetEnvironmentVariable(property.Name);

                if (_configurationBinders[property.Name].TryParse(property.Name, rawValue, errors, out var parsed))
                {
                    property.SetValue(config, parsed);
                }
            }

            if (errors.Any())
            {
                throw new ConfigurationBindException(errors);
            }

            return config;
        }
    }
}