using Microsoft.EntityFrameworkCore;
using WArena.Api.Data;
using WArena.Api.Models;

namespace WArena.Api.Repositories;

public interface ILiveArenaRepository
{
    Task<LiveRoom?> GetByCodeAsync(string code);
    Task<LiveRoom?> GetByCodeWithStateAsync(string code);
    Task AddRoomAsync(LiveRoom room);
    Task AddParticipantAsync(LiveParticipant participant);
    Task<bool> HasParticipantAsync(int roomId, string userId);
    Task<bool> HasAnsweredAsync(int roomId, int questionId, string userId);
    Task AddAnswerAsync(LiveAnswer answer);
    Task SaveAsync();
}

public class LiveArenaRepository(ApplicationDbContext db) : ILiveArenaRepository
{
    public Task<LiveRoom?> GetByCodeAsync(string code) =>
        db.LiveRooms.FirstOrDefaultAsync(x => x.Code == code);

    public Task<LiveRoom?> GetByCodeWithStateAsync(string code) =>
        db.LiveRooms
            .Include(x => x.ChallengeSet).ThenInclude(x => x!.ChallengeSetQuestions.OrderBy(y => y.Order)).ThenInclude(x => x.Question).ThenInclude(x => x!.Category)
            .Include(x => x.Participants).ThenInclude(x => x.User)
            .Include(x => x.Answers)
            .FirstOrDefaultAsync(x => x.Code == code);

    public async Task AddRoomAsync(LiveRoom room)
    {
        db.LiveRooms.Add(room);
        await db.SaveChangesAsync();
    }

    public async Task AddParticipantAsync(LiveParticipant participant)
    {
        db.LiveParticipants.Add(participant);
        await db.SaveChangesAsync();
    }

    public Task<bool> HasParticipantAsync(int roomId, string userId) =>
        db.LiveParticipants.AnyAsync(x => x.LiveRoomId == roomId && x.UserId == userId);

    public Task<bool> HasAnsweredAsync(int roomId, int questionId, string userId) =>
        db.LiveAnswers.AnyAsync(x => x.LiveRoomId == roomId && x.QuestionId == questionId && x.UserId == userId);

    public async Task AddAnswerAsync(LiveAnswer answer)
    {
        db.LiveAnswers.Add(answer);
        await db.SaveChangesAsync();
    }

    public Task SaveAsync() => db.SaveChangesAsync();
}
