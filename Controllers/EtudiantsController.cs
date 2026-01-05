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
    public class EtudiantsController : Controller
    {
        private readonly StagesDbContext _context;

        public EtudiantsController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: Etudiants - AVEC RECHERCHE ET FILTRAGE
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Index(string searchString, string filiereFilter, string niveauFilter)
        {
            // Requête de base
            var etudiantsQuery = _context.Etudiants.AsQueryable();

            // FILTRAGE PAR RÔLE
            if (User.IsInRole("Etudiant"))
            {
                // L'étudiant voit seulement son profil
                var userEmail = User.Identity.Name;
                etudiantsQuery = etudiantsQuery.Where(e => e.Email == userEmail);
            }
            // Admin voit tous les étudiants

            // RECHERCHE PAR MOTS-CLÉS (Nom, Prénom, Email)
            if (!string.IsNullOrEmpty(searchString))
            {
                etudiantsQuery = etudiantsQuery.Where(e =>
                    e.Nom.Contains(searchString) ||
                    e.Prenom.Contains(searchString) ||
                    e.Email.Contains(searchString)
                );
            }

            // FILTRE PAR FILIÈRE
            if (!string.IsNullOrEmpty(filiereFilter))
            {
                etudiantsQuery = etudiantsQuery.Where(e => e.Filiere == filiereFilter);
            }

            // FILTRE PAR NIVEAU
            if (!string.IsNullOrEmpty(niveauFilter))
            {
                etudiantsQuery = etudiantsQuery.Where(e => e.Niveau == niveauFilter);
            }

            // TRI PAR NOM (ordre alphabétique)
            etudiantsQuery = etudiantsQuery.OrderBy(e => e.Nom).ThenBy(e => e.Prenom);

            // PRÉPARER LES DONNÉES POUR LES FILTRES
            // Liste des filières distinctes
            var filieres = await _context.Etudiants
                .Where(e => !string.IsNullOrWhiteSpace(e.Filiere))
                .Select(e => e.Filiere)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();

            ViewBag.Filieres = filieres;

            // Liste des niveaux distincts
            var niveaux = await _context.Etudiants
                .Where(e => !string.IsNullOrWhiteSpace(e.Niveau))
                .Select(e => e.Niveau)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            ViewBag.Niveaux = niveaux;

            // Conserver les valeurs des filtres
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentFiliere = filiereFilter;
            ViewBag.CurrentNiveau = niveauFilter;

            return View(await etudiantsQuery.ToListAsync());
        }

        // GET: Etudiants/Details/5 - Admin et Etudiant peuvent voir les détails
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var etudiant = await _context.Etudiants
                .FirstOrDefaultAsync(m => m.Id == id);
            if (etudiant == null)
            {
                return NotFound();
            }

            // Si c'est un Etudiant, vérifier que c'est bien son profil
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                if (etudiant.Email != userEmail)
                {
                    // Interdire l'accès au profil d'un autre étudiant
                    return Forbid();
                }
            }

            return View(etudiant);
        }

        // GET: Etudiants/Create - Seulement Admin peut créer
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Etudiants/Create - Seulement Admin peut créer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Nom,Prenom,Email,Telephone,Filiere,Niveau")] Etudiant etudiant)
        {
            if (ModelState.IsValid)
            {
                _context.Add(etudiant);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(etudiant);
        }

        // GET: Etudiants/Edit/5 - Admin et Etudiant peuvent modifier
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var etudiant = await _context.Etudiants.FindAsync(id);
            if (etudiant == null)
            {
                return NotFound();
            }

            // Si c'est un Etudiant, vérifier que c'est bien son profil
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                if (etudiant.Email != userEmail)
                {
                    return Forbid();
                }
            }

            return View(etudiant);
        }

        // POST: Etudiants/Edit/5 - Admin et Etudiant peuvent modifier
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Etudiant")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Prenom,Email,Telephone,Filiere,Niveau")] Etudiant etudiant)
        {
            if (id != etudiant.Id)
            {
                return NotFound();
            }

            // Si c'est un Etudiant, vérifier que c'est bien son profil
            if (User.IsInRole("Etudiant"))
            {
                var userEmail = User.Identity.Name;
                if (etudiant.Email != userEmail)
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(etudiant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EtudiantExists(etudiant.Id))
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
            return View(etudiant);
        }

        // GET: Etudiants/Delete/5 - Seulement Admin peut supprimer
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var etudiant = await _context.Etudiants
                .FirstOrDefaultAsync(m => m.Id == id);
            if (etudiant == null)
            {
                return NotFound();
            }

            return View(etudiant);
        }

        // POST: Etudiants/Delete/5 - Seulement Admin peut supprimer
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var etudiant = await _context.Etudiants.FindAsync(id);
            if (etudiant != null)
            {
                _context.Etudiants.Remove(etudiant);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EtudiantExists(int id)
        {
            return _context.Etudiants.Any(e => e.Id == id);
        }
    }
}
