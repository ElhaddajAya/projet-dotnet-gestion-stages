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
    public class EntreprisesController : Controller
    {
        private readonly StagesDbContext _context;

        public EntreprisesController(StagesDbContext context)
        {
            _context = context;
        }

        // GET: Entreprises - AVEC RECHERCHE, FILTRAGE ET PAGINATION (10 par page)
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Index(string searchString, string secteurFilter, int pageNumber = 1)
        {
            const int pageSize = 10;

            // Conserver les filtres
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentSecteur = secteurFilter;

            var entreprisesQuery = _context.Entreprises.AsQueryable();

            // FILTRAGE PAR RÔLE
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                entreprisesQuery = entreprisesQuery.Where(e => e.EmailContact == userEmail);
                // Pour l'entreprise : pas de pagination, elle n'a qu'un seul profil
                var entrepriseProfil = await entreprisesQuery.SingleOrDefaultAsync();
                return View(new List<Entreprise> { entrepriseProfil ?? new Entreprise() });
            }

            // RECHERCHE (Admin seulement)
            if (!string.IsNullOrEmpty(searchString))
            {
                searchString = searchString.ToLower();
                entreprisesQuery = entreprisesQuery.Where(e =>
                    e.Nom.ToLower().Contains(searchString) ||
                    e.EmailContact.ToLower().Contains(searchString) ||
                    e.Adresse.ToLower().Contains(searchString) ||
                    e.Telephone.Contains(searchString));
            }

            // FILTRE PAR SECTEUR
            if (!string.IsNullOrEmpty(secteurFilter))
            {
                entreprisesQuery = entreprisesQuery.Where(e => e.Secteur == secteurFilter);
            }

            // TRI alphabétique
            entreprisesQuery = entreprisesQuery.OrderBy(e => e.Nom);

            // Nombre total (Admin)
            var totalEntreprises = await entreprisesQuery.CountAsync();

            // Pagination (Admin)
            var entreprises = await entreprisesQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Infos pagination
            ViewBag.PageNumber = pageNumber;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalEntreprises = totalEntreprises;
            ViewBag.HasPreviousPage = pageNumber > 1;
            ViewBag.HasNextPage = pageNumber < Math.Ceiling((double)totalEntreprises / pageSize);

            // Liste des secteurs pour le dropdown (évite NullReference)
            var secteurs = await _context.Entreprises
                .Select(e => e.Secteur)
                .Distinct()
                .Where(s => !string.IsNullOrEmpty(s))
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.Secteurs = secteurs;

            return View(entreprises);
        }

        // GET: Entreprises/Details/5 - Admin et Entreprise peuvent voir les détails
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entreprise = await _context.Entreprises
                .FirstOrDefaultAsync(m => m.Id == id);
            if (entreprise == null)
            {
                return NotFound();
            }

            // Si c'est une Entreprise, vérifier que c'est bien son profil
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                if (entreprise.EmailContact != userEmail)
                {
                    return Forbid();
                }
            }

            return View(entreprise);
        }

        // GET: Entreprises/Create - Seulement Admin peut créer
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Entreprises/Create - Seulement Admin peut créer
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,Nom,Adresse,Telephone,EmailContact,Secteur")] Entreprise entreprise)
        {
            if (ModelState.IsValid)
            {
                _context.Add(entreprise);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(entreprise);
        }

        // GET: Entreprises/Edit/5 - Admin et Entreprise peuvent modifier
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entreprise = await _context.Entreprises.FindAsync(id);
            if (entreprise == null)
            {
                return NotFound();
            }

            // Si c'est une Entreprise, vérifier que c'est bien son profil
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                if (entreprise.EmailContact != userEmail)
                {
                    return Forbid();
                }
            }

            return View(entreprise);
        }

        // POST: Entreprises/Edit/5 - Admin et Entreprise peuvent modifier
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Entreprise")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nom,Adresse,Telephone,EmailContact,Secteur")] Entreprise entreprise)
        {
            if (id != entreprise.Id)
            {
                return NotFound();
            }

            // Si c'est une Entreprise, vérifier que c'est bien son profil
            if (User.IsInRole("Entreprise"))
            {
                var userEmail = User.Identity.Name;
                if (entreprise.EmailContact != userEmail)
                {
                    return Forbid();
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(entreprise);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EntrepriseExists(entreprise.Id))
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
            return View(entreprise);
        }

        // GET: Entreprises/Delete/5 - Seulement Admin peut supprimer
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var entreprise = await _context.Entreprises
                .FirstOrDefaultAsync(m => m.Id == id);
            if (entreprise == null)
            {
                return NotFound();
            }

            return View(entreprise);
        }

        // POST: Entreprises/Delete/5 - Seulement Admin peut supprimer
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var entreprise = await _context.Entreprises.FindAsync(id);
            if (entreprise != null)
            {
                _context.Entreprises.Remove(entreprise);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EntrepriseExists(int id)
        {
            return _context.Entreprises.Any(e => e.Id == id);
        }
    }
}
