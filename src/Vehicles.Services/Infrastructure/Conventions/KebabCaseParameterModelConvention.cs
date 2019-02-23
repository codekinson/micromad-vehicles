using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Vehicles.Services.Infrastructure.Conventions
{
    public class KebabCaseParameterModelConvention : IParameterModelConvention
    {
        private static readonly Regex CamelCasingRegEx = new Regex("([a-z])([A-Z])", RegexOptions.Compiled);
        
        public void Apply(ParameterModel parameter)
        {
            if (parameter?.BindingInfo?.BindingSource == BindingSource.Query)
            {
                parameter.BindingInfo.BinderModelName = CamelCasingRegEx
                    .Replace(parameter.Name, "$1-$2")
                    .Trim()
                    .ToLower();
            }
        }
    }
}