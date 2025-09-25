using Microsoft.AspNetCore.Http;

namespace ScrumPoker.Application;

public static class Validation
{
    public static IResult Problem(string detail, string title = "Validation Failed", string instance = "/api")
    {
        var problem = new
        {
            type = "https://httpstatuses.com/400",
            title,
            status = 400,
            detail,
            instance
        };
        return Results.Json(problem, statusCode: StatusCodes.Status400BadRequest);
    }
}
