
using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.Logging;

using Moq;

using Sparks.Api.Controllers;

using Sparks.Api.Data;

using Sparks.Api.Dtos;

using Sparks.Api.Models;

using Sparks.Api.Services;

using Xunit;



namespace Sparks.Api.Tests;



public class UsersControllerTests

{

    private static AppDbContext CreateInMemoryDb()

    {

        var options = new DbContextOptionsBuilder<AppDbContext>()

            .UseInMemoryDatabase(Guid.NewGuid().ToString())

            .Options;

        return new AppDbContext(options);

    }



    private static Mock<UserManager<ApplicationUser>> CreateMockUserManager()

    {

        var store = new Mock<IUserStore<ApplicationUser>>();

        return new Mock<UserManager<ApplicationUser>>(

            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

    }



    private static Mock<EmailService> CreateMockEmailService()

    {

        var config = new ConfigurationBuilder().Build();

        var mock = new Mock<EmailService>(config, Mock.Of<ILogger<EmailService>>());

        mock.Setup(m => m.SendTemporaryPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))

            .Returns(Task.CompletedTask);

        mock.Setup(m => m.SendPasswordResetCodeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))

            .Returns(Task.CompletedTask);

        return mock;

    }



    private static UsersController CreateController(

        AppDbContext db,

        UserManager<ApplicationUser>? userManager = null,

        EmailService? emailService = null)

    {

        var controller = new UsersController(

            db,

            userManager ?? CreateMockUserManager().Object,

            emailService ?? CreateMockEmailService().Object,

            Mock.Of<ILogger<UsersController>>());

        TestSupport.SetFakeHttpContext(controller);

        return controller;

    }



    [Fact]

    public async Task List_FiltersBySearch_ReturnsMatchingUsers()

    {

        var db = CreateInMemoryDb();

        db.Users.Add(new ApplicationUser { FirstName = "Marc", LastName = "Dubois", Email = "marc@alten.com", Role = UserRole.Generalist });

        db.Users.Add(new ApplicationUser { FirstName = "Sofia", LastName = "Reyes", Email = "sofia@alten.com", Role = UserRole.Generalist });

        await db.SaveChangesAsync();



        var controller = CreateController(db);



        var result = await controller.List(search: "marc", role: null, status: null);



        var users = Assert.IsType<List<UserDto>>(result.Value);

        Assert.Single(users);

        Assert.Equal("Marc", users[0].FirstName);

    }



    [Fact]

    public async Task List_NoFilters_ReturnsAllUsers()

    {

        var db = CreateInMemoryDb();

        db.Users.Add(new ApplicationUser { FirstName = "Marc", LastName = "Dubois", Email = "marc@alten.com", Role = UserRole.Generalist });

        db.Users.Add(new ApplicationUser { FirstName = "Sofia", LastName = "Reyes", Email = "sofia@alten.com", Role = UserRole.Generalist });

        await db.SaveChangesAsync();



        var controller = CreateController(db);



        var result = await controller.List(search: null, role: null, status: null);



        var users = Assert.IsType<List<UserDto>>(result.Value);

        Assert.Equal(2, users.Count);

    }



    [Fact]

    public async Task Delete_LastAdmin_ReturnsBadRequest()

    {

        var db = CreateInMemoryDb();

        var admin = new ApplicationUser { FirstName = "Amara", LastName = "Diallo", Email = "amara@alten.com", Role = UserRole.Admin };

        db.Users.Add(admin);

        await db.SaveChangesAsync();



        var mockUserManager = CreateMockUserManager();

        mockUserManager.Setup(m => m.FindByIdAsync(admin.Id.ToString())).ReturnsAsync(admin);



        var controller = CreateController(db, mockUserManager.Object);



        var result = await controller.Delete(admin.Id);



        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);

    }



    [Fact]

    public async Task Delete_NotLastAdmin_Succeeds()

    {

        var db = CreateInMemoryDb();

        var admin1 = new ApplicationUser { FirstName = "Amara", LastName = "Diallo", Email = "amara@alten.com", Role = UserRole.Admin };

        var admin2 = new ApplicationUser { FirstName = "Gamatel", LastName = "Gamatel", Email = "gamatel@alten.com", Role = UserRole.Admin };

        db.Users.AddRange(admin1, admin2);

        await db.SaveChangesAsync();



        var mockUserManager = CreateMockUserManager();

        mockUserManager.Setup(m => m.FindByIdAsync(admin1.Id.ToString())).ReturnsAsync(admin1);

        mockUserManager.Setup(m => m.DeleteAsync(admin1)).ReturnsAsync(IdentityResult.Success);



        var controller = CreateController(db, mockUserManager.Object);



        var result = await controller.Delete(admin1.Id);



        Assert.IsType<Microsoft.AspNetCore.Mvc.NoContentResult>(result);

    }



    [Fact]

    public async Task Create_ValidRequest_ReturnsUserDto()

    {

        var db = CreateInMemoryDb();

        var mockUserManager = CreateMockUserManager();

        mockUserManager

            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))

            .ReturnsAsync(IdentityResult.Success);

        mockUserManager

            .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))

            .ReturnsAsync(IdentityResult.Success);



        var controller = CreateController(db, mockUserManager.Object);

        var request = new CreateUserDto("Léa", "Fontaine", "l.fontaine@alten.com", UserRole.Polyvalent);



        var result = await controller.Create(request);



        var dto = Assert.IsType<UserDto>(result.Value);

        Assert.Equal("Léa", dto.FirstName);

        Assert.Equal(UserRole.Polyvalent, dto.Role);

    }



    [Fact]

    public async Task Create_IdentityFails_ReturnsBadRequest()

    {

        var db = CreateInMemoryDb();

        var mockUserManager = CreateMockUserManager();

        mockUserManager

            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))

            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email 'x' is already taken." }));



        var controller = CreateController(db, mockUserManager.Object);

        var request = new CreateUserDto("Test", "User", "duplicate@alten.com", UserRole.Generalist);



        var result = await controller.Create(request);



        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result.Result);

    }



    [Fact]

    public async Task Create_EmailServiceThrows_StillPersistsUserAndReturnsCreatedUser()

    {

        var db = CreateInMemoryDb();

        var mockUserManager = CreateMockUserManager();

        mockUserManager

            .Setup(m => m.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))

            .ReturnsAsync(IdentityResult.Success)

            .Callback<ApplicationUser, string>((user, _) =>
            {
                db.Users.Add(user);
                db.SaveChanges();
            });

        mockUserManager

            .Setup(m => m.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))

            .ReturnsAsync(IdentityResult.Success);



        var mockEmailService = CreateMockEmailService();

        mockEmailService

            .Setup(m => m.SendTemporaryPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))

            .ThrowsAsync(new InvalidOperationException("SMTP unreachable"));



        var controller = CreateController(db, mockUserManager.Object, mockEmailService.Object);

        var request = new CreateUserDto("Test", "User", "test@alten.com", UserRole.Generalist);



        var result = await controller.Create(request);



        var dto = Assert.IsType<UserDto>(result.Value);

        Assert.Equal("Test", dto.FirstName);

        Assert.Equal(request.Email, dto.Email);

        Assert.Equal(1, await db.Users.CountAsync(u => u.Email == request.Email));

    }



    [Fact]

    public async Task ResetPassword_MatchingPasswords_ReturnsOk()

    {

        var db = CreateInMemoryDb();

        var user = new ApplicationUser { FirstName = "Marc", LastName = "Dubois", Email = "marc@alten.com", Role = UserRole.Generalist };

        db.Users.Add(user);

        await db.SaveChangesAsync();



        var mockUserManager = CreateMockUserManager();

        mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        mockUserManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("fake-token");

        mockUserManager

            .Setup(m => m.ResetPasswordAsync(user, "fake-token", "NewPass123!"))

            .ReturnsAsync(IdentityResult.Success);



        var controller = CreateController(db, mockUserManager.Object);

        var request = new ResetPasswordDto("NewPass123!", "NewPass123!");



        var result = await controller.ResetPassword(user.Id, request);



        Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result);

    }



    [Fact]

    public async Task ResetPassword_MismatchedPasswords_ReturnsBadRequest()

    {

        var db = CreateInMemoryDb();

        var user = new ApplicationUser { FirstName = "Marc", LastName = "Dubois", Email = "marc@alten.com", Role = UserRole.Generalist };

        db.Users.Add(user);

        await db.SaveChangesAsync();



        var controller = CreateController(db);

        var request = new ResetPasswordDto("NewPass123!", "Different456!");



        var result = await controller.ResetPassword(user.Id, request);



        Assert.IsType<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>(result);

    }

}

