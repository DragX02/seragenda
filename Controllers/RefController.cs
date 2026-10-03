using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using seragenda.Models;
using System.Security.Claims;

namespace seragenda.Controllers
{
    [Route("api/ref")]
    [ApiController]
    [Authorize]
    public class RefController : ControllerBase
    {
        private readonly AgendaContext _context;

        public RefController(AgendaContext context)
        {
            _context = context;
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.CategorieCours
                .OrderBy(c => c.Ordre)
                .Select(c => new { c.IdCat, c.NomCat, c.Ordre })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("cours/{idCat:int}")]
        public async Task<IActionResult> GetCours(int idCat)
        {
            var cours = await _context.Cours
                .Where(c => c.IdCatFk == idCat)
                .OrderBy(c => c.NomCours)
                .Select(c => new { c.CodeCours, c.NomCours, c.CouleurAgenda })
                .ToListAsync();

            return Ok(cours);
        }

        [HttpGet("niveaux/{codeCours}")]
        public async Task<IActionResult> GetNiveaux(string codeCours)
        {
            var niveaux = await _context.CoursNiveaus
                .Where(cn => cn.IdCoursFkNavigation.CodeCours == codeCours)
                .Select(cn => cn.IdNiveauFkNavigation)
                .Distinct()
                .OrderBy(n => n.CodeNiveau)
                .Select(n => new { n.CodeNiveau, n.NomNiveau })
                .ToListAsync();

            return Ok(niveaux);
        }

        [HttpGet("niveaux")]
        public async Task<IActionResult> GetNiveauxTous()
        {
            var moi = await GetUserId() ?? 0;

            var niveaux = await _context.CoursNiveaus
                .Where(cn => cn.Domaines.Any(d => d.Visees.Any()) || cn.IdProfFk == moi)
                .Select(cn => cn.IdNiveauFkNavigation)
                .Distinct()
                .OrderBy(n => n.CodeNiveau)
                .Select(n => new { n.CodeNiveau, n.NomNiveau })
                .ToListAsync();

            return Ok(niveaux);
        }

        [HttpGet("categories/by-niveau/{codeNiveau}")]
        public async Task<IActionResult> GetCategoriesByNiveau(string codeNiveau)
        {
            var moi = await GetUserId() ?? 0;

            var categories = await _context.CategorieCours
                .Where(cat => cat.Cours.Any(co => co.CoursNiveaus.Any(cn =>
                    cn.IdNiveauFkNavigation.CodeNiveau == codeNiveau &&
                    (cn.Domaines.Any(d => d.Visees.Any()) || cn.IdProfFk == moi))))
                .OrderBy(cat => cat.Ordre)
                .Select(cat => new { cat.IdCat, cat.NomCat, cat.Ordre })
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("cours/by-cat-niveau/{idCat:int}/{codeNiveau}")]
        public async Task<IActionResult> GetCoursByCatNiveau(int idCat, string codeNiveau)
        {
            var moi = await GetUserId() ?? 0;

            var cours = await _context.Cours
                .Where(c => c.IdCatFk == idCat &&
                    c.CoursNiveaus.Any(cn =>
                        cn.IdNiveauFkNavigation.CodeNiveau == codeNiveau &&
                        (cn.Domaines.Any(d => d.Visees.Any()) || cn.IdProfFk == moi)))
                .OrderBy(c => c.NomCours)
                .Select(c => new { c.CodeCours, c.NomCours, c.CouleurAgenda })
                .ToListAsync();

            return Ok(cours);
        }

        [HttpGet("cours/by-niveau/{codeNiveau}")]
        public async Task<IActionResult> GetCoursByNiveau(string codeNiveau)
        {
            var moi = await GetUserId() ?? 0;

            var cours = await _context.Cours
                .Where(c => c.CoursNiveaus.Any(cn =>
                    cn.IdNiveauFkNavigation.CodeNiveau == codeNiveau &&
                    (cn.Domaines.Any(d => d.Visees.Any()) || cn.IdProfFk == moi)))
                .OrderBy(c => c.NomCours)
                .Select(c => new { c.CodeCours, c.NomCours, c.CouleurAgenda })
                .ToListAsync();

            return Ok(cours);
        }

        [HttpGet("domaines/{codeCours}/{codeNiveau}")]
        public async Task<IActionResult> GetDomaines(string codeCours, string codeNiveau)
        {
            var moi = await GetUserId() ?? 0;

            var domaines = await _context.CoursNiveaus
                .Where(cn =>
                    cn.IdCoursFkNavigation.CodeCours   == codeCours &&
                    cn.IdNiveauFkNavigation.CodeNiveau == codeNiveau)
                .SelectMany(cn => cn.Domaines)
                .Where(d => d.Visees.Any() || d.IdCoursNiveauFkNavigation.IdProfFk == moi)
                .OrderBy(d => d.Nom)
                .Select(d => new { d.IdDom, d.Nom })
                .ToListAsync();

            return Ok(domaines);
        }

        [HttpGet("sous-domaines/{idDomaine:int}")]
        public async Task<IActionResult> GetSousDomaines(int idDomaine)
        {
            var list = await _context.Sousdomaines
                .Where(s => s.IdDomFk == idDomaine)
                .OrderBy(s => s.NomComp)
                .Select(s => new { s.IdSousDomaine, s.NomComp })
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("visees/{idDomaine:int}")]
        public async Task<IActionResult> GetVisees(int idDomaine, [FromQuery] int? sousDomaine)
        {
            var query = _context.Visees
                .Include(v => v.IdNomViseeFkNavigation)
                .Include(v => v.IdCompFkNavigation)
                .Where(v => v.IdDomaineFk == idDomaine);

            if (sousDomaine.HasValue && sousDomaine.Value > 0)
                query = query.Where(v => v.IdSousDomaineFk == sousDomaine.Value);

            var list = await query
                .OrderBy(v => v.IdNomViseeFkNavigation.NomVisee1)
                .ThenBy(v => v.IdCompFkNavigation.NomCompetence)
                .Select(v => new
                {
                    v.IdVisee,
                    IdNomVisee    = v.IdNomViseeFk,
                    NomVisee      = v.IdNomViseeFkNavigation.NomVisee1,
                    IdCompetence  = v.IdCompFk,
                    NomCompetence = v.IdCompFkNavigation.NomCompetence,
                    Label         = v.IdNomViseeFkNavigation.NomVisee1 + " — " + v.IdCompFkNavigation.NomCompetence
                })
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("visees-maitriser/{idVisee:int}")]
        public async Task<IActionResult> GetViseesMaitriser(int idVisee)
        {
            var visee = await _context.Visees
                .Include(v => v.IdViseesMaitriserFks)
                .FirstOrDefaultAsync(v => v.IdVisee == idVisee);

            if (visee == null) return NotFound();

            var list = visee.IdViseesMaitriserFks
                .OrderBy(vm => vm.NomViseesMaitriser)
                .Select(vm => new { vm.IdViseesMaitriser, vm.NomViseesMaitriser })
                .ToList();

            return Ok(list);
        }

        [HttpGet("visees-maitriser/par-visees")]
        public async Task<IActionResult> GetViseesMaitriserParVisees([FromQuery] int[] ids)
        {
            if (ids == null || ids.Length == 0) return Ok(Array.Empty<object>());

            var list = await _context.Visees
                .Where(v => ids.Contains(v.IdVisee))
                .SelectMany(v => v.IdViseesMaitriserFks)
                .Distinct()
                .OrderBy(vm => vm.NomViseesMaitriser)
                .Select(vm => new { vm.IdViseesMaitriser, vm.NomViseesMaitriser })
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("appartenir/{idVm:int}")]
        public async Task<IActionResult> GetAppartenir(int idVm, [FromQuery] int? idVisee)
        {
            var list = await _context.AppartenirViseeAptitudes
                .Include(a => a.IdAptitudeFkNavigation)
                .Include(a => a.IdCompetenceFkNavigation)
                .Where(a => a.IdViseesMaitriserFk == idVm)
                .OrderBy(a => a.IdAptitudeFkNavigation != null ? a.IdAptitudeFkNavigation.NomAptitude : "")
                .Select(a => new
                {
                    a.IdAppartenirViseeAptitude,
                    IdAptitude    = a.IdAptitudeFk,
                    NomAptitude   = a.IdAptitudeFkNavigation != null ? a.IdAptitudeFkNavigation.NomAptitude : null,
                    a.IdCompetenceFk,
                    NomCompetence = a.IdCompetenceFkNavigation.NomCompetence
                })
                .ToListAsync();

            if (!list.Any() && idVisee.HasValue)
            {
                var visee = await _context.Visees
                    .Include(v => v.IdCompFkNavigation)
                    .FirstOrDefaultAsync(v => v.IdVisee == idVisee.Value);

                if (visee != null)
                {
                    var comp = visee.IdCompFkNavigation;
                    return Ok(new[] { new
                    {
                        IdAppartenirViseeAptitude = -comp.IdCompetence,
                        IdAptitude                = (int?)null,
                        NomAptitude               = (string?)null,
                        IdCompetenceFk            = comp.IdCompetence,
                        NomCompetence             = comp.NomCompetence
                    }});
                }
            }

            return Ok(list);
        }

        [HttpGet("competences")]
        public async Task<IActionResult> GetToutesCompetences()
        {
            var list = await _context.Competences
                .OrderBy(c => c.NomCompetence)
                .Select(c => new { c.IdCompetence, c.NomCompetence })
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("nom-visees")]
        public async Task<IActionResult> GetTousNomVisees()
        {
            var list = await _context.NomVisees
                .OrderBy(nv => nv.NomVisee1)
                .Select(nv => new { nv.IdNomVisee, NomVisee = nv.NomVisee1 })
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("visees-maitriser")]
        public async Task<IActionResult> GetToutesViseesMaitriser()
        {
            var list = await _context.ViseesMaitrisers
                .OrderBy(vm => vm.NomViseesMaitriser)
                .Select(vm => new { vm.IdViseesMaitriser, vm.NomViseesMaitriser })
                .ToListAsync();

            return Ok(list);
        }

        private async Task<int?> GetUserId()
        {
            var email = User.FindFirst(ClaimTypes.Name)?.Value;
            if (email == null) return null;
            var user = await _context.Utilisateurs.FirstOrDefaultAsync(u => u.Email == email);
            return user?.IdUser;
        }

        [HttpPost("domaines")]
        public async Task<IActionResult> CreerDomaine([FromBody] CreerDomaineDto dto)
        {
            var nom = (dto.Nom ?? "").Trim();
            if (nom.Length == 0) return BadRequest(new { message = "Le nom du champ est obligatoire." });

            var userId = await GetUserId();
            if (userId == null) return Unauthorized();

            var cours = await _context.Cours.FirstOrDefaultAsync(c => c.CodeCours == dto.CodeCours);
            if (cours == null) return BadRequest(new { message = "Cours introuvable." });

            var niveau = await _context.Niveaus.FirstOrDefaultAsync(n => n.CodeNiveau == dto.CodeNiveau);
            if (niveau == null) return BadRequest(new { message = "Année introuvable." });

            var coursNiveau = await _context.CoursNiveaus.FirstOrDefaultAsync(
                cn => cn.IdCoursFk == cours.IdCours
                   && cn.IdNiveauFk == niveau.IdNiveau
                   && cn.IdProfFk == userId.Value);

            if (coursNiveau == null)
            {
                coursNiveau = new CoursNiveau
                {
                    IdCoursFk  = cours.IdCours,
                    IdNiveauFk = niveau.IdNiveau,
                    IdProfFk   = userId.Value
                };
                _context.CoursNiveaus.Add(coursNiveau);
                try { await _context.SaveChangesAsync(); }
                catch { return BadRequest(new { message = "Erreur lors de la création de la liaison cours/année." }); }
            }

            var existant = await _context.Domaines.FirstOrDefaultAsync(
                d => d.Nom.ToLower() == nom.ToLower()
                  && d.IdCoursNiveauFkNavigation.IdCoursFk == cours.IdCours
                  && d.IdCoursNiveauFkNavigation.IdNiveauFk == niveau.IdNiveau);

            if (existant != null) return Ok(new { existant.IdDom, existant.Nom, Creee = false });

            var domaine = new Domaine { Nom = nom, IdCoursNiveauFk = coursNiveau.IdCoursNiveau };
            _context.Domaines.Add(domaine);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création du champ." }); }

            return Ok(new { domaine.IdDom, domaine.Nom, Creee = true });
        }

        [HttpPost("sous-domaines")]
        public async Task<IActionResult> CreerSousDomaine([FromBody] CreerSousDomaineDto dto)
        {
            var nom = (dto.Nom ?? "").Trim();
            if (nom.Length == 0) return BadRequest(new { message = "Le nom du domaine est obligatoire." });

            if (!await _context.Domaines.AnyAsync(d => d.IdDom == dto.IdDomaine))
                return BadRequest(new { message = "Champ introuvable." });

            var existant = await _context.Sousdomaines.FirstOrDefaultAsync(
                sd => sd.IdDomFk == dto.IdDomaine && sd.NomComp.ToLower() == nom.ToLower());

            if (existant != null) return Ok(new { existant.IdSousDomaine, Creee = false });

            var sousDomaine = new Sousdomaine { NomComp = nom, IdDomFk = dto.IdDomaine };
            _context.Sousdomaines.Add(sousDomaine);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création du domaine." }); }

            return Ok(new { sousDomaine.IdSousDomaine, Creee = true });
        }

        [HttpPost("competences")]
        public async Task<IActionResult> CreerCompetence([FromBody] CreerNommeDto dto)
        {
            var nom = (dto.Nom ?? "").Trim();
            if (nom.Length == 0) return BadRequest(new { message = "Le nom est obligatoire." });

            var existante = await _context.Competences
                .FirstOrDefaultAsync(c => c.NomCompetence.ToLower() == nom.ToLower());

            if (existante != null) return Ok(new { existante.IdCompetence, Creee = false });

            var competence = new Competence { NomCompetence = nom };
            _context.Competences.Add(competence);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création de la compétence." }); }

            return Ok(new { competence.IdCompetence, Creee = true });
        }

        [HttpPost("nom-visees")]
        public async Task<IActionResult> CreerNomVisee([FromBody] CreerNommeDto dto)
        {
            var nom = (dto.Nom ?? "").Trim();
            if (nom.Length == 0) return BadRequest(new { message = "Le nom est obligatoire." });

            var existant = await _context.NomVisees
                .FirstOrDefaultAsync(nv => nv.NomVisee1.ToLower() == nom.ToLower());

            if (existant != null) return Ok(new { existant.IdNomVisee, Creee = false });

            var nomVisee = new NomVisee { NomVisee1 = nom };
            _context.NomVisees.Add(nomVisee);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création de l'intitulé de visée." }); }

            return Ok(new { nomVisee.IdNomVisee, Creee = true });
        }

        [HttpPost("visees-maitriser")]
        public async Task<IActionResult> CreerViseeMaitriser([FromBody] CreerNommeDto dto)
        {
            var nom = (dto.Nom ?? "").Trim();
            if (nom.Length == 0) return BadRequest(new { message = "Le nom est obligatoire." });

            var existante = await _context.ViseesMaitrisers
                .FirstOrDefaultAsync(vm => vm.NomViseesMaitriser.ToLower() == nom.ToLower());

            if (existante != null) return Ok(new { existante.IdViseesMaitriser, Creee = false });

            var vm2 = new ViseesMaitriser { NomViseesMaitriser = nom };
            _context.ViseesMaitrisers.Add(vm2);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création de la visée à maîtriser." }); }

            return Ok(new { vm2.IdViseesMaitriser, Creee = true });
        }

        [HttpPost("visees")]
        public async Task<IActionResult> CreerVisee([FromBody] CreerViseeDto dto)
        {
            if (dto.IdDomaine <= 0 || dto.IdNomVisee <= 0 || dto.IdCompetence <= 0)
                return BadRequest(new { message = "Champ, visée et compétence sont obligatoires." });

            int? idSousDomaine = dto.IdSousDomaine > 0 ? dto.IdSousDomaine : null;

            if (!await _context.Domaines.AnyAsync(d => d.IdDom == dto.IdDomaine))
                return BadRequest(new { message = "Champ introuvable." });
            if (!await _context.NomVisees.AnyAsync(nv => nv.IdNomVisee == dto.IdNomVisee))
                return BadRequest(new { message = "Intitulé de visée introuvable." });
            if (!await _context.Competences.AnyAsync(c => c.IdCompetence == dto.IdCompetence))
                return BadRequest(new { message = "Compétence introuvable." });
            if (idSousDomaine != null &&
                !await _context.Sousdomaines.AnyAsync(sd => sd.IdSousDomaine == idSousDomaine))
                return BadRequest(new { message = "Domaine introuvable." });

            var existante = await _context.Visees.FirstOrDefaultAsync(
                v => v.IdDomaineFk     == dto.IdDomaine
                  && v.IdSousDomaineFk == idSousDomaine
                  && v.IdNomViseeFk    == dto.IdNomVisee
                  && v.IdCompFk        == dto.IdCompetence);

            if (existante != null) return Ok(new { existante.IdVisee, Creee = false });

            var visee = new Visee
            {
                IdDomaineFk     = dto.IdDomaine,
                IdSousDomaineFk = idSousDomaine,
                IdNomViseeFk    = dto.IdNomVisee,
                IdCompFk        = dto.IdCompetence
            };

            _context.Visees.Add(visee);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création de la visée." }); }

            return Ok(new { visee.IdVisee, Creee = true });
        }

        [HttpPost("lien-visee-maitrise")]
        public async Task<IActionResult> CreerLienViseeMaitrise([FromBody] CreerLienDto dto)
        {
            var visee = await _context.Visees
                .Include(v => v.IdViseesMaitriserFks)
                .FirstOrDefaultAsync(v => v.IdVisee == dto.IdVisee);

            if (visee == null) return NotFound(new { message = "Visée introuvable." });

            if (visee.IdViseesMaitriserFks.Any(vm => vm.IdViseesMaitriser == dto.IdViseesMaitriser))
                return Ok(new { Creee = false });

            var vm = await _context.ViseesMaitrisers
                .FirstOrDefaultAsync(x => x.IdViseesMaitriser == dto.IdViseesMaitriser);

            if (vm == null) return NotFound(new { message = "Visée à maîtriser introuvable." });

            visee.IdViseesMaitriserFks.Add(vm);
            try { await _context.SaveChangesAsync(); }
            catch { return BadRequest(new { message = "Erreur lors de la création du lien." }); }

            return Ok(new { Creee = true });
        }

        [HttpPost("selection")]
        public async Task<IActionResult> EnregistrerSelection([FromBody] SelectionDto dto)
        {
            if (dto.IdDomaine <= 0 || dto.IdCompetence <= 0)
                return BadRequest(new { message = "Champ et compétence sont obligatoires." });

            var idNomVisees = (dto.IdNomVisees ?? new()).Where(id => id > 0).Distinct().ToList();
            var idVms       = (dto.IdsViseesMaitriser ?? new()).Where(id => id > 0).Distinct().ToList();

            if (idNomVisees.Count == 0)
                return BadRequest(new { message = "Aucune visée retenue." });

            int? idSousDomaine = dto.IdSousDomaine > 0 ? dto.IdSousDomaine : null;

            if (!await _context.Domaines.AnyAsync(d => d.IdDom == dto.IdDomaine))
                return BadRequest(new { message = "Champ introuvable." });
            if (!await _context.Competences.AnyAsync(c => c.IdCompetence == dto.IdCompetence))
                return BadRequest(new { message = "Compétence introuvable." });
            if (idSousDomaine != null &&
                !await _context.Sousdomaines.AnyAsync(sd => sd.IdSousDomaine == idSousDomaine))
                return BadRequest(new { message = "Domaine introuvable." });

            var nomViseesConnus = await _context.NomVisees
                .Where(nv => idNomVisees.Contains(nv.IdNomVisee))
                .Select(nv => nv.IdNomVisee)
                .ToListAsync();

            if (nomViseesConnus.Count != idNomVisees.Count)
                return BadRequest(new { message = "Intitulé de visée introuvable." });

            if (idVms.Count > 0)
            {
                var vmsConnues = await _context.ViseesMaitrisers
                    .Where(vm => idVms.Contains(vm.IdViseesMaitriser))
                    .Select(vm => vm.IdViseesMaitriser)
                    .ToListAsync();

                if (vmsConnues.Count != idVms.Count)
                    return BadRequest(new { message = "Visée à maîtriser introuvable." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existantes = await _context.Visees
                    .Where(v => v.IdDomaineFk     == dto.IdDomaine
                             && v.IdSousDomaineFk == idSousDomaine
                             && v.IdCompFk        == dto.IdCompetence
                             && idNomVisees.Contains(v.IdNomViseeFk))
                    .ToListAsync();

                var parNomVisee = existantes.ToDictionary(v => v.IdNomViseeFk);
                int creees = 0;

                foreach (var idNomVisee in idNomVisees)
                {
                    if (parNomVisee.ContainsKey(idNomVisee)) continue;

                    var visee = new Visee
                    {
                        IdDomaineFk     = dto.IdDomaine,
                        IdSousDomaineFk = idSousDomaine,
                        IdNomViseeFk    = idNomVisee,
                        IdCompFk        = dto.IdCompetence
                    };

                    _context.Visees.Add(visee);
                    parNomVisee[idNomVisee] = visee;
                    creees++;
                }

                if (creees > 0) await _context.SaveChangesAsync();

                var idVisees = idNomVisees.Select(id => parNomVisee[id].IdVisee).ToList();

                int liens = 0;

                if (idVms.Count > 0 && idVisees.Count > 0)
                {
                    var porteuse = await _context.Visees
                        .Include(v => v.IdViseesMaitriserFks)
                        .FirstAsync(v => v.IdVisee == idVisees[0]);

                    foreach (var idVm in idVms)
                    {
                        if (porteuse.IdViseesMaitriserFks.Any(vm => vm.IdViseesMaitriser == idVm)) continue;

                        var vm = await _context.ViseesMaitrisers
                            .FirstAsync(x => x.IdViseesMaitriser == idVm);

                        porteuse.IdViseesMaitriserFks.Add(vm);
                        liens++;
                    }

                    if (liens > 0) await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return Ok(new { IdVisees = idVisees, ViseesCreees = creees, LiensCrees = liens });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                var detail = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { message = $"Enregistrement refusé, rien n'a été créé : {detail}" });
            }
        }

        public class CreerNommeDto
        {
            public string? Nom { get; set; }
        }

        public class SelectionDto
        {
            public int IdDomaine { get; set; }
            public int IdSousDomaine { get; set; }

            public int IdCompetence { get; set; }

            public List<int> IdNomVisees { get; set; } = new();

            public List<int> IdsViseesMaitriser { get; set; } = new();
        }

        public class CreerDomaineDto
        {
            public string? Nom { get; set; }
            public string? CodeCours { get; set; }
            public string? CodeNiveau { get; set; }
        }

        public class CreerSousDomaineDto
        {
            public string? Nom { get; set; }
            public int IdDomaine { get; set; }
        }

        public class CreerViseeDto
        {
            public int IdDomaine { get; set; }
            public int IdSousDomaine { get; set; }
            public int IdNomVisee { get; set; }
            public int IdCompetence { get; set; }
        }

        public class CreerLienDto
        {
            public int IdVisee { get; set; }
            public int IdViseesMaitriser { get; set; }
        }
    }
}
