using AuthService.Data;
using AuthService.Models.User;
using AuthService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.SeedData
{
    public class AuthSeeder(AuthDbContext dbContext, GeoCodingService geoCodingService, RabbitMqProducerService rabbitMqProducerService)
    {
        private readonly PasswordHasher<User> _passwordHasher = new();

        public async Task SeedAsync()
        {
            var tableExists = await dbContext.Database.CanConnectAsync();
            if (!tableExists)
            {
                Console.WriteLine("❌ База данных не готова, сидирование пропущено.");
                return;
            }

            if (await dbContext.Users.AnyAsync()) return;

            var users = new List<User>();

            var addresses = new List<string>
            {
                "Кишинёв, улица Штефан чел Маре, 123",
                "Кишинёв, бульвар Дачия, 45",
                "Кишинёв, улица Александру чел Бун, 78",
                "Кишинёв, проспект Мира, 56",
                "Кишинёв, улица Гренобля, 90",
                "Кишинёв, улица Чуфля, 12",
                "Кишинёв, улица Измаил, 34",
                "Кишинёв, улица Куза-Водэ, 87",
                "Кишинёв, улица Мунчешть, 15",
                "Кишинёв, улица Колумна, 67",
                "Кишинёв, улица Алба Юлия, 32",
                "Кишинёв, улица Каля Ешилор, 98",
                "Кишинёв, улица Михаил Когэлничану, 21",
                "Кишинёв, улица Каля Мошилор, 73",
                "Кишинёв, улица Тестемицану, 11",
                "Кишинёв, улица Буюканы, 54",
                "3/H, улица Спрынченоая, Телецентр, Кишинёв",
                "Кишинёв, улица Ботаника, 38",
                "56, улица Алессандро Бернардацци, 40 лет ВЛКСМ, Кишинёв",
                "3, улица Иона Некулче, Флакэра, Кишинёв",
                "Кишинёв, улица Боюканы, 31",
                "31, улица Виктора Крэсеску, Старая Почта, Кишинёв",
                "2, улица Молдова, Ciorescu, Муниципалитет Кишинёв",
                "Кишинёв, улица Алба, 24",
                "17, улица Чоричешть, Мунчешты, Кишинёв",
                "44/3, Армянская улица, Валя Дическу, Кишинёв",
                "Кишинёв, улица Эминеску, 81",
                "Кишинёв, улица Лазо, 36",
                "170, улица Колумна, Индустриальная Зона «Скулянка», Кишинёв",
                "16, улица Истра, Мунчешты, Кишинёв"
            };

            // Добавляем 10 менеджеров
            for (int i = 0; i < 10; i++)
            {
                users.Add(new User
                {
                    UserName = $"manager{i + 1}",
                    Email = $"manager{i + 1}@test.com",
                    Role = "manager",
                    FirstName = $"Алексей{i + 1}",
                    LastName = $"Иванов{i + 1}",
                    PhoneNumber = $"+37369999{10 + i}",
                    Address = addresses[i]
                });
            }

            var workers = new List<User>(); 

            // Добавляем 10 рабочих (workers)
            for (int i = 10; i < 20; i++)
            {
                var worker = new User
                {
                    UserName = $"worker{i - 9}",
                    Email = $"worker{i - 9}@test.com",
                    Role = "worker",
                    FirstName = $"Петр{i - 9}",
                    LastName = $"Сидоров{i - 9}",
                    PhoneNumber = $"+37368888{10 + (i - 10)}",
                    Address = addresses[i]
                };
                users.Add(worker);
                workers.Add(worker); 
            }

            // Добавляем 10 клиентов (clients)
            for (int i = 20; i < 30; i++)
            {
                users.Add(new User
                {
                    UserName = $"client{i - 19}",
                    Email = $"client{i - 19}@test.com",
                    Role = "client",
                    FirstName = $"Анна{i - 19}",
                    LastName = $"Петрова{i - 19}",
                    PhoneNumber = $"+37367777{10 + (i - 20)}",
                    Address = addresses[i]
                });
            }

            foreach (var user in users)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, "Test@123");

                if (!string.IsNullOrWhiteSpace(user.Address))
                {
                    var coordinates = await geoCodingService.GetCoordinatesAsync(user.Address);
                    if (coordinates.HasValue)
                    {
                        user.Latitude = coordinates.Value.Latitude;
                        user.Longitude = coordinates.Value.Longitude;
                    }
                }

                dbContext.Users.Add(user);
            }

            await dbContext.SaveChangesAsync();
            Console.WriteLine("✅ SeedData: 10 Managers, 10 Workers, 10 Clients добавлены успешно.");

            if (users.Count > 0)
            {
                foreach (var user in users)
                {
                    await rabbitMqProducerService.PublishUserUpdatedAsync(user);
                }
                Console.WriteLine($"📤 [RabbitMQ] Отправлено {users.Count} обновленных пользователей в OrderService.");
            }

        }
    }
}
