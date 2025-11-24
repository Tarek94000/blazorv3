using GameService.Data;
using GameService.Services;
using Microsoft.EntityFrameworkCore;
using SharedModels;
using Xunit;

namespace Tests;

public class AdventureTests
{
    [Fact]
    public async Task Dungeon_Should_Have_At_Most_5_Rooms()
    {
        var service = CreateService(out _);
        var partie = await service.DemarrerNouvelleAventureAsync(1);

        Assert.NotNull(partie.Donjon);
        Assert.InRange(partie.Donjon!.Salles.Count, 1, 5);
        Assert.Equal(0, partie.IndexSalleCourante);
        Assert.False(partie.EstTerminee);
    }

    [Theory]
    [InlineData(RoomType.Enemy)]
    [InlineData(RoomType.Chest)]
    [InlineData(RoomType.Trap)]
    public void Room_Type_Should_Be_Valid(RoomType t)
    {
        Assert.Contains(t, Enum.GetValues<RoomType>());
    }

    [Fact]
    public async Task Combat_Action_Should_Affect_Score()
    {
        var service = CreateService(out _);
        var partie = await service.DemarrerNouvelleAventureAsync(1);

        var before = partie.ScoreCourant;
        var updated = await service.AppliquerChoixAsync(partie.Id, "Combattre");

        Assert.NotEqual(before, updated.ScoreCourant);
    }

    [Fact]
    public async Task Game_Should_End_On_Death_Or_After_N_Rooms()
    {
        var service = CreateService(out var context);
        var partie = await service.DemarrerNouvelleAventureAsync(1);

        // Force heavy penalty on the first room to simulate death
        var salle = context.Salles.First();
        salle.PointsPerdus = 20;
        partie.ScoreCourant = 1;
        partie.IndexSalleCourante = 0;
        await context.SaveChangesAsync();

        var updated = await service.AppliquerChoixAsync(partie.Id, "Fuir");

        Assert.True(updated.EstTerminee);
        Assert.Equal("Perdue", updated.EtatPartie);

        // Prepare another party to reach the last room
        var partie2 = await service.DemarrerNouvelleAventureAsync(1);
        partie2.ScoreCourant = 10;
        partie2.IndexSalleCourante = partie2.Donjon!.Salles.Count - 1;
        await context.SaveChangesAsync();

        var finished = await service.AppliquerChoixAsync(partie2.Id, "Fouiller");

        Assert.True(finished.EstTerminee);
        Assert.Equal("Terminee", finished.EtatPartie);
    }

    private static GameLogicService CreateService(out GameDbContext context)
    {
        var options = new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new GameDbContext(options);
        context.Joueurs.Add(new Joueur { Id = 1, Nom = "Testeur" });
        context.SaveChanges();

        return new GameLogicService(context);
    }
}
