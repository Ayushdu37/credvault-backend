using System.Security.Claims;
using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize] // All endpoints here require a valid JWT token
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET /api/users/me — get the current authenticated user's profile
        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserIdFromToken();
            var profile = await _userService.GetProfileAsync(userId);
            return Ok(profile);
        }

        // PUT /api/users/me — update current user's name and phone
        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
        {
            var userId = GetUserIdFromToken();
            var profile = await _userService.UpdateProfileAsync(userId, request);
            return Ok(profile);
        }

        // Helper — extract UserId from the JWT token claims
        private Guid GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            return Guid.Parse(userIdClaim!);
        }
    }
}
