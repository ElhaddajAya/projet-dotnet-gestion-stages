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
    public class CandidaturesController : Controller
    {
        private readonly StagesDbContext _context;

        public CandidaturesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: Candidatures - Admin voit tout, Etudiant voit ses candidatures, Entreprise voit les candidatures reçues
        [Authorize(Roles = "Admin,Etudiant,Entreprise")]
        public async Task<IActionResult> Index()
        {
            // Si Admin, afficher toutes les candidatures
            if (User.IsInRole("Admin"))
            {
                var toutesCandidatures = _context.Candidatures
                    .Include(c => c.Etudiant)
                    .Include(c => c.OffreStage);
                return View(await toutesCandidatures.ToListAsync());
            }

            // Si Etudiant, afficher seulement ses candidatures
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null)
                {
                    return View(await _context.Candidatures.Where(c => false).ToListAsync());
                }

                var candidaturesEtudiant = _context.Candidatures
                    .Include(c => c.Etudiant)
                    .Include(c => c.OffreStage)
                    .Where(c => c.EtudiantId == etudiant.Id);

                return View(await candidaturesEtudiant.ToListAsync());
            }

            // Si Entreprise, afficher les candidatures pour ses offres
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise == null)
                {
                    return View(await _context.Candidatures.Where(c => false).ToListAsync());
                }

                // Récupérer les candidatures pour les offres de cette entreprise
                var candidaturesRecues = _context.Candidatures
                    .Include(c => c.Etudiant)
                    .Include(c => c.OffreStage)
                    .Where(c => c.OffreStage.EntrepriseId == entreprise.Id);

                return View(await candidaturesRecues.ToListAsync());
            }

            return View(await _context.Candidatures.Include(c => c.Etudiant).Include(c => c.OffreStage).ToListAsync());
        }

        // GET: Candidatures/Details/5 - Admin, Etudiant et Entreprise peuvent voir les détails
        [Authorize(Roles = "Admin,Etudiant,Entreprise")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidature = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (candidature == null)
            {
                return NotFound();
            }

            // Vérifier les droits d'accès
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null || candidature.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }
            }
            else if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise == null || candidature.OffreStage.EntrepriseId != entreprise.Id)
                {
                    return Forbid();
                }
            }

            return View(candidature);
        }

        // GET: Candidatures/Create - Seulement Admin et Etudiant peuvent créer
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

                // L'étudiant ne peut postuler que pour lui-même
                ViewData["EtudiantId"] = new SelectList(new[] { etudiant }, "Id", "Nom", etudiant.Id);
            }
            else
            {
                // Admin peut choisir n'importe quel étudiant
                ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom");
            }

            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre");
            return View();
        }

        // POST: Candidatures/Create - Seulement Admin et Etudiant peuvent créer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Create([Bind("Id,DateCandidature,Statut,EtudiantId,OffreStageId")] Candidature candidature)
        {
            // Si Etudiant, forcer l'ID à être celui de l'étudiant connecté
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null)
                {
                    return Forbid();
                }

                candidature.EtudiantId = etudiant.Id;
            }

            // Retirer les propriétés de navigation de la validation
            ModelState.Remove("Etudiant");
            ModelState.Remove("OffreStage");

            if (ModelState.IsValid)
            {
                _context.Add(candidature);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Candidature créée avec succès !";
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
                    ViewData["EtudiantId"] = new SelectList(new[] { etudiant }, "Id", "Nom", candidature.EtudiantId);
                }
            }
            else
            {
                ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom", candidature.EtudiantId);
            }

            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre", candidature.OffreStageId);
            return View(candidature);
        }

        // GET: Candidatures/Edit/5 - Admin, Etudiant et Entreprise peuvent modifier (Entreprise pour changer le statut)
        [Authorize(Roles = "Admin,Etudiant,Entreprise")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidature = await _context.Candidatures
                .Include(c => c.OffreStage)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidature == null)
            {
                return NotFound();
            }

            // Vérifier les droits d'accès
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null || candidature.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }

                ViewData["EtudiantId"] = new SelectList(new[] { etudiant }, "Id", "Nom", candidature.EtudiantId);
            }
            else if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise == null || candidature.OffreStage.EntrepriseId != entreprise.Id)
                {
                    return Forbid();
                }

                // L'entreprise ne peut modifier que le statut
                ViewData["ReadOnly"] = true;
                ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom", candidature.EtudiantId);
            }
            else
            {
                ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom", candidature.EtudiantId);
            }

            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre", candidature.OffreStageId);
            return View(candidature);
        }

        // POST: Candidatures/Edit/5 - Admin, Etudiant et Entreprise peuvent modifier
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant,Entreprise")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DateCandidature,Statut,EtudiantId,OffreStageId")] Candidature candidature)
        {
            if (id != candidature.Id)
            {
                return NotFound();
            }

            // Récupérer la candidature originale
            var candidatureOriginale = await _context.Candidatures
                .Include(c => c.OffreStage)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (candidatureOriginale == null)
            {
                return NotFound();
            }

            // Vérifier les droits d'accès
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                var etudiant = await _context.Etudiants
                    .FirstOrDefaultAsync(e => e.Email == userEmail);

                if (etudiant == null || candidatureOriginale.EtudiantId != etudiant.Id)
                {
                    return Forbid();
                }
            }
            else if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                var entreprise = await _context.Entreprises
                    .FirstOrDefaultAsync(e => e.EmailContact == userEmail);

                if (entreprise == null || candidatureOriginale.OffreStage.EntrepriseId != entreprise.Id)
                {
                    return Forbid();
                }

                // L'entreprise ne peut modifier QUE le statut
                candidature.EtudiantId = candidatureOriginale.EtudiantId;
                candidature.OffreStageId = candidatureOriginale.OffreStageId;
                candidature.DateCandidature = candidatureOriginale.DateCandidature;
            }

            // Retirer les propriétés de navigation
            ModelState.Remove("Etudiant");
            ModelState.Remove("OffreStage");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(candidature);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Candidature modifiée avec succès !";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CandidatureExists(candidature.Id))
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
            ViewData["EtudiantId"] = new SelectList(_context.Etudiants, "Id", "Nom", candidature.EtudiantId);
            ViewData["OffreStageId"] = new SelectList(_context.OffresStages, "Id", "Titre", candidature.OffreStageId);
            return View(candidature);
        }


        // GET: Candidatures/Delete/5 - Seulement Admin peut supprimer
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var candidature = await _context.Candidatures
                .Include(c => c.Etudiant)
                .Include(c => c.OffreStage)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (candidature == null)
            {
                return NotFound();
            }

            return View(candidature);
        }

        // POST: Candidatures/Delete/5 - Seulement Admin peut supprimer
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var candidature = await _context.Candidatures.FindAsync(id);
            if (candidature != null)
            {
                _context.Candidatures.Remove(candidature);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Candidature supprimée avec succès !";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CandidatureExists(int id)
        {
            return _context.Candidatures.Any(e => e.Id == id);
        }
    }
}
