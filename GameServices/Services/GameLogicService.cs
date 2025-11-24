using GameService.Data;
using Microsoft.EntityFrameworkCore;
using SharedModels;

namespace GameService.Services;

public class GameLogicService
{
    private readonly GameDbContext _context;
    private readonly Random _random = new();

    public GameLogicService(GameDbContext context)
    {
        _context = context;
    }

    public async Task<Partie> DemarrerNouvelleAventureAsync(int joueurId)
    {
        var joueur = await _context.Joueurs.FindAsync(joueurId);
        if (joueur is null)
        {
            throw new ArgumentException($"Aucun joueur avec l'id {joueurId}", nameof(joueurId));
        }

        var nombreSalles = _random.Next(1, 6);
        var donjon = new Donjon
        {
            Nom = $"Donjon-{Guid.NewGuid().ToString()[..6]}",
            Difficulte = _random.Next(1, 4)
        };

        var roomTypes = Enum.GetValues<RoomType>();
        for (var i = 0; i < nombreSalles; i++)
        {
            var type = roomTypes[_random.Next(roomTypes.Length)];
            var difficulte = _random.Next(1, 6);

            donjon.Salles.Add(new Salle
            {
                Type = type,
                Description = $"Salle {i + 1} - {type}",
                Difficulte = difficulte,
                PointsGagnes = _random.Next(5, 16),
                PointsPerdus = _random.Next(1, 11)
            });
        }

        _context.Donjons.Add(donjon);

        var partie = new Partie
        {
            JoueurId = joueurId,
            Donjon = donjon,
            IndexSalleCourante = 0,
            ScoreCourant = 0,
            ScoreFinal = 0,
            EstTerminee = false,
            EtatPartie = "EnCours"
        };

        _context.Parties.Add(partie);
        await _context.SaveChangesAsync();

        return await RecupererPartieAsync(partie.Id)
               ?? throw new InvalidOperationException("Impossible de charger la partie nouvellement créée.");
    }

    public async Task<Partie> AppliquerChoixAsync(int partieId, string choix)
    {
        var partie = await RecupererPartieAsync(partieId)
                     ?? throw new KeyNotFoundException($"Partie {partieId} introuvable");

        if (partie.EstTerminee)
        {
            return partie;
        }

        if (partie.Donjon?.Salles is null || partie.Donjon.Salles.Count == 0)
        {
            throw new InvalidOperationException("La partie n'a aucune salle à explorer.");
        }

        partie.IndexSalleCourante = Math.Clamp(partie.IndexSalleCourante, 0, partie.Donjon.Salles.Count - 1);
        var salleCourante = partie.Donjon.Salles[partie.IndexSalleCourante];

        var variation = choix?.Trim() switch
        {
            "Combattre" => Math.Max(1, salleCourante.PointsGagnes - salleCourante.Difficulte),
            "Fuir" => -Math.Max(1, salleCourante.PointsPerdus),
            "Fouiller" => Math.Max(1, salleCourante.PointsGagnes / 2),
            _ => throw new ArgumentException("Choix invalide. Utilisez Combattre, Fuir ou Fouiller", nameof(choix))
        };

        partie.ScoreCourant += variation;

        if (partie.ScoreCourant <= 0)
        {
            partie.EstTerminee = true;
            partie.EtatPartie = "Perdue";
        }
        else if (partie.IndexSalleCourante >= partie.Donjon.Salles.Count - 1)
        {
            partie.EstTerminee = true;
            partie.EtatPartie = "Terminee";
        }
        else
        {
            partie.IndexSalleCourante++;
        }

        if (partie.EstTerminee)
        {
            partie.ScoreFinal = partie.ScoreCourant;
            partie.DateFin = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        return partie;
    }

    public async Task<Partie?> RecupererPartieAsync(int partieId)
    {
        return await _context.Parties
            .Include(p => p.Donjon)!
                .ThenInclude(d => d.Salles)
            .Include(p => p.Joueur)
            .FirstOrDefaultAsync(p => p.Id == partieId);
    }
}
