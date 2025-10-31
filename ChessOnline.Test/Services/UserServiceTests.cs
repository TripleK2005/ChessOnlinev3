using System;
using System.Linq;
using System.Threading.Tasks;
using ChessOnline.Application.DTOs.Auth;
using ChessOnline.Domain.Entities;
using ChessOnline.Infrastructure.Persistence;
using ChessOnline.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ChessOnline.Test.Services
{
    public class UserServiceTests
    {
        private static AppDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        private static Mock<UserManager<User>> CreateMockUserManager()
        {
            var storeMock = new Mock<IUserStore<User>>();
            var options = Options.Create(new IdentityOptions());
            var passwordHasher = new Mock<IPasswordHasher<User>>().Object;
            var userValidators = new[] { new Mock<IUserValidator<User>>().Object };
            var pwdValidators = new[] { new Mock<IPasswordValidator<User>>().Object };
            var keyNormalizer = new Mock<ILookupNormalizer>().Object;
            var errors = new IdentityErrorDescriber();
            var services = new Mock<IServiceProvider>().Object;
            var logger = new Mock<ILogger<UserManager<User>>>().Object;

            return new Mock<UserManager<User>>(
                storeMock.Object,
                options,
                passwordHasher,
                userValidators,
                pwdValidators,
                keyNormalizer,
                errors,
                services,
                logger
            );
        }

        private static Mock<SignInManager<User>> CreateMockSignInManager(Mock<UserManager<User>> userManagerMock)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>().Object;
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>().Object;
            var options = Options.Create(new IdentityOptions());
            var logger = new Mock<ILogger<SignInManager<User>>>().Object;
            var schemes = new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().Object;
            var confirmation = new Mock<IUserConfirmation<User>>().Object;

            return new Mock<SignInManager<User>>(
                userManagerMock.Object,
                contextAccessor,
                claimsFactory,
                Options.Create(new IdentityOptions()),
                logger,
                schemes,
                confirmation
            );
        }

        [Fact]
        public async Task RegisterAsync_OnSuccess_ReturnsSucceededDto_And_SignsIn()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());

            var umMock = CreateMockUserManager();
            // when CreateAsync called, set Id on user and return success
            umMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                  .Callback<User, string>((u, p) => u.Id = "new-id")
                  .ReturnsAsync(IdentityResult.Success);

            var smMock = CreateMockSignInManager(umMock);
            smMock.Setup(s => s.SignInAsync(It.IsAny<User>(), false, null)).Returns(Task.CompletedTask);

            var svc = new UserService(umMock.Object, smMock.Object, ctx);

            var dto = new RegisterDto
            {
                UserName = "alice",
                Email = "a@a.com",
                NickName = "Alice",
                Password = "pw1234"
            };

            var result = await svc.RegisterAsync(dto);

            Assert.True(result.Succeeded);
            Assert.Equal("new-id", result.UserId);
            Assert.Equal("alice", result.UserName);
            Assert.Equal("Alice", result.NickName);

            umMock.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Once);
            smMock.Verify(s => s.SignInAsync(It.IsAny<User>(), false, null), Times.Once);
        }

        [Fact]
        public async Task RegisterAsync_OnFailure_ReturnsErrors_And_DoesNotSignIn()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());

            var umMock = CreateMockUserManager();
            var identityError = new IdentityError { Description = "bad" };
            umMock.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                  .ReturnsAsync(IdentityResult.Failed(identityError));

            var smMock = CreateMockSignInManager(umMock);
            var svc = new UserService(umMock.Object, smMock.Object, ctx);

            var dto = new RegisterDto
            {
                UserName = "bob",
                Email = "b@b.com",
                NickName = "Bob",
                Password = "pw"
            };

            var result = await svc.RegisterAsync(dto);

            Assert.False(result.Succeeded);
            Assert.Contains("bad", result.Errors);

            smMock.Verify(s => s.SignInAsync(It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task LoginAsync_OnSuccess_ReturnsAuthResult_WithUserInfo()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());

            var user = new User { Id = "u1", UserName = "charlie", NickName = "Charlie" };
            // add user to in-memory store so UserManager.Users returns it
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var umMock = CreateMockUserManager();
            umMock.SetupGet(m => m.Users).Returns(ctx.Users);

            var smMock = CreateMockSignInManager(umMock);
            smMock.Setup(s => s.PasswordSignInAsync("charlie", "pw", true, false)).ReturnsAsync(SignInResult.Success);

            var svc = new UserService(umMock.Object, smMock.Object, ctx);

            var dto = new LoginDto { UserName = "charlie", Password = "pw", RememberMe = true };
            var res = await svc.LoginAsync(dto);

            Assert.True(res.Succeeded);
            Assert.Equal("u1", res.UserId);
            Assert.Equal("charlie", res.UserName);
            Assert.Equal("Charlie", res.NickName);
        }

        [Fact]
        public async Task LoginAsync_OnFailure_ReturnsFailedWithMessage()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());

            var umMock = CreateMockUserManager();
            umMock.SetupGet(m => m.Users).Returns(new User[] { }.AsQueryable());

            var smMock = CreateMockSignInManager(umMock);
            smMock.Setup(s => s.PasswordSignInAsync("noone", "bad", false, false)).ReturnsAsync(SignInResult.Failed);

            var svc = new UserService(umMock.Object, smMock.Object, ctx);

            var dto = new LoginDto { UserName = "noone", Password = "bad", RememberMe = false };
            var res = await svc.LoginAsync(dto);

            Assert.False(res.Succeeded);
            Assert.Contains("Đăng nhập thất bại.", res.Errors);
        }

        [Fact]
        public async Task LogoutAsync_CallsSignOut()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());

            var umMock = CreateMockUserManager();
            var smMock = CreateMockSignInManager(umMock);
            smMock.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask).Verifiable();

            var svc = new UserService(umMock.Object, smMock.Object, ctx);

            await svc.LogoutAsync();

            smMock.Verify(s => s.SignOutAsync(), Times.Once);
        }

        [Fact]
        public async Task GetMyProfileAsync_ReturnsProfile_WhenUserExists()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());

            var user = new User
            {
                Id = "me",
                UserName = "meuser",
                NickName = "Me",
                Email = "me@x.com",
                EloRating = 1500,
                Wins = 3,
                Losses = 1,
                Draws = 1
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var umMock = CreateMockUserManager();
            var smMock = CreateMockSignInManager(umMock);

            var svc = new UserService(umMock.Object, smMock.Object, ctx);

            var profile = await svc.GetMyProfileAsync("me");
            Assert.NotNull(profile);
            Assert.Equal("me", profile.Id);
            Assert.Equal("meuser", profile.UserName);
            Assert.Equal("Me", profile.NickName);
            Assert.Equal("me@x.com", profile.Email);
            Assert.Equal(1500, profile.EloRating);
            Assert.Equal(3, profile.Wins);
            Assert.Equal(1, profile.Losses);
            Assert.Equal(1, profile.Draws);
        }

        [Fact]
        public async Task GetUserProfileByUsernameAsync_ReturnsProfile_WhenUserExists()
        {
            using var ctx = CreateInMemoryContext(Guid.NewGuid().ToString());

            var user = new User
            {
                Id = "uX",
                UserName = "target",
                NickName = "Target",
                Email = "t@x.com",
                EloRating = 1100,
                Wins = 0,
                Losses = 0,
                Draws = 0
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            var umMock = CreateMockUserManager();
            var smMock = CreateMockSignInManager(umMock);

            var svc = new UserService(umMock.Object, smMock.Object, ctx);

            var profile = await svc.GetUserProfileByUsernameAsync("target");
            Assert.NotNull(profile);
            Assert.Equal("uX", profile.Id);
            Assert.Equal("target", profile.UserName);
            Assert.Equal("Target", profile.NickName);
        }
    }
}
