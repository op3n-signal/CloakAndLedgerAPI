using EncompassAPI.Models;
using Microsoft.OpenApi.Models;
using Shared.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EncompassAPI.Utility.Filters
{
    public class AuthHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if the action or controller has the ExternalVendorAttribute
            var hasAttribute = context.ApiDescription.CustomAttributes()
                .OfType<AuthRequired>()
                .Any();

            if (hasAttribute)
            {
                if (operation.Parameters == null)
                    operation.Parameters = new List<OpenApiParameter>();

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = CommonFields.IncomingAuthKeyName,
                    In = ParameterLocation.Header,
                    Required = true, // Mark as required or optional as needed
                    Description = "Required to authenticate to this API",
                    Schema = new OpenApiSchema
                    {
                        Type = "string"
                    }
                });
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AuthRequired : Attribute
    {
    }
}
