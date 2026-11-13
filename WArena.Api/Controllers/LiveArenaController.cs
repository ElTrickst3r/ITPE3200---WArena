using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WArena.Api.Dtos;
using WArena.Api.Hubs;
using WArena.Api.Services;

namespace WArena.Api.Controllers;

[ApiController, Route("api/live"), Authorize]
public class LiveArenaController(ILiveArenaService live, IHubContext<LiveArenaHub> hub, ILogger<LiveArenaController> logger) : ControllerBase
{
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("rooms"), Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<LiveRoomDto>> Create(CreateLiveRoomDto dto)
    {
        try
        {
            var room = await live.CreateAsync(CurrentUserId, dto);
            logger.LogInformation("Live room {Code} created by {UserId} for challenge set {SetId}", room.Code, CurrentUserId, dto.ChallengeSetId);
            return Ok(await live.GetStateAsync(room.Code));
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Live room creation rejected: challenge set {SetId} not found", dto.ChallengeSetId);
            return NotFound(new { message = "Challenge set not found." });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Live room creation rejected for challenge set {SetId}: {Reason}", dto.ChallengeSetId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("rooms/{code}/join")]
    public async Task<ActionResult<LiveRoomDto>> Join(string code)
    {
        var room = await live.JoinAsync(code, CurrentUserId);
        if (room is null)
        {
            logger.LogWarning("User {UserId} could not join room {Code}: not found or completed", CurrentUserId, code);
            return NotFound();
        }

        logger.LogInformation("User {UserId} joined live room {Code}", CurrentUserId, room.Code);
        return Ok(await BroadcastStateAsync(code));
    }

    [HttpGet("rooms/{code}")]
    public async Task<ActionResult<LiveRoomDto>> Get(string code)
    {
        var state = await live.GetStateAsync(code);
        return state is null ? NotFound() : Ok(state);
    }

    [HttpPost("rooms/{code}/start"), Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<LiveRoomDto>> Start(string code)
    {
        var rejection = await CheckHostActionAsync(code, "start");
        if (rejection is not null) return rejection;

        var room = await live.StartAsync(code, CurrentUserId);
        if (room is null)
        {
            logger.LogWarning("Start of live room {Code} rejected: room is not waiting", code);
            return Conflict(new { message = "The room has already started or is completed." });
        }

        logger.LogInformation("Live room {Code} started", room.Code);
        return Ok(await BroadcastStateAsync(code));
    }

    [HttpPost("rooms/{code}/answer")]
    public async Task<ActionResult> Answer(string code, LiveAnswerDto dto)
    {
        var result = await live.SubmitAnswerAsync(code, CurrentUserId, dto);
        if (result.Room is null) return NotFound();
        if (result.Error is not null)
        {
            logger.LogWarning("Answer from user {UserId} in room {Code} rejected: {Reason}", CurrentUserId, code, result.Error);
            return BadRequest(new { message = result.Error });
        }

        logger.LogInformation("User {UserId} answered in live room {Code}. Score={Score}", CurrentUserId, result.Room.Code, result.Score);
        var state = await BroadcastStateAsync(code);
        return Ok(new { score = result.Score, state });
    }

    [HttpPost("rooms/{code}/next"), Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<LiveRoomDto>> Next(string code)
    {
        var rejection = await CheckHostActionAsync(code, "next");
        if (rejection is not null) return rejection;

        var room = await live.NextAsync(code, CurrentUserId);
        if (room is null)
        {
            logger.LogWarning("Next question in live room {Code} rejected: room is not running", code);
            return Conflict(new { message = "The room is not running." });
        }

        logger.LogInformation("Live room {Code} moved to question index {Index} (status {Status})", room.Code, room.CurrentQuestionIndex, room.Status);
        return Ok(await BroadcastStateAsync(code));
    }

    // Returns 404 if the room does not exist and 403 if the current user is not the host; otherwise null.
    private async Task<ActionResult?> CheckHostActionAsync(string code, string action)
    {
        var state = await live.GetStateAsync(code);
        if (state is null) return NotFound();
        if (state.HostUserId != CurrentUserId)
        {
            logger.LogWarning("User {UserId} tried to {Action} live room {Code} without being host", CurrentUserId, action, code);
            return Forbid();
        }
        return null;
    }

    // Sends the latest room state to everyone in the SignalR group and returns it.
    private async Task<LiveRoomDto?> BroadcastStateAsync(string code)
    {
        var state = await live.GetStateAsync(code);
        await hub.Clients.Group(code.ToUpperInvariant()).SendAsync("StateChanged", state);
        return state;
    }
}