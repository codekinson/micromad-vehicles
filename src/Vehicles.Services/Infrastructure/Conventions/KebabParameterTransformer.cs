using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Routing;

namespace Vehicles.Services.Infrastructure.Conventions
{
    public class KebabParameterTransformer: IOutboundParameterTransformer
    {
        private static readonly Regex CamelCasingRegEx = new Regex("([a-z])([A-Z])", RegexOptions.Compiled);
        
        public string TransformOutbound(object value)
        {
            if (value == null) { return null; }

            return CamelCasingRegEx
                .Replace(value.ToString(), "$1-$2")
                .Trim()
                .ToLower();
        }
    }
}