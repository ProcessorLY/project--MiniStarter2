#nullable disable
namespace API.Controllers;
public class AccountController : BaseApiController
{
    private readonly UserManager<User> _userManager;
    private readonly TokenService _tokenService;
    private readonly JwtSettings _jwtSettings;
    private readonly IStringLocalizer<AccountController> _localizer;

    public AccountController(UserManager<User> userManager, TokenService tokenService,
        IOptions<JwtSettings> jwtSettings, IStringLocalizer<AccountController> localizer)
    {
        _tokenService = tokenService;
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
        _localizer = localizer;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        var user = await _userManager.FindByNameAsync(loginDto.Username);
        if(!user.IsActive) return Unauthorized(new ProblemDetails { Title = _localizer["account.usernotactive"]});
        if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password)) return Unauthorized(new ProblemDetails { Title = _localizer["account.invalidcredentials"]});

        return Ok(await CreateUserObject(user, GenerateIPAddress()));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> RefreshAsync(RefreshTokenRequest request)
    {
        var userPrincipal = GetPrincipalFromExpiredToken(request.Token);
        string userEmail = userPrincipal.GetEmail();
        var user = await _userManager.FindByEmailAsync(userEmail);
        if (user is null) return Unauthorized(_localizer["auth.failed"]);
        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized(_localizer["account.invalidrefreshtoken"]);
        }

        return Ok(await CreateUserObject(user, GenerateIPAddress()));
    }

    [HttpPost("register")]
    public async Task<ActionResult> Register(RegisterDto registerDto)
    {
        var user = new User { UserName = registerDto.Username, Email = registerDto.Email };

        var result = await _userManager.CreateAsync(user, registerDto.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return ValidationProblem();
        }

        await _userManager.AddToRoleAsync(user, "Member");

        return StatusCode(201);
    }

    private string GenerateIPAddress()
    {
        if (Request.Headers.ContainsKey("X-Forwarded-For"))
            return Request.Headers["X-Forwarded-For"];
        else
            return HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString();
    }
    private async Task<UserDto> CreateUserObject(User user, string ipAddress)
    {
        var tokenResponse = await _tokenService.GenerateTokensAndUpdateUser(user, ipAddress);

        return new UserDto
        {
            UserName = user.UserName,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            ImageUrl = user.ImageUrl,
            IsActive = user.IsActive,
            Token = tokenResponse.Token,
            RefreshToken = tokenResponse.RefreshToken,
            RefreshTokenExpiryTime = tokenResponse.RefreshTokenExpiryTime
        };
    }
    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        if (string.IsNullOrEmpty(_jwtSettings.Key))
        {
            throw new InvalidOperationException("No Key defined in JwtSettings config.");
        }

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
            ValidateIssuer = false,
            ValidateAudience = false,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = false
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(
                SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
        {
            throw new Exception("account.invalidtoken");
        }

        return principal;
    }
}