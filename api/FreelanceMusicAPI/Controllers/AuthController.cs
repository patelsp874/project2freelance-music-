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
                
                // Create Users table
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

                // Create Sessions table
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

                // Create TeacherProfiles table
                var createTeacherProfilesTableCmd = connection.CreateCommand();
                createTeacherProfilesTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS TeacherProfiles (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        UserId INTEGER NOT NULL,
                        Name TEXT NOT NULL,
                        Instrument TEXT NOT NULL,
                        Bio TEXT NOT NULL,
                        ContactInfo TEXT NOT NULL,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_TeacherProfiles_UserId ON TeacherProfiles (UserId);
                    CREATE UNIQUE INDEX IF NOT EXISTS IX_TeacherProfiles_UserId_Unique ON TeacherProfiles (UserId);
                ";
                createTeacherProfilesTableCmd.ExecuteNonQuery();

                // Create TeacherAvailability table
                var createTeacherAvailabilityTableCmd = connection.CreateCommand();
                createTeacherAvailabilityTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS TeacherAvailability (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TeacherId INTEGER NOT NULL,
                        DayOfWeek TEXT NOT NULL,
                        StartTime TEXT NOT NULL,
                        EndTime TEXT NOT NULL,
                        IsAvailable BOOLEAN NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        FOREIGN KEY (TeacherId) REFERENCES Users (Id) ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS IX_TeacherAvailability_TeacherId ON TeacherAvailability (TeacherId);
                    CREATE INDEX IF NOT EXISTS IX_TeacherAvailability_DayOfWeek ON TeacherAvailability (DayOfWeek);
                ";
                createTeacherAvailabilityTableCmd.ExecuteNonQuery();

                // Create Lessons table
                var createLessonsTableCmd = connection.CreateCommand();
                createLessonsTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Lessons (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TeacherId INTEGER NOT NULL,
                        StudentId INTEGER,
                        StudentName TEXT NOT NULL,
                        Instrument TEXT NOT NULL,
                        LessonDate TEXT NOT NULL,
                        LessonTime TEXT NOT NULL,
                        LessonType TEXT NOT NULL,
                        LessonMaterial TEXT,
                        MaterialFileName TEXT,
                        MaterialFileSize INTEGER,
                        MaterialMimeType TEXT,
                        Status TEXT NOT NULL DEFAULT 'scheduled',
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL,
                        FOREIGN KEY (TeacherId) REFERENCES Users (Id) ON DELETE CASCADE,
                        FOREIGN KEY (StudentId) REFERENCES Users (Id) ON DELETE SET NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_Lessons_TeacherId ON Lessons (TeacherId);
                    CREATE INDEX IF NOT EXISTS IX_Lessons_StudentId ON Lessons (StudentId);
                    CREATE INDEX IF NOT EXISTS IX_Lessons_LessonDate ON Lessons (LessonDate);
                ";
                createLessonsTableCmd.ExecuteNonQuery();
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
                    SELECT last_insert_rowid();";

                insertUserCmd.Parameters.AddWithValue("@firstName", request.FirstName);
                insertUserCmd.Parameters.AddWithValue("@lastName", request.LastName);
                insertUserCmd.Parameters.AddWithValue("@email", request.Email);
                insertUserCmd.Parameters.AddWithValue("@password", request.Password);
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
                    WHERE Email = @email AND Password = @password";
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
                        var expiresAt = DateTime.UtcNow.AddHours(24);

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

        // ===========================================
        // TEACHER PROFILE ENDPOINTS
        // ===========================================

        [HttpPost("teacher-profile/create")]
        public IActionResult CreateTeacherProfile([FromBody] CreateTeacherProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return Unauthorized(new { success = false, error = "Session token is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Instrument) ||
                string.IsNullOrWhiteSpace(request.Bio) || string.IsNullOrWhiteSpace(request.ContactInfo))
            {
                return BadRequest(new { success = false, error = "Please fill in all required fields (name, instrument, bio, contact info)." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var validateSessionCmd = connection.CreateCommand();
                validateSessionCmd.CommandText = @"
                    SELECT u.Id, u.AccountType
                    FROM Users u
                    JOIN Sessions s ON u.Id = s.UserId
                    WHERE s.SessionToken = @sessionToken AND s.ExpiresAt > @currentTime";
                validateSessionCmd.Parameters.AddWithValue("@sessionToken", request.SessionToken);
                validateSessionCmd.Parameters.AddWithValue("@currentTime", DateTime.UtcNow.ToString("o"));

                using (var reader = validateSessionCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return Unauthorized(new { success = false, error = "Invalid or expired session." });
                    }

                    var userId = reader.GetInt32(0);
                    var accountType = reader.GetString(1);

                    reader.Close();

                    // Check if teacher profile already exists
                    var checkProfileCmd = connection.CreateCommand();
                    checkProfileCmd.CommandText = "SELECT COUNT(*) FROM TeacherProfiles WHERE UserId = @userId";
                    checkProfileCmd.Parameters.AddWithValue("@userId", userId);
                    if (Convert.ToInt32(checkProfileCmd.ExecuteScalar()) > 0)
                    {
                        return BadRequest(new { success = false, error = "Teacher profile already exists. Use update endpoint instead." });
                    }

                    // Create teacher profile
                    var createProfileCmd = connection.CreateCommand();
                    createProfileCmd.CommandText = @"
                        INSERT INTO TeacherProfiles (UserId, Name, Instrument, Bio, ContactInfo, CreatedAt, UpdatedAt)
                        VALUES (@userId, @name, @instrument, @bio, @contactInfo, @createdAt, @updatedAt);
                        SELECT last_insert_rowid();";

                    var now = DateTime.UtcNow.ToString("o");
                    createProfileCmd.Parameters.AddWithValue("@userId", userId);
                    createProfileCmd.Parameters.AddWithValue("@name", request.Name);
                    createProfileCmd.Parameters.AddWithValue("@instrument", request.Instrument);
                    createProfileCmd.Parameters.AddWithValue("@bio", request.Bio);
                    createProfileCmd.Parameters.AddWithValue("@contactInfo", request.ContactInfo);
                    createProfileCmd.Parameters.AddWithValue("@createdAt", now);
                    createProfileCmd.Parameters.AddWithValue("@updatedAt", now);

                    var profileId = Convert.ToInt32(createProfileCmd.ExecuteScalar());

                    return Ok(new { success = true, message = "Teacher profile created successfully", profileId = profileId });
                }
            }
        }

        [HttpPost("teacher-profile/get")]
        public IActionResult GetTeacherProfile([FromBody] GetTeacherProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return Unauthorized(new { success = false, error = "Session token is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var validateSessionCmd = connection.CreateCommand();
                validateSessionCmd.CommandText = @"
                    SELECT u.Id, u.AccountType
                    FROM Users u
                    JOIN Sessions s ON u.Id = s.UserId
                    WHERE s.SessionToken = @sessionToken AND s.ExpiresAt > @currentTime";
                validateSessionCmd.Parameters.AddWithValue("@sessionToken", request.SessionToken);
                validateSessionCmd.Parameters.AddWithValue("@currentTime", DateTime.UtcNow.ToString("o"));

                using (var reader = validateSessionCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return Unauthorized(new { success = false, error = "Invalid or expired session." });
                    }

                    var userId = reader.GetInt32(0);
                    reader.Close();

                    var getProfileCmd = connection.CreateCommand();
                    getProfileCmd.CommandText = @"
                        SELECT Id, Name, Instrument, Bio, ContactInfo, CreatedAt, UpdatedAt
                        FROM TeacherProfiles
                        WHERE UserId = @userId";
                    getProfileCmd.Parameters.AddWithValue("@userId", userId);

                    using (var profileReader = getProfileCmd.ExecuteReader())
                    {
                        if (profileReader.Read())
                        {
                            var profile = new
                            {
                                id = profileReader.GetInt32(0),
                                name = profileReader.GetString(1),
                                instrument = profileReader.GetString(2),
                                bio = profileReader.GetString(3),
                                contactInfo = profileReader.GetString(4),
                                createdAt = profileReader.GetString(5),
                                updatedAt = profileReader.GetString(6)
                            };

                            return Ok(new { success = true, profile = profile });
                        }
                        else
                        {
                            return NotFound(new { success = false, error = "Teacher profile not found." });
                        }
                    }
                }
            }
        }

        // ===========================================
        // TEACHER AVAILABILITY ENDPOINTS
        // ===========================================

        [HttpPost("teacher-availability/get")]
        public IActionResult GetTeacherAvailability([FromBody] GetTeacherAvailabilityRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return Unauthorized(new { success = false, error = "Session token is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var validateSessionCmd = connection.CreateCommand();
                validateSessionCmd.CommandText = @"
                    SELECT u.Id, u.AccountType
                    FROM Users u
                    JOIN Sessions s ON u.Id = s.UserId
                    WHERE s.SessionToken = @sessionToken AND s.ExpiresAt > @currentTime";
                validateSessionCmd.Parameters.AddWithValue("@sessionToken", request.SessionToken);
                validateSessionCmd.Parameters.AddWithValue("@currentTime", DateTime.UtcNow.ToString("o"));

                using (var reader = validateSessionCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return Unauthorized(new { success = false, error = "Invalid or expired session." });
                    }

                    var userId = reader.GetInt32(0);
                    reader.Close();

                    var getAvailabilityCmd = connection.CreateCommand();
                    getAvailabilityCmd.CommandText = @"
                        SELECT DayOfWeek, StartTime, EndTime, IsAvailable
                        FROM TeacherAvailability
                        WHERE TeacherId = @teacherId
                        ORDER BY 
                            CASE DayOfWeek 
                                WHEN 'Monday' THEN 1
                                WHEN 'Tuesday' THEN 2
                                WHEN 'Wednesday' THEN 3
                                WHEN 'Thursday' THEN 4
                                WHEN 'Friday' THEN 5
                                WHEN 'Saturday' THEN 6
                                WHEN 'Sunday' THEN 7
                            END";
                    getAvailabilityCmd.Parameters.AddWithValue("@teacherId", userId);

                    var availability = new List<object>();
                    using (var availabilityReader = getAvailabilityCmd.ExecuteReader())
                    {
                        while (availabilityReader.Read())
                        {
                            availability.Add(new
                            {
                                dayOfWeek = availabilityReader.GetString(0),
                                startTime = availabilityReader.GetString(1),
                                endTime = availabilityReader.GetString(2),
                                isAvailable = availabilityReader.GetBoolean(3)
                            });
                        }
                    }

                    return Ok(new { success = true, availability = availability });
                }
            }
        }

        [HttpPost("teacher-availability/set")]
        public IActionResult SetTeacherAvailability([FromBody] SetTeacherAvailabilityRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return Unauthorized(new { success = false, error = "Session token is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var validateSessionCmd = connection.CreateCommand();
                validateSessionCmd.CommandText = @"
                    SELECT u.Id, u.AccountType
                    FROM Users u
                    JOIN Sessions s ON u.Id = s.UserId
                    WHERE s.SessionToken = @sessionToken AND s.ExpiresAt > @currentTime";
                validateSessionCmd.Parameters.AddWithValue("@sessionToken", request.SessionToken);
                validateSessionCmd.Parameters.AddWithValue("@currentTime", DateTime.UtcNow.ToString("o"));

                using (var reader = validateSessionCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return Unauthorized(new { success = false, error = "Invalid or expired session." });
                    }

                    var userId = reader.GetInt32(0);
                    reader.Close();

                    // Clear existing availability
                    var clearAvailabilityCmd = connection.CreateCommand();
                    clearAvailabilityCmd.CommandText = "DELETE FROM TeacherAvailability WHERE TeacherId = @teacherId";
                    clearAvailabilityCmd.Parameters.AddWithValue("@teacherId", userId);
                    clearAvailabilityCmd.ExecuteNonQuery();

                    // Insert new availability
                    var now = DateTime.UtcNow.ToString("o");
                    foreach (var slot in request.Availability)
                    {
                        var insertAvailabilityCmd = connection.CreateCommand();
                        insertAvailabilityCmd.CommandText = @"
                            INSERT INTO TeacherAvailability (TeacherId, DayOfWeek, StartTime, EndTime, IsAvailable, CreatedAt, UpdatedAt)
                            VALUES (@teacherId, @dayOfWeek, @startTime, @endTime, @isAvailable, @createdAt, @updatedAt)";
                        insertAvailabilityCmd.Parameters.AddWithValue("@teacherId", userId);
                        insertAvailabilityCmd.Parameters.AddWithValue("@dayOfWeek", slot.DayOfWeek);
                        insertAvailabilityCmd.Parameters.AddWithValue("@startTime", slot.StartTime);
                        insertAvailabilityCmd.Parameters.AddWithValue("@endTime", slot.EndTime);
                        insertAvailabilityCmd.Parameters.AddWithValue("@isAvailable", slot.IsAvailable);
                        insertAvailabilityCmd.Parameters.AddWithValue("@createdAt", now);
                        insertAvailabilityCmd.Parameters.AddWithValue("@updatedAt", now);
                        insertAvailabilityCmd.ExecuteNonQuery();
                    }

                    return Ok(new { success = true, message = "Availability updated successfully" });
                }
            }
        }

        // ===========================================
        // STUDENT-TEACHER INTERCONNECTION ENDPOINTS
        // ===========================================

        [HttpPost("teachers/list")]
        public IActionResult GetAvailableTeachers([FromBody] GetAvailableTeachersRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return Unauthorized(new { success = false, error = "Session token is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var validateSessionCmd = connection.CreateCommand();
                validateSessionCmd.CommandText = @"
                    SELECT u.Id, u.AccountType
                    FROM Users u
                    JOIN Sessions s ON u.Id = s.UserId
                    WHERE s.SessionToken = @sessionToken AND s.ExpiresAt > @currentTime";
                validateSessionCmd.Parameters.AddWithValue("@sessionToken", request.SessionToken);
                validateSessionCmd.Parameters.AddWithValue("@currentTime", DateTime.UtcNow.ToString("o"));

                using (var reader = validateSessionCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return Unauthorized(new { success = false, error = "Invalid or expired session." });
                    }

                    reader.Close();

                    var getTeachersCmd = connection.CreateCommand();
                    var whereClause = "";
                    var parameters = new List<SqliteParameter>();

                    if (!string.IsNullOrWhiteSpace(request.Instrument))
                    {
                        whereClause = "WHERE tp.Instrument LIKE @instrument";
                        parameters.Add(new SqliteParameter("@instrument", $"%{request.Instrument}%"));
                    }

                    getTeachersCmd.CommandText = $@"
                        SELECT tp.UserId, tp.Name, tp.Instrument, tp.Bio, tp.ContactInfo,
                               COUNT(ta.Id) as AvailabilityCount
                        FROM TeacherProfiles tp
                        LEFT JOIN TeacherAvailability ta ON tp.UserId = ta.TeacherId AND ta.IsAvailable = 1
                        {whereClause}
                        GROUP BY tp.UserId, tp.Name, tp.Instrument, tp.Bio, tp.ContactInfo
                        HAVING AvailabilityCount > 0
                        ORDER BY tp.Name";

                    foreach (var param in parameters)
                    {
                        getTeachersCmd.Parameters.Add(param);
                    }

                    var teachers = new List<object>();
                    using (var teachersReader = getTeachersCmd.ExecuteReader())
                    {
                        while (teachersReader.Read())
                        {
                            teachers.Add(new
                            {
                                userId = teachersReader.GetInt32(0),
                                name = teachersReader.GetString(1),
                                instrument = teachersReader.GetString(2),
                                bio = teachersReader.GetString(3),
                                contactInfo = teachersReader.GetString(4),
                                availabilityCount = teachersReader.GetInt32(5)
                            });
                        }
                    }

                    return Ok(new { success = true, teachers = teachers });
                }
            }
        }

        [HttpPost("teacher-schedule/get")]
        public IActionResult GetTeacherSchedule([FromBody] GetTeacherScheduleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return Unauthorized(new { success = false, error = "Session token is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var validateSessionCmd = connection.CreateCommand();
                validateSessionCmd.CommandText = @"
                    SELECT u.Id, u.AccountType
                    FROM Users u
                    JOIN Sessions s ON u.Id = s.UserId
                    WHERE s.SessionToken = @sessionToken AND s.ExpiresAt > @currentTime";
                validateSessionCmd.Parameters.AddWithValue("@sessionToken", request.SessionToken);
                validateSessionCmd.Parameters.AddWithValue("@currentTime", DateTime.UtcNow.ToString("o"));

                using (var reader = validateSessionCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return Unauthorized(new { success = false, error = "Invalid or expired session." });
                    }

                    reader.Close();

                    // Get teacher availability
                    var getAvailabilityCmd = connection.CreateCommand();
                    getAvailabilityCmd.CommandText = @"
                        SELECT DayOfWeek, StartTime, EndTime
                        FROM TeacherAvailability
                        WHERE TeacherId = @teacherId AND IsAvailable = 1
                        ORDER BY 
                            CASE DayOfWeek 
                                WHEN 'Monday' THEN 1
                                WHEN 'Tuesday' THEN 2
                                WHEN 'Wednesday' THEN 3
                                WHEN 'Thursday' THEN 4
                                WHEN 'Friday' THEN 5
                                WHEN 'Saturday' THEN 6
                                WHEN 'Sunday' THEN 7
                            END";
                    getAvailabilityCmd.Parameters.AddWithValue("@teacherId", request.TeacherId);

                    var availability = new List<object>();
                    using (var availabilityReader = getAvailabilityCmd.ExecuteReader())
                    {
                        while (availabilityReader.Read())
                        {
                            availability.Add(new
                            {
                                dayOfWeek = availabilityReader.GetString(0),
                                startTime = availabilityReader.GetString(1),
                                endTime = availabilityReader.GetString(2)
                            });
                        }
                    }

                    // Get existing lessons for this teacher
                    var getLessonsCmd = connection.CreateCommand();
                    getLessonsCmd.CommandText = @"
                        SELECT LessonDate, LessonTime, StudentName, Instrument, LessonType
                        FROM Lessons
                        WHERE TeacherId = @teacherId AND Status = 'scheduled'
                        ORDER BY LessonDate, LessonTime";
                    getLessonsCmd.Parameters.AddWithValue("@teacherId", request.TeacherId);

                    var lessons = new List<object>();
                    using (var lessonsReader = getLessonsCmd.ExecuteReader())
                    {
                        while (lessonsReader.Read())
                        {
                            lessons.Add(new
                            {
                                lessonDate = lessonsReader.GetString(0),
                                lessonTime = lessonsReader.GetString(1),
                                studentName = lessonsReader.GetString(2),
                                instrument = lessonsReader.GetString(3),
                                lessonType = lessonsReader.GetString(4)
                            });
                        }
                    }

                    return Ok(new { success = true, availability = availability, lessons = lessons });
                }
            }
        }

        // ===========================================
        // LESSON MANAGEMENT ENDPOINTS
        // ===========================================

        [HttpPost("lessons/get")]
        public IActionResult GetLessons([FromBody] GetLessonsRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return Unauthorized(new { success = false, error = "Session token is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var validateSessionCmd = connection.CreateCommand();
                validateSessionCmd.CommandText = @"
                    SELECT u.Id, u.AccountType
                    FROM Users u
                    JOIN Sessions s ON u.Id = s.UserId
                    WHERE s.SessionToken = @sessionToken AND s.ExpiresAt > @currentTime";
                validateSessionCmd.Parameters.AddWithValue("@sessionToken", request.SessionToken);
                validateSessionCmd.Parameters.AddWithValue("@currentTime", DateTime.UtcNow.ToString("o"));

                using (var reader = validateSessionCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return Unauthorized(new { success = false, error = "Invalid or expired session." });
                    }

                    var userId = reader.GetInt32(0);
                    var accountType = reader.GetString(1);
                    reader.Close();

                    var getLessonsCmd = connection.CreateCommand();
                    if (accountType.ToLower() == "teacher")
                    {
                        getLessonsCmd.CommandText = @"
                            SELECT Id, StudentName, Instrument, LessonDate, LessonTime, LessonType, 
                                   LessonMaterial, MaterialFileName, MaterialFileSize, MaterialMimeType, Status
                            FROM Lessons
                            WHERE TeacherId = @userId
                            ORDER BY LessonDate DESC, LessonTime DESC";
                    }
                    else
                    {
                        getLessonsCmd.CommandText = @"
                            SELECT l.Id, l.StudentName, l.Instrument, l.LessonDate, l.LessonTime, l.LessonType, 
                                   l.LessonMaterial, l.MaterialFileName, l.MaterialFileSize, l.MaterialMimeType, l.Status,
                                   tp.Name as TeacherName
                            FROM Lessons l
                            JOIN TeacherProfiles tp ON l.TeacherId = tp.UserId
                            WHERE l.StudentId = @userId
                            ORDER BY l.LessonDate DESC, l.LessonTime DESC";
                    }
                    getLessonsCmd.Parameters.AddWithValue("@userId", userId);

                    var lessons = new List<object>();
                    using (var lessonsReader = getLessonsCmd.ExecuteReader())
                    {
                        while (lessonsReader.Read())
                        {
                            var lesson = new
                            {
                                id = lessonsReader.GetInt32(0),
                                studentName = lessonsReader.GetString(1),
                                instrument = lessonsReader.GetString(2),
                                lessonDate = lessonsReader.GetString(3),
                                lessonTime = lessonsReader.GetString(4),
                                lessonType = lessonsReader.GetString(5),
                                lessonMaterial = lessonsReader.IsDBNull(6) ? null : lessonsReader.GetString(6),
                                materialFileName = lessonsReader.IsDBNull(7) ? null : lessonsReader.GetString(7),
                                materialFileSize = lessonsReader.IsDBNull(8) ? null : (int?)lessonsReader.GetInt32(8),
                                materialMimeType = lessonsReader.IsDBNull(9) ? null : lessonsReader.GetString(9),
                                status = lessonsReader.GetString(10),
                                teacherName = accountType.ToLower() == "student" && !lessonsReader.IsDBNull(11) ? lessonsReader.GetString(11) : null
                            };

                            lessons.Add(lesson);
                        }
                    }

                    return Ok(new { success = true, lessons = lessons });
                }
            }
        }

        [HttpPost("lessons/book")]
        public IActionResult BookLesson([FromBody] BookLessonRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken))
            {
                return Unauthorized(new { success = false, error = "Session token is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var validateSessionCmd = connection.CreateCommand();
                validateSessionCmd.CommandText = @"
                    SELECT u.Id, u.AccountType, u.FirstName, u.LastName
                    FROM Users u
                    JOIN Sessions s ON u.Id = s.UserId
                    WHERE s.SessionToken = @sessionToken AND s.ExpiresAt > @currentTime";
                validateSessionCmd.Parameters.AddWithValue("@sessionToken", request.SessionToken);
                validateSessionCmd.Parameters.AddWithValue("@currentTime", DateTime.UtcNow.ToString("o"));

                using (var reader = validateSessionCmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return Unauthorized(new { success = false, error = "Invalid or expired session." });
                    }

                    var userId = reader.GetInt32(0);
                    var accountType = reader.GetString(1);
                    var firstName = reader.GetString(2);
                    var lastName = reader.GetString(3);
                    reader.Close();

                    // Check if there's a conflict
                    var checkConflictCmd = connection.CreateCommand();
                    checkConflictCmd.CommandText = @"
                        SELECT COUNT(*) FROM Lessons 
                        WHERE TeacherId = @teacherId AND LessonDate = @lessonDate AND LessonTime = @lessonTime AND Status = 'scheduled'";
                    checkConflictCmd.Parameters.AddWithValue("@teacherId", request.TeacherId);
                    checkConflictCmd.Parameters.AddWithValue("@lessonDate", request.LessonDate);
                    checkConflictCmd.Parameters.AddWithValue("@lessonTime", request.LessonTime);

                    if (Convert.ToInt32(checkConflictCmd.ExecuteScalar()) > 0)
                    {
                        return BadRequest(new { success = false, error = "This time slot is already booked." });
                    }

                    // Book the lesson
                    var bookLessonCmd = connection.CreateCommand();
                    bookLessonCmd.CommandText = @"
                        INSERT INTO Lessons (TeacherId, StudentId, StudentName, Instrument, LessonDate, LessonTime, LessonType, Status, CreatedAt, UpdatedAt)
                        VALUES (@teacherId, @studentId, @studentName, @instrument, @lessonDate, @lessonTime, @lessonType, 'scheduled', @createdAt, @updatedAt);
                        SELECT last_insert_rowid();";

                    var now = DateTime.UtcNow.ToString("o");
                    bookLessonCmd.Parameters.AddWithValue("@teacherId", request.TeacherId);
                    bookLessonCmd.Parameters.AddWithValue("@studentId", userId);
                    bookLessonCmd.Parameters.AddWithValue("@studentName", $"{firstName} {lastName}");
                    bookLessonCmd.Parameters.AddWithValue("@instrument", request.Instrument);
                    bookLessonCmd.Parameters.AddWithValue("@lessonDate", request.LessonDate);
                    bookLessonCmd.Parameters.AddWithValue("@lessonTime", request.LessonTime);
                    bookLessonCmd.Parameters.AddWithValue("@lessonType", request.LessonType);
                    bookLessonCmd.Parameters.AddWithValue("@createdAt", now);
                    bookLessonCmd.Parameters.AddWithValue("@updatedAt", now);

                    var lessonId = Convert.ToInt32(bookLessonCmd.ExecuteScalar());

                    return Ok(new { success = true, message = "Lesson booked successfully", lessonId = lessonId });
                }
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
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? AccountType { get; set; }
    }

    public class LoginRequest
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class SessionValidationRequest
    {
        public string? SessionToken { get; set; }
    }

    public class LogoutRequest
    {
        public string? SessionToken { get; set; }
    }

    public class CreateTeacherProfileRequest
    {
        public string? SessionToken { get; set; }
        public string? Name { get; set; }
        public string? Instrument { get; set; }
        public string? Bio { get; set; }
        public string? ContactInfo { get; set; }
    }

    public class GetTeacherProfileRequest
    {
        public string? SessionToken { get; set; }
    }

    public class GetTeacherAvailabilityRequest
    {
        public string? SessionToken { get; set; }
    }

    public class SetTeacherAvailabilityRequest
    {
        public string? SessionToken { get; set; }
        public List<AvailabilitySlot>? Availability { get; set; }
    }

    public class AvailabilitySlot
    {
        public string? DayOfWeek { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class GetAvailableTeachersRequest
    {
        public string? SessionToken { get; set; }
        public string? Instrument { get; set; }
    }

    public class GetTeacherScheduleRequest
    {
        public string? SessionToken { get; set; }
        public int TeacherId { get; set; }
    }

    public class GetLessonsRequest
    {
        public string? SessionToken { get; set; }
    }

    public class BookLessonRequest
    {
        public string? SessionToken { get; set; }
        public int TeacherId { get; set; }
        public string? Instrument { get; set; }
        public string? LessonDate { get; set; }
        public string? LessonTime { get; set; }
        public string? LessonType { get; set; }
    }
}