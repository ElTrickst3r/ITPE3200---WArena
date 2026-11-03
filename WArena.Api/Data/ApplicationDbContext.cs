using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WArena.Api.Models;

namespace WArena.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<ChallengeSet> ChallengeSets => Set<ChallengeSet>();
    public DbSet<ChallengeSetQuestion> ChallengeSetQuestions => Set<ChallengeSetQuestion>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<LiveRoom> LiveRooms => Set<LiveRoom>();
    public DbSet<LiveParticipant> LiveParticipants => Set<LiveParticipant>();
    public DbSet<LiveAnswer> LiveAnswers => Set<LiveAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<ChallengeSetQuestion>().HasKey(x => new { x.ChallengeSetId, x.QuestionId });
        builder.Entity<ChallengeSetQuestion>().HasOne(x => x.ChallengeSet).WithMany(x => x.ChallengeSetQuestions).HasForeignKey(x => x.ChallengeSetId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ChallengeSetQuestion>().HasOne(x => x.Question).WithMany(x => x.ChallengeSetQuestions).HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UserAchievement>().HasKey(x => new { x.UserId, x.AchievementId });
        builder.Entity<Submission>().HasOne(x => x.User).WithMany(x => x.Submissions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<UserAchievement>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LiveParticipant>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<LiveRoom>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<LiveParticipant>().HasIndex(x => new { x.LiveRoomId, x.UserId }).IsUnique();
        builder.Entity<LiveAnswer>().HasIndex(x => new { x.LiveRoomId, x.QuestionId, x.UserId }).IsUnique();
    }
}
