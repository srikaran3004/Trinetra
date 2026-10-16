using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Trinetra.API.DTOs;
using Trinetra.API.Services;
using Trinetra.Core.Entities;
using Trinetra.Core.Enums;
using Trinetra.Infrastructure.Auth;
using Trinetra.Infrastructure.Data;
using Xunit;

namespace Trinetra.UnitTests.Auth;

public class AuthServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();

        _mockConfig = new Mock<IConfiguration>();
        _mockConfig.Setup(c => c["Jwt:Key"]).Returns("very_secret_unit_test_key_minimum_32_characters_long!");
        _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("Trinetra.API");
        _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("Trinetra.App");
        _mockConfig.Setup(c => c["Jwt:AccessTokenExpirationMinutes"]).Returns("15");
        _mockConfig.Setup(c => c["Jwt:RefreshTokenExpirationDays"]).Returns("7");

        _passwordHasher = new PasswordHasher();
        _tokenGenerator = new JwtTokenGenerator(_mockConfig.Object);
        _authService = new AuthService(_dbContext, _tokenGenerator, _passwordHasher, _mockConfig.Object);
    }

    [Fact]
    public void PasswordHasher_ShouldHashAndVerify_Successfully()
    {
        var rawPassword = "SecurePassword123!";
        var hash = _passwordHasher.HashPassword(rawPassword);

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(rawPassword);

        _passwordHasher.VerifyPassword(hash, rawPassword).Should().BeTrue();
        _passwordHasher.VerifyPassword(hash, "WrongPassword").Should().BeFalse();
    }

    [Fact]
    public void JwtTokenGenerator_ShouldGenerateToken_WithCorrectClaims()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "sre_lead",
            Email = "lead@trinetra.local",
            Role = UserRoles.Lead
        };

        var (token, expiresAt) = _tokenGenerator.GenerateAccessToken(user);

        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTime.UtcNow);
        expiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(15), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokens()
    {
        var password = "AdminPassword!";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "testadmin",
            Email = "testadmin@trinetra.local",
            PasswordHash = _passwordHasher.HashPassword(password),
            Role = UserRoles.Admin,
            IsActive = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "testadmin",
            Password = password
        };

        var response = await _authService.LoginAsync(request);

        response.Should().NotBeNull();
        response!.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.RefreshToken.Should().NotBeNullOrWhiteSpace();
        response.User.Username.Should().Be("testadmin");
        response.User.Role.Should().Be("admin");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnNull()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "user2",
            Email = "user2@trinetra.local",
            PasswordHash = _passwordHasher.HashPassword("RealPass!"),
            Role = UserRoles.Viewer,
            IsActive = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new LoginRequest
        {
            Username = "user2",
            Password = "WrongPassword!"
        };

        var response = await _authService.LoginAsync(request);

        response.Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ShouldRotateToken()
    {
        var existingRefreshToken = "existing_valid_refresh_token_123";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "sre1",
            Email = "sre1@trinetra.local",
            PasswordHash = "hash",
            Role = UserRoles.Sre,
            RefreshToken = existingRefreshToken,
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(3),
            IsActive = true
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var request = new RefreshTokenRequest
        {
            RefreshToken = existingRefreshToken
        };

        var response = await _authService.RefreshTokenAsync(request);

        response.Should().NotBeNull();
        response!.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.RefreshToken.Should().NotBeNullOrWhiteSpace();
        response.RefreshToken.Should().NotBe(existingRefreshToken); // Token rotated
    }
}
