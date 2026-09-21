using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sparks.Api.Controllers;
using Sparks.Api.Data;
using Sparks.Api.Dtos;
using Sparks.Api.Models;
using Xunit;

namespace Sparks.Api.Tests;

public class ProfileControllerTests
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

    private static Mock<IWebHostEnvironment> CreateMockEnv(string contentRootPath)
    {
        var mock = new Mock<IWebHostEnvironment>();
        mock.Setup(e => e.ContentRootPath).Returns(contentRootPath);
        return mock;
    }

    private static ProfileController CreateController(
        AppDbContext db, UserManager<ApplicationUser> userManager, string? contentRootPath = null)
    {
        var controller = new ProfileController(userManager, db, CreateMockEnv(contentRootPath ?? Path.GetTempPath()).Object);
        return controller;
    }

    private static void SetCurrentUser(ControllerBase controller, Guid userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
    }

    private static UpdateProfileDto ValidUpdateDto(bool isActive = true) =>
        new("Marc", "Dubois", "STL-1", "marc.stellantis@stellantis.com", "ALT-1", ["French", "English"], isActive);

    // ---- UpdateProfile ----

    [Fact]
    public async Task UpdateProfile_NoSessionClaim_ReturnsUnauthorized()
    {
        var db = CreateInMemoryDb();
        var mockUserManager = CreateMockUserManager();
        var controller = CreateController(db, mockUserManager.Object);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.UpdateProfile(ValidUpdateDto());

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateProfile_LastActiveAdminTriesToDeactivateSelf_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var admin = new ApplicationUser { FirstName = "Amara", LastName = "Diallo", Email = "amara@alten.com", Role = UserRole.Admin, Status = UserStatus.Active };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(admin.Id.ToString())).ReturnsAsync(admin);

        var controller = CreateController(db, mockUserManager.Object);
        SetCurrentUser(controller, admin.Id);

        var result = await controller.UpdateProfile(ValidUpdateDto(isActive: false));

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateProfile_NotLastActiveAdmin_CanDeactivateSelf()
    {
        var db = CreateInMemoryDb();
        var admin1 = new ApplicationUser { FirstName = "Amara", LastName = "Diallo", Email = "amara@alten.com", Role = UserRole.Admin, Status = UserStatus.Active };
        var admin2 = new ApplicationUser { FirstName = "Gamatel", LastName = "Gamatel", Email = "gamatel@alten.com", Role = UserRole.Admin, Status = UserStatus.Active };
        db.Users.AddRange(admin1, admin2);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(admin1.Id.ToString())).ReturnsAsync(admin1);
        mockUserManager.Setup(m => m.UpdateAsync(admin1)).ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(db, mockUserManager.Object);
        SetCurrentUser(controller, admin1.Id);

        var result = await controller.UpdateProfile(ValidUpdateDto(isActive: false));

        Assert.IsType<JwtClaimsDto>(result.Value);
        Assert.Equal(UserStatus.Inactive, admin1.Status);
    }

    [Fact]
    public async Task UpdateProfile_ValidRequest_UpdatesFieldsAndReturnsClaims()
    {
        var db = CreateInMemoryDb();
        var user = new ApplicationUser { FirstName = "Old", LastName = "Name", Email = "marc@alten.com", Role = UserRole.Generalist, Status = UserStatus.Active };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(db, mockUserManager.Object);
        SetCurrentUser(controller, user.Id);

        var result = await controller.UpdateProfile(ValidUpdateDto());

        var dto = Assert.IsType<JwtClaimsDto>(result.Value);
        Assert.Equal("Marc", dto.FirstName);
        Assert.Equal("Dubois", dto.LastName);
        Assert.Equal(new List<string> { "French", "English" }, user.LanguagesList);
    }

    // ---- UploadPhoto ----

    private static FormFile BuildFormFile(string fileName, int sizeBytes = 100)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(new string('a', sizeBytes)));
        return new FormFile(stream, 0, stream.Length, "file", fileName);
    }

    [Fact]
    public async Task UploadPhoto_NoFile_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var user = new ApplicationUser { FirstName = "Marc", LastName = "Dubois", Email = "marc@alten.com", Role = UserRole.Generalist };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager.Object);
        SetCurrentUser(controller, user.Id);

        var result = await controller.UploadPhoto(null!);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UploadPhoto_TooLarge_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var user = new ApplicationUser { FirstName = "Marc", LastName = "Dubois", Email = "marc@alten.com", Role = UserRole.Generalist };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager.Object);
        SetCurrentUser(controller, user.Id);

        var oversized = new Mock<IFormFile>();
        oversized.Setup(f => f.Length).Returns(6 * 1024 * 1024);
        oversized.Setup(f => f.FileName).Returns("big.png");

        var result = await controller.UploadPhoto(oversized.Object);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UploadPhoto_DisallowedExtension_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var user = new ApplicationUser { FirstName = "Marc", LastName = "Dubois", Email = "marc@alten.com", Role = UserRole.Generalist };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager.Object);
        SetCurrentUser(controller, user.Id);

        var file = BuildFormFile("malware.exe");

        var result = await controller.UploadPhoto(file);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UploadPhoto_ValidJpg_SavesFileAndUpdatesProfilePhotoUrl()
    {
        var db = CreateInMemoryDb();
        var user = new ApplicationUser { FirstName = "Marc", LastName = "Dubois", Email = "marc@alten.com", Role = UserRole.Generalist };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var tempRoot = Path.Combine(Path.GetTempPath(), "sparks-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(tempRoot);
        try
        {
            var controller = CreateController(db, mockUserManager.Object, tempRoot);
            SetCurrentUser(controller, user.Id);

            var file = BuildFormFile("avatar.jpg");

            var result = await controller.UploadPhoto(file);

            var dto = Assert.IsType<JwtClaimsDto>(result.Value);
            Assert.Equal($"/uploads/avatars/{user.Id}.jpg", dto.ProfilePhotoUrl);
            Assert.True(File.Exists(Path.Combine(tempRoot, "wwwroot", "uploads", "avatars", $"{user.Id}.jpg")));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
