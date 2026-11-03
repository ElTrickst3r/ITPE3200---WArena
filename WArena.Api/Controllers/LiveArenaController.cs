using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WArena.Api.Dtos;
using WArena.Api.Hubs;
using WArena.Api.Services;

namespace WArena.Api.Controllers;

[ApiController,Route("api/live"),Authorize]
public class LiveArenaController(ILiveArenaService live,IHubContext<LiveArenaHub> hub,ILogger<LiveArenaController> logger):ControllerBase
{
    [HttpPost("rooms"),Authorize(Roles="Admin,Teacher")]
    public async Task<ActionResult<LiveRoomDto>> Create(CreateLiveRoomDto dto)
    {
        try{var room=await live.CreateAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!,dto);logger.LogInformation("Live room {Code} created",room.Code);return Ok(await live.GetStateAsync(room.Code));}
        catch(KeyNotFoundException){return NotFound(new{message="Challenge set not found."});}
        catch(InvalidOperationException ex){return BadRequest(new{message=ex.Message});}
    }

    [HttpPost("rooms/{code}/join")]
    public async Task<ActionResult<LiveRoomDto>> Join(string code)
    {
        var room=await live.JoinAsync(code,User.FindFirstValue(ClaimTypes.NameIdentifier)!);if(room is null)return NotFound();var state=await live.GetStateAsync(code);await hub.Clients.Group(code.ToUpperInvariant()).SendAsync("StateChanged",state);return Ok(state);
    }

    [HttpGet("rooms/{code}")]
    public async Task<ActionResult<LiveRoomDto>> Get(string code){var state=await live.GetStateAsync(code);return state is null?NotFound():Ok(state);}

    [HttpPost("rooms/{code}/start"),Authorize(Roles="Admin,Teacher")]
    public async Task<ActionResult<LiveRoomDto>> Start(string code)
    {
        var room=await live.StartAsync(code,User.FindFirstValue(ClaimTypes.NameIdentifier)!);if(room is null)return Forbid();var state=await live.GetStateAsync(code);await hub.Clients.Group(code.ToUpperInvariant()).SendAsync("StateChanged",state);return Ok(state);
    }

    [HttpPost("rooms/{code}/answer")]
    public async Task<ActionResult> Answer(string code,LiveAnswerDto dto)
    {
        var result=await live.SubmitAnswerAsync(code,User.FindFirstValue(ClaimTypes.NameIdentifier)!,dto);if(result.Room is null)return NotFound();if(result.Error is not null)return BadRequest(new{message=result.Error});var state=await live.GetStateAsync(code);await hub.Clients.Group(code.ToUpperInvariant()).SendAsync("StateChanged",state);return Ok(new{score=result.Score,state});
    }

    [HttpPost("rooms/{code}/next"),Authorize(Roles="Admin,Teacher")]
    public async Task<ActionResult<LiveRoomDto>> Next(string code)
    {
        var room=await live.NextAsync(code,User.FindFirstValue(ClaimTypes.NameIdentifier)!);if(room is null)return Forbid();var state=await live.GetStateAsync(code);await hub.Clients.Group(code.ToUpperInvariant()).SendAsync("StateChanged",state);return Ok(state);
    }
}
