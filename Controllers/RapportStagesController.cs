using GestionStages.Data;
using GestionStages.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GestionStages.Controllers
{
    [Authorize] // Tous les utilisateurs connectés
    public class RapportStagesController : Controller
    {
        private readonly StagesDbContext _context;

        public RapportStagesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: RapportStages - Admin et Etudiant peuvent voir
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Index()
        {
            // Si Admin, afficher tous les rapports
            if (User.IsInRole("Admin"))
            {
                var tousLesRapports = _context.RapportsStages.Include(r => r.Convention);
                return View(await tousLesRapports.ToListAsync());
            }

            // Si Etudiant, afficher seulement ses rapports
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null)
                {
                    return View(await _context.RapportsStages.Where(r => false).ToListAsync());
                }

                // Récupérer les rapports de l'étudiant via les conventions
                var rapportsEtudiant = _context.RapportsStages
                    .Include(r => r.Convention)
                        .ThenInclude(c => c.Candidature)
                    .Where(r => r.Convention.Candidature.EtudiantId == etudiant.Id);

                return View(await rapportsEtudiant.ToListAsync());
            }

            return View(await _context.RapportsStages.Include(r => r.Convention).ToListAsync());
        }

        // GET: RapportStages/Details/5 - Admin et Etudiant peuvent voir les détails
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rapportStage = await _context.RapportsStages
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (rapportStage == null)
            {
                return NotFound();
            }

            // Vérifier les droits d'accès pour l'étudiant
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null || rapportStage.Convention.Candidature.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }
            }

            return View(rapportStage);
        }

        // GET: RapportStages/Create - Admin et Etudiant peuvent créer
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Create()
        {
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null)
                {
                    TempData["Error"] = "Veuillez d'abord compléter votre profil étudiant.";
                    return RedirectToAction("Index", "Etudiants");
                }

                // L'étudiant ne voit que ses propres conventions
                var conventionsEtudiant = await _context.Conventions
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.Etudiant)
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.OffreStage)
                    .Where(c => c.Candidature.EtudiantId == etudiant.Id)
                    .Select(c => new
                    {
                        c.Id,
                        Display = "Convention #" + c.Id + " - " + c.Candidature.OffreStage.Titre
                    })
                    .ToListAsync(); // ✅ Ajout de ToListAsync

                if (!conventionsEtudiant.Any())
                {
                    // ✅ Ne pas rediriger, afficher la page avec un message
                    TempData["Warning"] = "Vous n'avez aucune convention active. Contactez l'administrateur pour créer une convention suite à une candidature acceptée.";
                    ViewData["ConventionId"] = new SelectList(Enumerable.Empty<SelectListItem>());
                    return View();
                }

                ViewData["ConventionId"] = new SelectList(conventionsEtudiant, "Id", "Display");
            }
            else
            {
                // Admin peut choisir n'importe quelle convention
                var conventions = await _context.Conventions
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.Etudiant)
                    .Select(c => new
                    {
                        c.Id,
                        Display = "Convention #" + c.Id + " - " + c.Candidature.Etudiant.Nom
                    })
                    .ToListAsync(); // ✅ Ajout de ToListAsync

                ViewData["ConventionId"] = new SelectList(conventions, "Id", "Display");
            }

            return View();
        }

        // POST: RapportStages/Create - Admin et Etudiant peuvent créer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Create([Bind("Id,Titre,NomFichier,DateDepot,ConventionId")] RapportStage rapportStage)
        {
            // Vérifier les droits d'accès pour l'étudiant
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null)
                {
                    return Forbid();
                }

                // Vérifier que la convention appartient bien à l'étudiant
                var convention = await _context.Conventions
                    .Include(c => c.Candidature)
                    .FirstOrDefaultAsync(c => c.Id == rapportStage.ConventionId);

                if (convention == null || convention.Candidature.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }
            }

            // Retirer la propriété de navigation de la validation
            ModelState.Remove("Convention");

            if (ModelState.IsValid)
            {
                _context.Add(rapportStage);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Rapport de stage créé avec succès !";
                return RedirectToAction(nameof(Index));
            }

            // Recharger les listes en cas d'erreur
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant != null)
                {
                    var conventionsEtudiant = _context.Conventions
                        .Include(c => c.Candidature)
                            .ThenInclude(ca => ca.Etudiant)
                        .Include(c => c.Candidature)
                            .ThenInclude(ca => ca.OffreStage)
                        .Where(c => c.Candidature.EtudiantId == etudiant.Id)
                        .Select(c => new
                        {
                            c.Id,
                            Display = "Convention #" + c.Id + " - " + c.Candidature.OffreStage.Titre
                        })
                        .ToList();

                    ViewData["ConventionId"] = new SelectList(conventionsEtudiant, "Id", "Display", rapportStage.ConventionId);
                }
            }
            else
            {
                var conventions = _context.Conventions
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.Etudiant)
                    .Select(c => new
                    {
                        c.Id,
                        Display = "Convention #" + c.Id + " - " + c.Candidature.Etudiant.Nom
                    })
                    .ToList();

                ViewData["ConventionId"] = new SelectList(conventions, "Id", "Display", rapportStage.ConventionId);
            }

            return View(rapportStage);
        }

        // GET: RapportStages/Edit/5 - Admin et Etudiant peuvent modifier
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rapportStage = await _context.RapportsStages
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rapportStage == null)
            {
                return NotFound();
            }

            // Vérifier les droits d'accès pour l'étudiant
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null || rapportStage.Convention.Candidature.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }

                // L'étudiant ne peut modifier que ses propres rapports
                var conventionsEtudiant = _context.Conventions
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.Etudiant)
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.OffreStage)
                    .Where(c => c.Candidature.EtudiantId == etudiant.Id)
                    .Select(c => new
                    {
                        c.Id,
                        Display = "Convention #" + c.Id + " - " + c.Candidature.OffreStage.Titre
                    })
                    .ToList();

                ViewData["ConventionId"] = new SelectList(conventionsEtudiant, "Id", "Display", rapportStage.ConventionId);
            }
            else
            {
                // Admin peut tout modifier
                var conventions = _context.Conventions
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.Etudiant)
                    .Select(c => new
                    {
                        c.Id,
                        Display = "Convention #" + c.Id + " - " + c.Candidature.Etudiant.Nom
                    })
                    .ToList();

                ViewData["ConventionId"] = new SelectList(conventions, "Id", "Display", rapportStage.ConventionId);
            }

            return View(rapportStage);
        }

        // POST: RapportStages/Edit/5 - Admin et Etudiant peuvent modifier
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,NomFichier,DateDepot,ConventionId")] RapportStage rapportStage)
        {
            if (id != rapportStage.Id)
            {
                return NotFound();
            }

            // Récupérer le rapport original pour vérifier les droits
            var rapportOriginal = await _context.RapportsStages
                .Include(r => r.Convention)
                    .ThenInclude(c => c.Candidature)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (rapportOriginal == null)
            {
                return NotFound();
            }

            // Vérifier les droits d'accès pour l'étudiant
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null || rapportOriginal.Convention.Candidature.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }
            }

            // Retirer la propriété de navigation
            ModelState.Remove("Convention");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(rapportStage);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Rapport de stage modifié avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RapportStageExists(rapportStage.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            // Recharger les listes
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant != null)
                {
                    var conventionsEtudiant = _context.Conventions
                        .Include(c => c.Candidature)
                            .ThenInclude(ca => ca.Etudiant)
                        .Include(c => c.Candidature)
                            .ThenInclude(ca => ca.OffreStage)
                        .Where(c => c.Candidature.EtudiantId == etudiant.Id)
                        .Select(c => new
                        {
                            c.Id,
                            Display = "Convention #" + c.Id + " - " + c.Candidature.OffreStage.Titre
                        })
                        .ToList();

                    ViewData["ConventionId"] = new SelectList(conventionsEtudiant, "Id", "Display", rapportStage.ConventionId);
                }
            }
            else
            {
                var conventions = _context.Conventions
                    .Include(c => c.Candidature)
                        .ThenInclude(ca => ca.Etudiant)
                    .Select(c => new
                    {
                        c.Id,
                        Display = "Convention #" + c.Id + " - " + c.Candidature.Etudiant.Nom
                    })
                    .ToList();

                ViewData["ConventionId"] = new SelectList(conventions, "Id", "Display", rapportStage.ConventionId);
            }

            return View(rapportStage);
        }

        // GET: RapportStages/Delete/5 - Seulement Admin peut supprimer
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var rapportStage = await _context.RapportsStages
                .Include(r => r.Convention)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (rapportStage == null)
            {
                return NotFound();
            }

            return View(rapportStage);
        }

        // POST: RapportStages/Delete/5 - Seulement Admin peut supprimer
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var rapportStage = await _context.RapportsStages.FindAsync(id);
            if (rapportStage != null)
            {
                _context.RapportsStages.Remove(rapportStage);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Rapport de stage supprimé avec succès !";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool RapportStageExists(int id)
        {
            return _context.RapportsStages.Any(e => e.Id == id);
        }
    }
}
