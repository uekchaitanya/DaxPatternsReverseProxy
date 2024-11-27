using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Validations.Rules;

namespace DaxPatternsReverseProxy;

public class CustomProxy
{
    const string scopeRequiredByAPI = "access_as_user" ;
    private readonly RequestDelegate _next;
    
    public CustomProxy(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        var isFlagTrue = IsValidRole(context);
        
        
            //we can check for the role to the specific API or etc here
            
            if (isFlagTrue)
            {
                // Forward the request to the original service
                await _next(context);
            }
            else
            {
                // If flag is false or there is an issue, return a custom response
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Request blocked based on flag.");
            }

            return;


            // Call the next middleware in the pipeline
        await _next(context);
    }

    private static bool IsValidRole(HttpContext context)
    {
        //get role from DB /validate from user Claims 
        //context.User.Claims
        //context.Request.Path

        var readUriList = new List<string>() { };
        
        var writeUriList = new List<string>() { };

        

        //if (context.Request.Path.StartsWithSegments("/api/Allergy/GetAllergies"))
        if (context.Request.Path.StartsWithSegments("/api/"))
        {
            RemoveBearerToken(context); //toTest
            return true;

        }

        var x = context.User.Claims;
        return false;
    }

    private static void RemoveBearerToken(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                // Remove the Authorization header completely
                context.Request.Headers.Remove("Authorization");
        
                // Alternatively, if you want to keep the header but remove the token:
                // string[] parts = authHeader.ToString().Split(' ');
                // if (parts.Length == 2 && parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                // {
                //     context.Request.Headers["Authorization"] = "Bearer [removed]";
                // }
            }
        }
}