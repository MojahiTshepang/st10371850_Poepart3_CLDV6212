using SleazyRetailers.Models;
using Microsoft.EntityFrameworkCore;

namespace SleazyRetailers.Services
{
    public interface IAuthService
    {
        Task<User> RegisterAsync(string username, string email, string password, string role, string firstName, string lastName);
        Task<User> LoginAsync(string username, string password);
        Task<User> GetUserByIdAsync(string userId);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User> RegisterAsync(string username, string email, string password, string role, string firstName, string lastName)
        {
            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == username || u.Email == email);
            if (existingUser != null)
                throw new Exception("Username or email already exists");

            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Username = username,
                Email = email,
                Password = password, // Note: In production, hash passwords!
                Role = role,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User> LoginAsync(string username, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
            if (user == null)
                throw new Exception("Invalid username or password");

            return user;
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }
    }
}