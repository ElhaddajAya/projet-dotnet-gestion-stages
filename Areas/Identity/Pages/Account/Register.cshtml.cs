// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using GestionStages.Data;
using GestionStages.Models;
using Humanizer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;

namespace GestionStages.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStore<IdentityUser> _userStore;
        private readonly IUserEmailStore<IdentityUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly StagesDbContext _stagesContext; // accès à la base de données des stages

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            IUserStore<IdentityUser> userStore,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            StagesDbContext stagesContext) // injection du contexte
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _stagesContext = stagesContext; // ajout du contexte
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        // Liste des rôles disponibles pour l'inscription
        public List<SelectListItem> RolesDisponibles { get; set; }


        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            // Nouveau champ pour choisir le rôle
            [Required(ErrorMessage = "Veuillez choisir un type de compte")]
            [Display(Name = "Type de compte")]
            public string Role { get; set; }
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // On prépare la liste des rôles disponibles
            // Seulement Etudiant et Entreprise (pas Admin)
            RolesDisponibles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Etudiant", Text = "Étudiant" },
                new SelectListItem { Value = "Entreprise", Text = "Entreprise" }
            };
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            // On recharge la liste des rôles en cas d'erreur
            RolesDisponibles = new List<SelectListItem>
            {
                new SelectListItem { Value = "Etudiant", Text = "Étudiant" },
                new SelectListItem { Value = "Entreprise", Text = "Entreprise" }
            };

            if (ModelState.IsValid)
            {
                // On crée un nouvel utilisateur
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // On crée l'utilisateur dans la base Identity
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Un nouvel utilisateur a créé un compte avec mot de passe.");

                    // On assigne le rôle choisi à l'utilisateur
                    await _userManager.AddToRoleAsync(user, Input.Role);
                    _logger.LogInformation($"Rôle '{Input.Role}' assigné à l'utilisateur {Input.Email}");

                    // On crée automatiquement l'entité Etudiant ou Entreprise
                    if (Input.Role == "Etudiant")
                    {
                        // Créer un nouvel étudiant avec l'email de l'utilisateur
                        var etudiant = new Etudiant
                        {
                            Email = Input.Email,
                            Nom = "", // L'étudiant pourra remplir ces informations plus tard
                            Prenom = "",
                            Telephone = "",
                            Filiere = "",
                            Niveau = ""
                        };

                        _stagesContext.Etudiants.Add(etudiant);
                        await _stagesContext.SaveChangesAsync();
                        _logger.LogInformation($"Profil étudiant créé pour {Input.Email}");
                    }
                    else if (Input.Role == "Entreprise")
                    {
                        // Créer une nouvelle entreprise avec l'email de l'utilisateur
                        var entreprise = new Entreprise
                        {
                            EmailContact = Input.Email,
                            Nom = "", // L'entreprise pourra remplir ces informations plus tard
                            Adresse = "",
                            Telephone = "",
                            Secteur = ""
                        };

                        _stagesContext.Entreprises.Add(entreprise);
                        await _stagesContext.SaveChangesAsync();
                        _logger.LogInformation($"Profil entreprise créé pour {Input.Email}");
                    }

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "Confirmez votre email",
                        $"Veuillez confirmer votre compte en <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>cliquant ici</a>.");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        // On connecte automatiquement l'utilisateur
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }

                // Si la création a échoué, on affiche les erreurs
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Si on arrive ici, c'est qu'il y a eu une erreur
            return Page();
        }

        private IdentityUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<IdentityUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(IdentityUser)}'. " +
                    $"Ensure that '{nameof(IdentityUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<IdentityUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<IdentityUser>)_userStore;
        }
    }
}
