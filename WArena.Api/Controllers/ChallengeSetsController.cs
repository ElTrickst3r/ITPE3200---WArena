using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WArena.Api.Dtos;
using WArena.Api.Models;
using WArena.Api.Repositories;
using WArena.Api.Services;

namespace WArena.Api.Controllers;

[ApiController,Route("api/challenge-sets"),Authorize]
public class ChallengeSetsController(IChallengeSetRepository repo, IQuestionRepository questions, ILogger<ChallengeSetsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChallengeSetListDto>>> Get([FromQuery]string? search,[FromQuery]int? minQuestions,[FromQuery]bool? isPublished)
    {
        var manage=User.IsInRole("Admin")||User.IsInRole("Teacher");
        return Ok((await repo.SearchAsync(new SetFilter(search,minQuestions,isPublished),manage)).Select(x=>x.ToListDto()));
    }
    [HttpGet("{id:int}")] public async Task<ActionResult<ChallengeSetDetailsDto>> GetOne(int id){var s=await repo.GetAsync(id);if(s is null||(!s.IsPublished&&!User.IsInRole("Admin")&&!User.IsInRole("Teacher")))return NotFound();return Ok(s.ToDetailsDto());}
    [HttpPost, Authorize(Roles = "Admin,Teacher")]
public async Task<ActionResult> Create(ChallengeSetUpsertDto dto)
{
    if (!await questions.AllExistAsync(dto.QuestionIds))
        return BadRequest(new { message = "One or more questions do not exist." });

    var s = new ChallengeSet { Name = dto.Name.Trim(), Description = dto.Description?.Trim(), IsPublished = dto.IsPublished };
    await repo.AddAsync(s, dto.QuestionIds);
    logger.LogInformation("API set {Id} created", s.Id);
    return CreatedAtAction(nameof(GetOne), new { id = s.Id }, new { id = s.Id });
}

[HttpPut("{id:int}"), Authorize(Roles = "Admin,Teacher")]
public async Task<IActionResult> Update(int id, ChallengeSetUpsertDto dto)
{
    var s = await repo.GetAsync(id);
    if (s is null) return NotFound();
    if (!await questions.AllExistAsync(dto.QuestionIds))
        return BadRequest(new { message = "One or more questions do not exist." });

    s.Name = dto.Name.Trim();
    s.Description = dto.Description?.Trim();
    s.IsPublished = dto.IsPublished;
    await repo.UpdateAsync(s, dto.QuestionIds);
    return NoContent();
}
    [HttpDelete("{id:int}"),Authorize(Roles="Admin,Teacher")] public async Task<IActionResult> Delete(int id){var s=await repo.GetAsync(id);if(s is null)return NotFound();await repo.DeleteAsync(s);return NoContent();}
}
