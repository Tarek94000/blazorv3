using GameService.Services;
using Microsoft.AspNetCore.Mvc;
using SharedModels;

namespace GameService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly GameLogicService _gameLogicService;

    public GameController(GameLogicService gameLogicService)
    {
        _gameLogicService = gameLogicService;
    }

    [HttpPost("start")]
    public async Task<ActionResult<Partie>> DemarrerNouvelleAventure([FromQuery] int joueurId)
    {
        var partie = await _gameLogicService.DemarrerNouvelleAventureAsync(joueurId);
        return CreatedAtAction(nameof(ObtenirPartie), new { partieId = partie.Id }, partie);
    }

    [HttpGet("{partieId:int}")]
    public async Task<ActionResult<object>> ObtenirPartie(int partieId)
    {
        var partie = await _gameLogicService.RecupererPartieAsync(partieId);
        if (partie is null)
        {
            return NotFound();
        }

        var salleCourante = partie.Donjon?.Salles.Count > 0
            ? partie.Donjon.Salles[Math.Clamp(partie.IndexSalleCourante, 0, partie.Donjon.Salles.Count - 1)]
            : null;

        return Ok(new { partie, salleCourante });
    }

    [HttpPost("{partieId:int}/choice")]
    public async Task<ActionResult<Partie>> AppliquerChoix(int partieId, [FromBody] ChoixRequest request)
    {
        var partie = await _gameLogicService.AppliquerChoixAsync(partieId, request.Choix);
        return Ok(partie);
    }
}

public class ChoixRequest
{
    public string Choix { get; set; } = string.Empty;
}
