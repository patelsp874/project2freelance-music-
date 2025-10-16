using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;

namespace FreelanceMusicAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=freelance_music.db";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                var createUsersTableCmd = connection.CreateCommand();
                createUsersTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        FirstName TEXT NOT NULL,
                        LastName TEXT NOT NULL,
                        Email TEXT UNIQUE NOT NULL,
                        Password TEXT NOT NULL,
                        AccountType TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL
                    );";
                createUsersTableCmd.ExecuteNonQuery();

                var createSessionsTableCmd = connection.CreateCommand();
                createSessionsTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Sessions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        SessionToken TEXT UNIQUE NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        ExpiresAt TEXT NOT NULL,
                        FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_Sessions_SessionToken ON Sessions (SessionToken);
                    CREATE INDEX IF NOT EXISTS IX_Sessions_UserId ON Sessions (UserId);
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_Users_Email ON Users (Email);
                ";
                createSessionsTableCmd.ExecuteNonQuery();
            }
        }

        [HttpPost("signup")]
        public IActionResult Signup([FromBody] SignupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName) ||
                string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) ||
                string.IsNullOrWhiteSpace(request.AccountType))
            {
                return BadRequest(new { success = false, error = "Please fill in all required fields." });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new { success = false, error = "Password must be at least 6 characters." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Check if email already exists
                var checkEmailCmd = connection.CreateCommand();
                checkEmailCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Email = @email";
                checkEmailCmd.Parameters.AddWithValue("@email", request.Email);
                if (Convert.ToInt32(checkEmailCmd.ExecuteScalar()) > 0)
                {
                    return BadRequest(new { success = false, error = "Email already registered." });
                }

                // Insert new user
                var insertUserCmd = connection.CreateCommand();
                insertUserCmd.CommandText = @"
                    INSERT INTO Users (FirstName, LastName, Email, Password, AccountType, CreatedAt)
                    VALUES (@firstName, @lastName, @email, @password, @accountType, @createdAt);
                    SELECT last_insert_rowid();"; // Get the last inserted Id

                insertUserCmd.Parameters.AddWithValue("@firstName", request.FirstName);
                insertUserCmd.Parameters.AddWithValue("@lastName", request.LastName);
                insertUserCmd.Parameters.AddWithValue("@email", request.Email);
                insertUserCmd.Parameters.AddWithValue("@password", request.Password); // In a real app, hash this password!
                insertUserCmd.Parameters.AddWithValue("@accountType", request.AccountType);
                insertUserCmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));

                var userId = Convert.ToInt32(insertUserCmd.ExecuteScalar());

                return Ok(new { success = true, message = "User created successfully", userId = userId });
            }
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, error = "Please enter email and password." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var getUserCmd = connection.CreateCommand();
                getUserCmd.CommandText = @"
                    SELECT Id, FirstName, LastName, Email, AccountType 
                    FROM Users 
                    WHERE Email = @email AND Password = @password"; // In a real app, compare hashed passwords!
                getUserCmd.Parameters.AddWithValue("@email", request.Email);
                getUserCmd.Parameters.AddWithValue("@password", request.Password);

                using (var reader = getUserCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var userId = reader.GetInt32(0);
                        var firstName = reader.GetString(1);
                        var lastName = reader.GetString(2);
                        var email = reader.GetString(3);
                        var accountType = reader.GetString(4);

                        // Create session
                        var sessionToken = GenerateSessionToken();
                        var expiresAt = DateTime.UtcNow.AddHours(24); // 24-hour session

                        var insertSessionCmd = connection.CreateCommand();
                        insertSessionCmd.CommandText = @"
                            INSERT INTO Sessions (UserId, SessionToken, CreatedAt, ExpiresAt)
                            VALUES (@userId, @sessionToken, @createdAt, @expiresAt)";
                        insertSessionCmd.Parameters.AddWithValue("@userId", userId);
                        insertSessionCmd.Parameters.AddWithValue("@sessionToken", sessionToken);
                        insertSessionCmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
                        insertSessionCmd.Parameters.AddWithValue("@expiresAt", expiresAt.ToString("o"));
                        insertSessionCmd.ExecuteNonQuery();

                        return Ok(new
                        {
                            success = true,
                            user = new { id = userId, firstName, lastName, email, accountType },
                            sessionToken
                        });
                    }
                    else
                    {
                        return Unauthorized(new { success = false, error = "Invalid email or password." });
                    }
                }
            }
        }

        [HttpPost("validate-session")]
        public IActionResult ValidateSession([FromBody] SessionValidationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return Unauthorized(new { success = false, error = "Session token is missing." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var validateSessionCmd = connection.CreateCommand();
                validateSessionCmd.CommandText = @"
                    SELECT u.Id, u.FirstName, u.LastName, u.Email, u.AccountType
                    FROM Users u
                    JOIN Sessions s ON u.Id = s.UserId
                    WHERE s.SessionToken = @sessionToken AND s.ExpiresAt > @currentTime";
                validateSessionCmd.Parameters.AddWithValue("@sessionToken", request.SessionToken);
                validateSessionCmd.Parameters.AddWithValue("@currentTime", DateTime.UtcNow.ToString("o"));

                using (var reader = validateSessionCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var userId = reader.GetInt32(0);
                        var firstName = reader.GetString(1);
                        var lastName = reader.GetString(2);
                        var email = reader.GetString(3);
                        var accountType = reader.GetString(4);

                        return Ok(new
                        {
                            success = true,
                            user = new { id = userId, firstName, lastName, email, accountType }
                        });
                    }
                    else
                    {
                        return Unauthorized(new { success = false, error = "Session expired or invalid." });
                    }
                }
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout([FromBody] LogoutRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return BadRequest(new { success = false, error = "Session token is missing." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var deleteSessionCmd = connection.CreateCommand();
                deleteSessionCmd.CommandText = "DELETE FROM Sessions WHERE SessionToken = @sessionToken";
                deleteSessionCmd.Parameters.AddWithValue("@sessionToken", request.SessionToken);
                deleteSessionCmd.ExecuteNonQuery();

                return Ok(new { success = true, message = "Logged out successfully." });
            }
        }

        private string GenerateSessionToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }

    // DTOs for requests
    public class SignupRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string AccountType { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class SessionValidationRequest
    {
        public string SessionToken { get; set; }
    }

    public class LogoutRequest
    {
        public string SessionToken { get; set; }
    }
}