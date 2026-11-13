using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WArena.Api.Dtos;
using WArena.Api.Models;
using WArena.Api.Repositories;
using WArena.Api.Services;

namespace WArena.Api.Controllers;

[ApiController, Route("api/questions"), Authorize]
public class QuestionsController(IQuestionRepository repo, ICategoryRepository categories, ILogger<QuestionsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuestionDto>>> Get([FromQuery] string? search, [FromQuery] int? categoryId, [FromQuery] Difficulty? difficulty, [FromQuery] int? minPoints, [FromQuery] int? maxPoints)
    {
        var questions = await repo.SearchAsync(new QuestionFilter(search, categoryId, difficulty, minPoints, maxPoints));
        return Ok(questions.Select(x => x.ToDto()));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<QuestionDto>> GetOne(int id)
    {
        var q = await repo.GetAsync(id);
        return q is null ? NotFound() : Ok(q.ToDto());
    }

    [HttpGet("{id:int}/manage"), Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<QuestionManageDto>> GetForManagement(int id)
    {
        var q = await repo.GetAsync(id);
        if (q is null) return NotFound();
        return Ok(new QuestionManageDto(
            q.Id, q.Title, q.Prompt, q.CorrectAnswer, q.Type, q.Difficulty, q.Points,
            q.CategoryId, q.OptionA, q.OptionB, q.OptionC, q.OptionD));
    }

    [HttpPost, Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<QuestionDto>> Create(QuestionUpsertDto dto)
    {
        if (!ValidateChoices(dto, out var error))
            return BadRequest(new { message = error });
        if (!await categories.ExistsAsync(dto.CategoryId))
            return BadRequest(new { message = "Category does not exist." });

        var q = Map(dto);
        await repo.AddAsync(q);
        logger.LogInformation("API question {Id} created", q.Id);
        var created = await repo.GetAsync(q.Id);
        return CreatedAtAction(nameof(GetOne), new { id = q.Id }, created!.ToDto());
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Update(int id, QuestionUpsertDto dto)
    {
        var q = await repo.GetAsync(id);
        if (q is null) return NotFound();
        if (!ValidateChoices(dto, out var error))
            return BadRequest(new { message = error });
        if (!await categories.ExistsAsync(dto.CategoryId))
            return BadRequest(new { message = "Category does not exist." });

        q.Title = dto.Title.Trim();
        q.Prompt = dto.Prompt.Trim();
        q.CorrectAnswer = dto.CorrectAnswer.Trim();
        q.Type = dto.Type;
        q.Difficulty = dto.Difficulty;
        q.Points = dto.Points;
        q.CategoryId = dto.CategoryId;
        q.OptionA = dto.OptionA?.Trim();
        q.OptionB = dto.OptionB?.Trim();
        q.OptionC = dto.OptionC?.Trim();
        q.OptionD = dto.OptionD?.Trim();
        await repo.SaveAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Delete(int id)
    {
        var q = await repo.GetAsync(id);
        if (q is null) return NotFound();
        await repo.DeleteAsync(q);
        return NoContent();
    }

    private static Question Map(QuestionUpsertDto d) => new()
    {
        Title = d.Title.Trim(),
        Prompt = d.Prompt.Trim(),
        CorrectAnswer = d.CorrectAnswer.Trim(),
        Type = d.Type,
        Difficulty = d.Difficulty,
        Points = d.Points,
        CategoryId = d.CategoryId,
        OptionA = d.OptionA?.Trim(),
        OptionB = d.OptionB?.Trim(),
        OptionC = d.OptionC?.Trim(),
        OptionD = d.OptionD?.Trim()
    };

    private static bool ValidateChoices(QuestionUpsertDto d, out string? error)
    {
        error = null;
        if (d.Type != QuestionType.MultipleChoice) return true;

        var options = new[] { d.OptionA, d.OptionB, d.OptionC, d.OptionD };
        if (options.Any(string.IsNullOrWhiteSpace))
        {
            error = "Multiple-choice questions need four options.";
            return false;
        }

        var correct = d.CorrectAnswer?.Trim() ?? string.Empty;
        if (!options.Any(x => string.Equals(x?.Trim(), correct, StringComparison.OrdinalIgnoreCase)))
        {
            error = "Correct answer must match one option.";
            return false;
        }

        return true;
    }
}