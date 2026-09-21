using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

public class AuthControllerTests
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

    private static Mock<SignInManager<ApplicationUser>> CreateMockSignInManager(UserManager<ApplicationUser> userManager)
    {
        var contextAccessor = Mock.Of<IHttpContextAccessor>();
        var claimsFactory = Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>();
        return new Mock<SignInManager<ApplicationUser>>(
            userManager, contextAccessor, claimsFactory, null!, null!, null!, null!);
    }

    private static Mock<EmailService> CreateMockEmailService()
    {
        var config = new ConfigurationBuilder().Build();
        var mock = new Mock<EmailService>(config, Mock.Of<ILogger<EmailService>>());
        mock.Setup(m => m.SendPasswordResetCodeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mock.Setup(m => m.SendTemporaryPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    /// <summary>TokenService has no interface and no virtual members, so it can't be mocked — it is
    /// deterministic and side-effect-free, so tests use a real instance backed by an in-memory Jwt config.</summary>
    private static TokenService CreateRealTokenService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "SparksApiTests",
                ["Jwt:Audience"] = "SparksClientTests",
                ["Jwt:Key"] = "test-signing-key-not-for-production-32chars-min",
                ["Jwt:AccessTokenMinutes"] = "60",
                ["Jwt:RefreshTokenDays"] = "14",
            })
            .Build();
        return new TokenService(config);
    }

    private static AuthController CreateController(
        AppDbContext db,
        UserManager<ApplicationUser>? userManager = null,
        SignInManager<ApplicationUser>? signInManager = null,
        EmailService? emailService = null,
        TokenService? tokenService = null)
    {
        var manager = userManager ?? CreateMockUserManager().Object;
        return new AuthController(
            manager,
            signInManager ?? CreateMockSignInManager(manager).Object,
            tokenService ?? CreateRealTokenService(),
            db,
            emailService ?? CreateMockEmailService().Object,
            Mock.Of<ILogger<AuthController>>());
    }

    private static ApplicationUser CreateUser(UserStatus status = UserStatus.Active) => new()
    {
        FirstName = "Marc",
        LastName = "Dubois",
        Email = "marc@alten.com",
        Role = UserRole.Generalist,
        Status = status,
    };

    // ---- Login ----

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var db = CreateInMemoryDb();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync("nobody@alten.com")).ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.Login(new LoginRequestDto("nobody@alten.com", "Password1!"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_InactiveUser_ReturnsUnauthorized()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser(UserStatus.Inactive);
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.Login(new LoginRequestDto(user.Email!, "Password1!"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_LockedOut_ReturnsUnauthorizedWithLockMessage()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
        mockSignInManager
            .Setup(m => m.CheckPasswordSignInAsync(user, "WrongPass1!", true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var controller = CreateController(db, mockUserManager.Object, mockSignInManager.Object);

        var result = await controller.Login(new LoginRequestDto(user.Email!, "WrongPass1!"));

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Contains("locked", unauthorized.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
        mockSignInManager
            .Setup(m => m.CheckPasswordSignInAsync(user, "WrongPass1!", true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var controller = CreateController(db, mockUserManager.Object, mockSignInManager.Object);

        var result = await controller.Login(new LoginRequestDto(user.Email!, "WrongPass1!"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokensAndClaims()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var mockSignInManager = CreateMockSignInManager(mockUserManager.Object);
        mockSignInManager
            .Setup(m => m.CheckPasswordSignInAsync(user, "Password1!", true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        var controller = CreateController(db, mockUserManager.Object, mockSignInManager.Object);

        var result = await controller.Login(new LoginRequestDto(user.Email!, "Password1!"));

        var dto = Assert.IsType<LoginResponseDto>(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(dto.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(dto.RefreshToken));
        Assert.Equal(user.Email, dto.User.Email);
        Assert.Single(db.RefreshTokens);
    }

    // ---- Refresh ----

    [Fact]
    public async Task Refresh_UnknownToken_ReturnsUnauthorized()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db);

        var result = await controller.Refresh(new RefreshRequestDto("does-not-exist"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Refresh_RevokedToken_ReturnsUnauthorized()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "revoked-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
            RevokedAtUtc = DateTime.UtcNow.AddMinutes(-1),
        };
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        var controller = CreateController(db);

        var result = await controller.Refresh(new RefreshRequestDto("revoked-token"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Refresh_InactiveUser_ReturnsUnauthorized()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser(UserStatus.Inactive);
        db.Users.Add(user);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "valid-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
        };
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.Refresh(new RefreshRequestDto("valid-token"));

        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task Refresh_ValidToken_RevokesOldAndReturnsNewTokens()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        db.Users.Add(user);
        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "valid-token",
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1),
        };
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.Refresh(new RefreshRequestDto("valid-token"));

        var dto = Assert.IsType<LoginResponseDto>(result.Value);
        Assert.False(string.IsNullOrWhiteSpace(dto.AccessToken));
        Assert.NotNull(refreshToken.RevokedAtUtc);
        Assert.Equal(2, db.RefreshTokens.Count());
    }

    // ---- ChangePassword ----

    private static void SetCurrentUser(ControllerBase controller, Guid userId)
    {
        var identity = new System.Security.Claims.ClaimsIdentity(
            new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId.ToString()) },
            "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal(identity) },
        };
    }

    [Fact]
    public async Task ChangePassword_NoSessionClaim_ReturnsUnauthorized()
    {
        var db = CreateInMemoryDb();
        var controller = CreateController(db);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await controller.ChangePassword(new ChangePasswordDto("Old1!", "New1!"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.ChangePasswordAsync(user, "Wrong1!", "New1!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password." }));

        var controller = CreateController(db, mockUserManager.Object);
        SetCurrentUser(controller, user.Id);

        var result = await controller.ChangePassword(new ChangePasswordDto("Wrong1!", "New1!"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ClearsMustChangePasswordFlag()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        user.MustChangePassword = true;
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);
        mockUserManager
            .Setup(m => m.ChangePasswordAsync(user, "Old1!", "New1!"))
            .ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(db, mockUserManager.Object);
        SetCurrentUser(controller, user.Id);

        var result = await controller.ChangePassword(new ChangePasswordDto("Old1!", "New1!"));

        Assert.IsType<OkObjectResult>(result);
        Assert.False(user.MustChangePassword);
        mockUserManager.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    // ---- ForgotPassword ----

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsGenericOk()
    {
        var db = CreateInMemoryDb();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync("nobody@alten.com")).ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.ForgotPassword(new ForgotPasswordDto("nobody@alten.com"));

        Assert.IsType<OkObjectResult>(result);
        mockUserManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_InactiveUser_DoesNotGenerateCode()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser(UserStatus.Inactive);
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.ForgotPassword(new ForgotPasswordDto(user.Email!));

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(user.PasswordResetCode);
        mockUserManager.Verify(m => m.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_ActiveUser_GeneratesCodeAndSendsEmail()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var mockEmailService = CreateMockEmailService();

        var controller = CreateController(db, mockUserManager.Object, emailService: mockEmailService.Object);

        var result = await controller.ForgotPassword(new ForgotPasswordDto(user.Email!));

        Assert.IsType<OkObjectResult>(result);
        Assert.False(string.IsNullOrEmpty(user.PasswordResetCode));
        Assert.NotNull(user.PasswordResetCodeExpiresAtUtc);
        mockEmailService.Verify(m => m.SendPasswordResetCodeEmailAsync(user.Email!, It.IsAny<string>(), user.PasswordResetCode!), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_EmailServiceThrows_StillReturnsGenericOk()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var mockEmailService = CreateMockEmailService();
        mockEmailService
            .Setup(m => m.SendPasswordResetCodeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("SMTP unreachable"));

        var controller = CreateController(db, mockUserManager.Object, emailService: mockEmailService.Object);

        var result = await controller.ForgotPassword(new ForgotPasswordDto(user.Email!));

        Assert.IsType<OkObjectResult>(result);
    }

    // ---- ResetPassword ----

    [Fact]
    public async Task ResetPassword_UnknownEmail_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync("nobody@alten.com")).ReturnsAsync((ApplicationUser?)null);

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.ResetPassword(new ResetPasswordWithCodeDto("nobody@alten.com", "123456", "New1!"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_ExpiredCode_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        user.PasswordResetCode = "123456";
        user.PasswordResetCodeExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.ResetPassword(new ResetPasswordWithCodeDto(user.Email!, "123456", "New1!"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_WrongCode_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        user.PasswordResetCode = "123456";
        user.PasswordResetCodeExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.ResetPassword(new ResetPasswordWithCodeDto(user.Email!, "999999", "New1!"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_IdentityFails_ReturnsBadRequest()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        user.PasswordResetCode = "123456";
        user.PasswordResetCodeExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        mockUserManager.Setup(m => m.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
        mockUserManager
            .Setup(m => m.AddPasswordAsync(user, "weak"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.ResetPassword(new ResetPasswordWithCodeDto(user.Email!, "123456", "weak"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_ValidCode_ResetsPasswordAndClearsCode()
    {
        var db = CreateInMemoryDb();
        var user = CreateUser();
        user.PasswordResetCode = "123456";
        user.PasswordResetCodeExpiresAtUtc = DateTime.UtcNow.AddMinutes(10);
        user.MustChangePassword = false;
        var mockUserManager = CreateMockUserManager();
        mockUserManager.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        mockUserManager.Setup(m => m.RemovePasswordAsync(user)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(m => m.AddPasswordAsync(user, "NewPass123!")).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var controller = CreateController(db, mockUserManager.Object);

        var result = await controller.ResetPassword(new ResetPasswordWithCodeDto(user.Email!, "123456", "NewPass123!"));

        Assert.IsType<OkObjectResult>(result);
        Assert.Null(user.PasswordResetCode);
        Assert.Null(user.PasswordResetCodeExpiresAtUtc);
    }
}
