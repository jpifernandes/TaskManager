using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Jwt.Model;
using NetDevPack.Identity.Model;
using TaskManager.Models;
using TaskManager.Models.DTOs;

namespace TaskManager
{
    public static class AppConfigurator
    {
        public static WebApplication ConfigureApp(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthConfiguration()
               .UseHttpsRedirection();

            return app;
        }

        public static WebApplication MapEndpoints(this WebApplication app)
        {
            #region Auth

            app.MapPost("/create-user", async (SignInManager<IdentityUser> signInManager,
                                               UserManager<IdentityUser> userManager,
                                               IOptions<AppJwtSettings> appJwtSettings,
                                               RegisterUser registerUser) =>
            {
                if (registerUser == null)
                    return Results.Problem(detail: "User not informed.", statusCode: 400);

                if (string.IsNullOrEmpty(registerUser.Email))
                    return Results.Problem(detail: "Invalid email.", statusCode: 400);

                if (string.IsNullOrEmpty(registerUser.Password) || registerUser.Password.Length < 6)
                    return Results.Problem(detail: "Invalid password.", statusCode: 400);

                if (registerUser.Password != registerUser.ConfirmPassword)
                    return Results.Problem(detail: "Passwords are not matching.", statusCode: 400);

                var newUser = new IdentityUser
                {
                    UserName = registerUser.Email,
                    Email = registerUser.Email,
                    EmailConfirmed = true
                };

                IdentityResult createUserResult = await userManager.CreateAsync(newUser, registerUser.Password);

                if (!createUserResult.Succeeded)
                    return Results.BadRequest(createUserResult.Errors);

                var jwt = new JwtBuilder().WithUserManager(userManager)
                                          .WithJwtSettings(appJwtSettings.Value)
                                          .WithEmail(newUser.Email)
                                          .WithJwtClaims()
                                          .WithUserClaims()
                                          .WithUserRoles()
                                          .BuildUserResponse();

                return Results.Ok(jwt);
            })
            .ProducesValidationProblem()
            .Produces<UserResponse>(200)
            .Produces(500)
            .AllowAnonymous()
            .WithName("CreateUser")
            .WithTags("Auth");

            app.MapPost("/auth", async (SignInManager<IdentityUser> signInManager,
                                        UserManager<IdentityUser> userManager,
                                        IOptions<AppJwtSettings> appJwtSettings,
                                        LoginUser loginUser) =>
            {
                if (loginUser == null)
                    return Results.Problem(detail: "Login not informed.", statusCode: 400);

                SignInResult signInResult = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

                if (signInResult.IsLockedOut)
                    return Results.Problem(detail: "User blocked.", statusCode: 400);

                if(!signInResult.Succeeded)
                    return Results.Problem(detail: "Invalid email or password.", statusCode: 400);

                var jwt = new JwtBuilder().WithUserManager(userManager)
                                          .WithJwtSettings(appJwtSettings.Value)
                                          .WithEmail(loginUser.Email)
                                          .WithJwtClaims()
                                          .WithUserClaims()
                                          .WithUserRoles()
                                          .BuildUserResponse();

                return Results.Ok(jwt);
            })
            .ProducesValidationProblem()
            .Produces<UserResponse>(200)
            .Produces(500)
            .AllowAnonymous()
            .WithName("Auth")
            .WithTags("Auth");

            #endregion

            #region Tasks

            app.MapGet("/tasks", async (EfContext efContext, PersonalTaskStatus? status) =>
            {
                IQueryable<PersonalTask> query = efContext.PersonalTasks.AsNoTracking();

                IEnumerable<PersonalTask> tasks = status.HasValue
                    ? await query.Where(t => t.IsAvailable && t.Status == status).ToArrayAsync()
                    : await query.Where(t => t.IsAvailable).ToArrayAsync();

                var result = new List<PersonalTaskDto?>();

                foreach (PersonalTask task in tasks)
                    result.Add(PersonalTaskDto.Create(task));

                return Results.Ok(result);
            })
            .Produces<IEnumerable<PersonalTaskDto>>(200)
            .Produces(500)
            .RequireAuthorization()
            .WithName("GetTasks")
            .WithTags("Tasks");

            app.MapGet("/tasks/{id}", async (EfContext efContext, int id) =>
            {
                PersonalTask? task = await efContext.PersonalTasks.AsNoTracking()
                                                                  .FirstOrDefaultAsync(t => t.IsAvailable && t.Id == id);

                if (task == null)
                    return Results.NotFound();

                PersonalTaskDto? result = PersonalTaskDto.Create(task);

                return Results.Ok(result);
            })
            .Produces<PersonalTaskDto>(200)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization()
            .WithName("GetTaskById")
            .WithTags("Tasks");

            app.MapPost("/tasks", async (EfContext efContext, NewPersonalTaskRequest request) =>
            {
                if (request == null)
                    return Results.Problem(detail: "Request body cannot be null.", statusCode: 400);

                if (string.IsNullOrEmpty(request.Title))
                    return Results.Problem(detail: $"Property {nameof(request.Title)} cannot be null or empty.", statusCode: 400);

                var newTask = new PersonalTask
                {
                    Title = request.Title,
                    Description = request.Description,
                    Status = PersonalTaskStatus.Created,
                    CreatedAt = DateTime.UtcNow,
                    DueDate = request.DueDate,
                    IsAvailable = true
                };

                efContext.Add(newTask);
                
                int numberOfEntries = await efContext.SaveChangesAsync();

                if (numberOfEntries == 0)
                    return Results.Problem(detail: "An error ocurred while saving the task in the database.", statusCode: 500);

                return Results.CreatedAtRoute("GetTaskById", new { newTask.Id }, PersonalTaskDto.Create(newTask));
            })
            .ProducesValidationProblem()
            .Produces<PersonalTaskDto>(201)
            .Produces(500)
            .RequireAuthorization()
            .WithName("AddNewTask")
            .WithTags("Tasks");

            app.MapPut("/tasks/{id}", async (EfContext efContext, int id, PersonalTaskDto dto) =>
            {
                if(dto == null)
                    return Results.Problem(detail: "Request body cannot be null.", statusCode: 400);

                if (string.IsNullOrEmpty(dto.Title))
                    return Results.Problem(detail: $"Property {nameof(dto.Title)} cannot be null or empty.", statusCode: 400);

                PersonalTask? task = await efContext.PersonalTasks.FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return Results.NotFound();

                if (!task.IsAvailable)
                    return Results.Problem(detail: "Task not available.", statusCode: 400);

                task.Title = dto.Title;
                task.Description = dto.Description;
                task.Status = dto.Status;
                task.CreatedAt = dto.CreatedAt;
                task.DueDate = dto.DueDate;

                int numberOfEntries = await efContext.SaveChangesAsync();

                if (numberOfEntries == 0)
                    return Results.Problem(detail: "An error ocurred while updating the task in the database.", statusCode: 500);

                return Results.NoContent();
            })
            .ProducesValidationProblem()
            .Produces(204)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization()
            .WithName("UpdateTask")
            .WithTags("Tasks");

            app.MapDelete("/tasks/{id}", async (EfContext efContext, int id) =>
            {
                PersonalTask? task = await efContext.PersonalTasks.FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return Results.NotFound();

                if (task.IsAvailable)
                {
                    task.IsAvailable = false;

                    int numberOfEntries = await efContext.SaveChangesAsync();

                    if (numberOfEntries == 0)
                        return Results.Problem(detail: "An error ocurred while removing the task from the database.", statusCode: 500);
                }
                   
                return Results.NoContent();
            })
            .Produces(204)
            .Produces(404)
            .Produces(500)
            .RequireAuthorization("DeleteTask")
            .WithName("DeleteTask")
            .WithTags("Tasks");

            #endregion

            return app;
        }
    }
}
