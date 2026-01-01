using Microsoft.AspNetCore.Identity;

namespace GestionStages.Data
{
    public class IdentitySeeder
    {
        private readonly AuthDbContext _authContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        // Cette classe pour créer les rôles et un compte Admin au démarrage
        public IdentitySeeder(AuthDbContext authContext, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _authContext = authContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // méthode est appelée au démarrage de l'appli
        public async Task SeedAsync()
        {
            // Liste des rôles qu'on veut dans l'appli
            string[] roles = { "Admin", "Etudiant", "Entreprise" };

            // je parcours chaque role
            foreach (var role in roles)
            {
                // verifions que le role n'existe pas deja dans la base
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    // si oui, on le créer
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Email de l'admin par défaut
            var adminEmail = "admin@emsi.ma";
            var adminPassword = "Admin@123";

            // on verifie si l'admin existe deja
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                // si n'existe pas, on le créer
                var newAdmin = new IdentityUser
                {
                    UserName = adminEmail, // le username sera l'email
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                // on cree l'utilisateur Admin
                var result = await _userManager.CreateAsync(newAdmin, adminPassword);

                // si la creation a reussi
                if (result.Succeeded)
                {
                    // on lui ajoute le role Admin
                    await _userManager.AddToRoleAsync(newAdmin, "Admin");

                    // identifiants de connexion de l'admin
                    // admin@emsi.ma / Admin@123
                }
            }
        }

    }
}
