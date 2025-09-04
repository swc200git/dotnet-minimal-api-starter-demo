using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Api.Infrastructure;

/// <summary>
/// Transforms OpenAPI document to include JWT Bearer authentication scheme
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    /// <summary>
    /// Adds JWT Bearer security scheme to OpenAPI documentation
    /// </summary>
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        // Define JWT Bearer security scheme
        var requirements = new Dictionary<string, OpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,              // HTTP authentication
                Scheme = "bearer",                          // Bearer token scheme
                BearerFormat = "JWT",                       // JWT format specification
                In = ParameterLocation.Header,              // Authorization header
                Description = "Enter 'Bearer {token}'"     // User instruction for Swagger UI
            }
        };
        
        // Add security schemes to document
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = requirements;

        // Apply security requirement to all operations
        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
        {
            operation.Value.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme 
                { 
                    Reference = new OpenApiReference 
                    { 
                        Id = "Bearer", 
                        Type = ReferenceType.SecurityScheme 
                    } 
                }] = Array.Empty<string>()
            });
        }

        return Task.CompletedTask;
    }
}
