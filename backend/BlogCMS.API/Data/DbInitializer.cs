using BlogCMS.API.Models;
using Microsoft.AspNetCore.Identity;

namespace BlogCMS.API.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Create roles if they don't exist
            string[] roles = { "Admin", "Editor", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Create admin user if it doesn't exist
            if (await userManager.FindByEmailAsync("admin@blogcms.com") == null)
            {
                var adminUser = new ApplicationUser
                {
                    UserName = "admin@blogcms.com",
                    Email = "admin@blogcms.com",
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Create editor user if it doesn't exist
            if (await userManager.FindByEmailAsync("editor@blogcms.com") == null)
            {
                var editorUser = new ApplicationUser
                {
                    UserName = "editor@blogcms.com",
                    Email = "editor@blogcms.com",
                    FullName = "Content Editor",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(editorUser, "Editor@123456");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(editorUser, "Editor");
                }
            }

            // Seed initial posts if none exist
            if (!context.Posts.Any())
            {
                var samplePosts = new List<Post>
                {
                    new Post
                    {
                        Title = "Getting Started with ASP.NET Core",
                        Category = "Tech",
                        Content = @"ASP.NET Core is a cross-platform, high-performance framework for building modern, cloud-based, Internet-connected applications. 
                        
With ASP.NET Core, you can:
- Build web apps and services, IoT apps, and mobile backends
- Use your favorite development tools on Windows, macOS, and Linux
- Deploy to the cloud or on-premises
- Run on .NET Core or .NET Framework

This is a comprehensive guide to help you get started with ASP.NET Core development.",
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        IsPublished = true,
                        CreatedBy = "admin@blogcms.com"
                    },
                    new Post
                    {
                        Title = "10 Study Tips for College Students",
                        Category = "College",
                        Content = @"College life can be challenging. Here are 10 proven study tips to help you succeed in your academic journey:

1. Create a study schedule and stick to it
2. Find a quiet study space free from distractions
3. Take regular breaks using the Pomodoro technique
4. Form study groups with classmates
5. Use active recall and spaced repetition
6. Teach concepts to others to reinforce learning
7. Stay organized with digital tools and planners
8. Get enough sleep and exercise
9. Eat healthy foods to fuel your brain
10. Don't hesitate to ask for help when needed

Implement these tips and watch your grades improve!",
                        CreatedAt = DateTime.UtcNow.AddDays(-3),
                        IsPublished = true,
                        CreatedBy = "editor@blogcms.com"
                    },
                    new Post
                    {
                        Title = "Healthy Study Snacks for College Students",
                        Category = "Lifestyle",
                        Content = @"Eating healthy while studying is important. Here are some nutritious snack ideas to keep you energized during long study sessions:

**Brain-Boosting Snacks:**
- Nuts and seeds (almonds, walnuts, pumpkin seeds)
- Fresh fruits (berries, apples, bananas)
- Greek yogurt with honey
- Vegetable sticks with hummus
- Dark chocolate (70%+ cocoa)

**Quick Energy Snacks:**
- Trail mix with dried fruits
- Rice cakes with avocado
- Hard-boiled eggs
- Smoothies with spinach and berries
- Oatmeal with fruits

Remember to stay hydrated with water instead of energy drinks!",
                        CreatedAt = DateTime.UtcNow.AddDays(-1),
                        IsPublished = true,
                        CreatedBy = "editor@blogcms.com"
                    }
                };

                await context.Posts.AddRangeAsync(samplePosts);
                await context.SaveChangesAsync();
            }
        }
    }
}