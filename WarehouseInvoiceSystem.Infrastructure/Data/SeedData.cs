namespace WarehouseInvoiceSystem.Infrastructure.Data
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using WarehouseInvoiceSystem.Domain.Entities;
    using WarehouseInvoiceSystem.Domain.Enums;

    public static class SeedData
    {
        private const string address = "с. Брајковци, Валандово";
        public static async Task RunAsync(IDbContextFactory<ApplicationDbContext> factory)
        {
            await using var context = factory.CreateDbContext();

            if (await context.Warehouses.AnyAsync())
            {
                Console.WriteLine("Database already has data — skipping seed.");
                return;
            }

            Console.WriteLine("Seeding database...");

            var now = DateTime.UtcNow;

            // ── Warehouse ──────────────────────────────────────────────────
            var warehouse = new Warehouse
            {
                Name = "Брајковци",
                Address = address,
                IsDefault = true,
                IsActive = true,
                CreatedAt = now
            };
            context.Warehouses.Add(warehouse);

            // ── Products ───────────────────────────────────────────────────
            var pepper = new Product
            {
                Code = "PRD-001",
                Name = "Пиперка",
                Description = "Свежа пиперка",
                Unit = "кг",
                CostPrice = 25.00m,
                SellingPrice = 35.00m,
                IsActive = true,
                CreatedAt = now
            };
            var tomato = new Product
            {
                Code = "PRD-002",
                Name = "Домат",
                Description = "Свеж домат",
                Unit = "кг",
                CostPrice = 20.00m,
                SellingPrice = 30.00m,
                IsActive = true,
                CreatedAt = now
            };
            var watermelon = new Product
            {
                Code = "PRD-003",
                Name = "Лубеница",
                Description = "Свежа лубеница",
                Unit = "кг",
                CostPrice = 8.00m,
                SellingPrice = 15.00m,
                IsActive = true,
                CreatedAt = now
            };
            context.Products.AddRange(pepper, tomato, watermelon);

            // ── Companies ──────────────────────────────────────────────────
            var clientSerbia = new Company
            {
                Name = "Agro Export Srbija",
                Type = CompanyType.Client,
                ContactPerson = "Милан Јовановић",
                Email = "milan@agroexport.rs",
                Phone = "+381 63 123456",
                Address = "Белград, Србија",
                TaxId = "RS100200300",
                PaymentTermsDays = 30,
                CreditLimit = 500000m,
                IsActive = true,
                CreatedAt = now
            };
            var clientKosovo = new Company
            {
                Name = "Fresh Produce Kosovo",
                Type = CompanyType.Client,
                ContactPerson = "Arben Hoxha",
                Email = "arben@freshproduce.xk",
                Phone = "+383 44 234567",
                Address = "Приштина, Косово",
                TaxId = "XK400500600",
                PaymentTermsDays = 15,
                CreditLimit = 300000m,
                IsActive = true,
                CreatedAt = now
            };
            var vendorCompany = new Company
            {
                Name = "Агро Снабдување ДООЕЛ",
                Type = CompanyType.Vendor,
                ContactPerson = "Петар Стојанов",
                Email = "petar@agrosnabduvanje.mk",
                Phone = "+389 70 345678",
                Address = "Скопје, Македонија",
                TaxId = "MK4030020010",
                PaymentTermsDays = 45,
                CreditLimit = 200000m,
                IsActive = true,
                CreatedAt = now
            };
            context.Companies.AddRange(clientSerbia, clientKosovo, vendorCompany);

            // ── Individuals ────────────────────────────────────────────────
            var individual1 = new Individual
            {
                FirstName = "Горан",
                LastName = "Трајков",
                IdentificationNumber = "0101980450001",
                Phone = "+389 71 111222",
                Address = address,
                IsActive = true,
                CreatedAt = now
            };
            var individual2 = new Individual
            {
                FirstName = "Стојан",
                LastName = "Митрев",
                IdentificationNumber = "1506975450002",
                Phone = "+389 72 333444",
                Address = address,
                IsActive = true,
                CreatedAt = now
            };
            var individual3 = new Individual
            {
                FirstName = "Благица",
                LastName = "Ристова",
                IdentificationNumber = "2009985450003",
                Phone = "+389 70 555666",
                Address = address,
                BankAccount = "300000000123456",
                IsActive = true,
                CreatedAt = now
            };
            context.Individuals.AddRange(individual1, individual2, individual3);

            // ── Users ──────────────────────────────────────────────────────
            var hasher = new PasswordHasher<User>();

            var admin = new User
            {
                Username = "admin",
                Email = "admin@warehouse.mk",
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = now
            };
            admin.PasswordHash = hasher.HashPassword(admin, "admin123");

            var user = new User
            {
                Username = "user",
                Email = "user@warehouse.mk",
                Role = UserRole.User,
                IsActive = true,
                CreatedAt = now
            };
            user.PasswordHash = hasher.HashPassword(user, "user123");

            context.Users.AddRange(admin, user);

            // Save to generate IDs
            await context.SaveChangesAsync();

            // ── Invoices (all Draft) ───────────────────────────────────────

            // Receivable 1: selling peppers to Serbia
            var inv1 = new Invoice
            {
                InvoiceNumber = "INV-2026-001",
                CompanyId = clientSerbia.Id,
                WarehouseId = warehouse.Id,
                Type = InvoiceType.Receivable,
                Status = InvoiceStatus.Draft,
                IssueDate = now,
                DueDate = now.AddDays(30),
                SubTotal = 35000m,
                TaxAmount = 6300m,
                TotalAmount = 41300m,
                CreatedAt = now
            };
            context.Invoices.Add(inv1);
            await context.SaveChangesAsync();

            context.InvoiceLines.Add(new InvoiceLine
            {
                InvoiceId = inv1.Id,
                ProductId = pepper.Id,
                Description = "Пиперка",
                Quantity = 1000,
                UnitPrice = 35.00m,
                TaxRate = 18m,
                CreatedAt = now
            });

            // Receivable 2: selling tomatoes to Kosovo
            var inv2 = new Invoice
            {
                InvoiceNumber = "INV-2026-002",
                CompanyId = clientKosovo.Id,
                WarehouseId = warehouse.Id,
                Type = InvoiceType.Receivable,
                Status = InvoiceStatus.Draft,
                IssueDate = now,
                DueDate = now.AddDays(15),
                SubTotal = 15000m,
                TaxAmount = 2700m,
                TotalAmount = 17700m,
                CreatedAt = now
            };
            context.Invoices.Add(inv2);
            await context.SaveChangesAsync();

            context.InvoiceLines.Add(new InvoiceLine
            {
                InvoiceId = inv2.Id,
                ProductId = tomato.Id,
                Description = "Домат",
                Quantity = 500,
                UnitPrice = 30.00m,
                TaxRate = 18m,
                CreatedAt = now
            });

            // Payable 1: buying watermelons from vendor company
            var inv3 = new Invoice
            {
                InvoiceNumber = "BILL-2026-001",
                CompanyId = vendorCompany.Id,
                WarehouseId = warehouse.Id,
                Type = InvoiceType.Payable,
                Status = InvoiceStatus.Draft,
                IssueDate = now,
                DueDate = now.AddDays(45),
                SubTotal = 16000m,
                TaxAmount = 2880m,
                TotalAmount = 18880m,
                CreatedAt = now
            };
            context.Invoices.Add(inv3);
            await context.SaveChangesAsync();

            context.InvoiceLines.Add(new InvoiceLine
            {
                InvoiceId = inv3.Id,
                ProductId = watermelon.Id,
                Description = "Лубеница",
                Quantity = 2000,
                UnitPrice = 8.00m,
                TaxRate = 18m,
                CreatedAt = now
            });

            // ── Purchase Notes (all Draft) ─────────────────────────────────

            // Purchase from individual 1: peppers
            var pn1 = new PurchaseNote
            {
                NoteNumber = "PN-2026-001",
                IndividualId = individual1.Id,
                WarehouseId = warehouse.Id,
                PurchaseDate = now,
                SubTotal = 12500m,
                TotalAmount = 12500m,
                Status = PurchaseNoteStatus.Draft,
                CreatedAt = now
            };
            context.PurchaseNotes.Add(pn1);
            await context.SaveChangesAsync();

            context.PurchaseNoteLines.Add(new PurchaseNoteLine
            {
                PurchaseNoteId = pn1.Id,
                ProductId = pepper.Id,
                Description = "Пиперка",
                GrossQuantity = 520m,
                KaloPercentage = 3.85m,
                Quantity = 500m,
                UnitPrice = 25.00m,
                CreatedAt = now
            });

            // Purchase from individual 2: tomatoes
            var pn2 = new PurchaseNote
            {
                NoteNumber = "PN-2026-002",
                IndividualId = individual2.Id,
                WarehouseId = warehouse.Id,
                PurchaseDate = now,
                SubTotal = 6000m,
                TotalAmount = 6000m,
                Status = PurchaseNoteStatus.Draft,
                CreatedAt = now
            };
            context.PurchaseNotes.Add(pn2);
            await context.SaveChangesAsync();

            context.PurchaseNoteLines.Add(new PurchaseNoteLine
            {
                PurchaseNoteId = pn2.Id,
                ProductId = tomato.Id,
                Description = "Домат",
                GrossQuantity = 310m,
                KaloPercentage = 3.23m,
                Quantity = 300m,
                UnitPrice = 20.00m,
                CreatedAt = now
            });

            // Purchase from individual 3: watermelons
            var pn3 = new PurchaseNote
            {
                NoteNumber = "PN-2026-003",
                IndividualId = individual3.Id,
                WarehouseId = warehouse.Id,
                PurchaseDate = now,
                SubTotal = 8000m,
                TotalAmount = 8000m,
                Status = PurchaseNoteStatus.Draft,
                CreatedAt = now
            };
            context.PurchaseNotes.Add(pn3);
            await context.SaveChangesAsync();

            context.PurchaseNoteLines.Add(new PurchaseNoteLine
            {
                PurchaseNoteId = pn3.Id,
                ProductId = watermelon.Id,
                Description = "Лубеница",
                GrossQuantity = 1050m,
                KaloPercentage = 4.76m,
                Quantity = 1000m,
                UnitPrice = 8.00m,
                CreatedAt = now
            });

            await context.SaveChangesAsync();

            Console.WriteLine("Seed complete:");
            Console.WriteLine("  1 warehouse (Брајковци)");
            Console.WriteLine("  3 products (Пиперка, Домат, Лубеница)");
            Console.WriteLine("  3 companies (2 clients, 1 vendor)");
            Console.WriteLine("  3 individuals");
            Console.WriteLine("  2 users (admin/admin123, user/user123)");
            Console.WriteLine("  3 invoices (Draft: 2 receivable, 1 payable)");
            Console.WriteLine("  3 purchase notes (Draft)");
        }
    }
}
