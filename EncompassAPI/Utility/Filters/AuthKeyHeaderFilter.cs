using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using EncompassAPI.Models;
using EncompassAPI.Utility.Attributes;

namespace EncompassAPI.Utility.Filters
{
    public class AuthKeyHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Check if the action or controller has the ExternalVendorAttribute
            var hasAttribute = context.ApiDescription.CustomAttributes()
                .OfType<AuthKeyRequired>()
                .Any();

            if (hasAttribute)
            {
                if (operation.Parameters == null)
                    operation.Parameters = new List<OpenApiParameter>();

                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = CommonStrings.IncomingAuthKeyName,
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
}
