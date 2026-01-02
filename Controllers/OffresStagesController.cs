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
    public class OffresStagesController : Controller
    {
        private readonly StagesDbContext _context;

        public OffresStagesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: OffresStages - Tout le monde peut voir (même non connectés)
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // Si l'utilisateur est Admin ou Etudiant, on affiche toutes les offres
            if (User.IsInRole("Admin") || User.IsInRole("Etudiant") || !User.Identity.IsAuthenticated)
            {
                var toutesLesOffres = _context.OffresStages.Include(o => o.Entreprise);
                return View(await toutesLesOffres.ToListAsync());
            }

            // Si c'est une Entreprise, on affiche seulement ses offres
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;

                // On récupère l'ID de l'entreprise connectée
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise == null)
                {
                    // L'entreprise n'a pas encore complété son profil
                    return View(await _context.OffresStages.Where(o => false).ToListAsync());
                }

                // On affiche uniquement les offres de cette entreprise
                var offresEntreprise = _context.OffresStages
                    .Include(o => o.Entreprise)
                    .Where(o => o.EntrepriseId == entreprise.Id);

                return View(await offresEntreprise.ToListAsync());
            }

            // Par défaut, afficher toutes les offres
            return View(await _context.OffresStages.Include(o => o.Entreprise).ToListAsync());
        }

        // GET: OffresStages/Details/5 - Tout le monde peut voir les détails
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offresStage = await _context.OffresStages
                .Include(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (offresStage == null)
            {
                return NotFound();
            }

            return View(offresStage);
        }

        // GET: OffresStages/Create - Seulement Admin et Entreprise peuvent créer
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Create() 
        {
            // Si c'est une Entreprise, on pré-sélectionne son profil
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises 
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise == null)
                {
                    // L'entreprise doit d'abord compléter son profil
                    TempData["Error"] = "Veuillez d'abord compléter votre profil entreprise.";
                    return RedirectToAction("Index", "Entreprises");
                }

                // Vérifier que le nom est rempli
                if (string.IsNullOrWhiteSpace(entreprise.Nom))
                {
                    TempData["Error"] = "Veuillez d'abord remplir le nom de votre entreprise.";
                    return RedirectToAction("Edit", "Entreprises", new { id = entreprise.Id });
                }

                // On n'affiche que l'entreprise connectée dans la liste
                ViewData["EntrepriseId"] = new SelectList(
                    new[] { entreprise },
                    "Id",
                    "Nom",
                    entreprise.Id
                );
            }
            else
            {
                // Admin peut choisir n'importe quelle entreprise
                ViewData["EntrepriseId"] = new SelectList(_context.Entreprises, "Id", "Nom");
            }

            return View();
        }

        // POST: OffresStages/Create - Seulement Admin et Entreprise peuvent créer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Create([Bind("Id,Titre,Description,DureeMois,DateDebutSouhaitee,EntrepriseId")] OffresStage offresStage)
        {
            // Si c'est une Entreprise, vérifier qu'elle crée une offre pour elle-même
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise == null)
                {
                    TempData["Error"] = "Votre profil entreprise n'a pas été trouvé.";
                    return RedirectToAction("Index", "Entreprises");
                }

                // Forcer l'EntrepriseId à être celui de l'entreprise connectée
                offresStage.EntrepriseId = entreprise.Id;
            }

            // Retirer la navigation Entreprise de la validation
            ModelState.Remove("Entreprise");

            if (ModelState.IsValid)
            {
                _context.Add(offresStage);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Offre de stage créée avec succès !";
                return RedirectToAction(nameof(Index));
            }

            // Afficher les erreurs dans la console (pour déboguer)
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                Console.WriteLine($"Erreur de validation : {error.ErrorMessage}");
            }

            // Recharger la liste en cas d'erreur
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise != null)
                {
                    ViewData["EntrepriseId"] = new SelectList(
                        new[] { entreprise },
                        "Id",
                        "Nom",
                        offresStage.EntrepriseId
                    );
                }
            }
            else
            {
                ViewData["EntrepriseId"] = new SelectList(_context.Entreprises, "Id", "Nom", offresStage.EntrepriseId);
            }

            return View(offresStage);
        }

        // GET: OffresStages/Edit/5 - Seulement Admin et Entreprise peuvent modifier
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offresStage = await _context.OffresStages.FindAsync(id);
            if (offresStage == null)
            {
                return NotFound();
            }

            // Si c'est une Entreprise, vérifier que c'est bien son offre
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise == null || offresStage.EntrepriseId != entreprise.Id)
                {
                    return Forbid();
                }

                // On n'affiche que l'entreprise connectée dans la liste
                ViewData["EntrepriseId"] = new SelectList(
                    new[] { entreprise },
                    "Id",
                    "Nom",
                    offresStage.EntrepriseId
                );
            }
            else
            {
                // Admin peut choisir n'importe quelle entreprise
                ViewData["EntrepriseId"] = new SelectList(_context.Entreprises, "Id", "Nom", offresStage.EntrepriseId);
            }

            return View(offresStage);
        }

        // POST: OffresStages/Edit/5 - Seulement Admin et Entreprise peuvent modifier
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titre,Description,DureeMois,DateDebutSouhaitee,EntrepriseId")] OffresStage offresStage)
        {
            if (id != offresStage.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(offresStage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OffresStageExists(offresStage.Id))
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
            ViewData["EntrepriseId"] = new SelectList(_context.Entreprises, "Id", "Nom", offresStage.EntrepriseId);
            return View(offresStage);
        }

        // GET: OffresStages/Delete/5 - Seulement Admin et Entreprise peuvent supprimer
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var offresStage = await _context.OffresStages
                .Include(o => o.Entreprise)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (offresStage == null)
            {
                return NotFound();
            }

            return View(offresStage);
        }

        // POST: OffresStages/Delete/5 - Seulement Admin et Entreprise peuvent supprimer
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var offresStage = await _context.OffresStages.FindAsync(id);

            if (offresStage != null)
            {
                // Si c'est une Entreprise, vérifier que c'est bien son offre
                if (User.IsInRole("Entreprise"))
                {
                    var userEmail = User.Identity.Name;
                    var entreprise = await _context.Entreprises
                        .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                    if (entreprise == null || offresStage.EntrepriseId != entreprise.Id)
                    {
                        return Forbid();
                    }
                }

                _context.OffresStages.Remove(offresStage);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OffresStageExists(int id)
        {
            return _context.OffresStages.Any(e => e.Id == id);
        }
    }
}
