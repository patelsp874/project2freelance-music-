using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using BCrypt.Net;

namespace FreelanceMusicAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=freelancemusic.db";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();
                
                // Enable foreign key constraints
                var pragmaCmd = connection.CreateCommand();
                pragmaCmd.CommandText = "PRAGMA foreign_keys = ON;";
                pragmaCmd.ExecuteNonQuery();

                // Create Student table (matching your schema)
                var createStudentTableCmd = connection.CreateCommand();
                createStudentTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Student (
                        student_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        student_name TEXT NOT NULL,
                        student_email TEXT NOT NULL UNIQUE,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        studentpassword TEXT
                    );";
                createStudentTableCmd.ExecuteNonQuery();

                // Create Teacher table (matching your schema with current_class and password added)
                var createTeacherTableCmd = connection.CreateCommand();
                createTeacherTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Teacher (
                        teacher_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        teacher_name TEXT NOT NULL,
                        teacher_email TEXT NOT NULL UNIQUE,
                        teacher_password TEXT,
                        instrument TEXT NOT NULL,
                        class_full INTEGER DEFAULT 0 CHECK (class_full IN (0, 1)),
                        class_limit INTEGER DEFAULT 10,
                        current_class INTEGER DEFAULT 0,
                        bio TEXT,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                    );";
                createTeacherTableCmd.ExecuteNonQuery();

                // Add teacherpassword column if it doesn't exist (for existing databases)
                try
                {
                    var alterTeacherTableCmd = connection.CreateCommand();
                    alterTeacherTableCmd.CommandText = "ALTER TABLE Teacher ADD COLUMN teacher_password INTEGER UNIQUE;";
                    alterTeacherTableCmd.ExecuteNonQuery();
                }
                catch (SqliteException)
                {
                    // Column already exists, ignore the error
                }

                // Create Student_Studying table (matching your schema)
                var createStudentStudyingTableCmd = connection.CreateCommand();
                createStudentStudyingTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Student_Studying (
                        student_id INTEGER NOT NULL,
                        teacher_id INTEGER NOT NULL,
                        day TEXT NOT NULL CHECK (day IN ('Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday')),
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        PRIMARY KEY (student_id, teacher_id, day),
                        FOREIGN KEY (student_id) REFERENCES Student(student_id) ON DELETE CASCADE,
                        FOREIGN KEY (teacher_id) REFERENCES Teacher(teacher_id) ON DELETE CASCADE
                    );";
                createStudentStudyingTableCmd.ExecuteNonQuery();

                // Create Teacher_Day_Availability table (matching your schema)
                var createTeacherAvailabilityTableCmd = connection.CreateCommand();
                createTeacherAvailabilityTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Teacher_Day_Availability (
                        teacher_id INTEGER NOT NULL,
                        day TEXT NOT NULL CHECK (day IN ('Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday')),
                        available INTEGER DEFAULT 1 CHECK (available IN (0, 1)),
                        start_time TIME,
                        end_time TIME,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        PRIMARY KEY (teacher_id, day),
                        FOREIGN KEY (teacher_id) REFERENCES Teacher(teacher_id) ON DELETE CASCADE
                    );";
                createTeacherAvailabilityTableCmd.ExecuteNonQuery();

                // Create Payment table
                var createPaymentTableCmd = connection.CreateCommand();
                createPaymentTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Payment (
                        payment_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        student_id INTEGER NOT NULL,
                        teacher_id INTEGER NOT NULL,
                        lesson_day TEXT NOT NULL,
                        amount DECIMAL(10,2) NOT NULL,
                        payment_method TEXT NOT NULL CHECK (payment_method IN ('Credit Card', 'Debit Card', 'PayPal', 'Bank Transfer')),
                        payment_status TEXT NOT NULL DEFAULT 'Pending' CHECK (payment_status IN ('Pending', 'Completed', 'Failed', 'Refunded')),
                        transaction_id TEXT UNIQUE,
                        payment_date DATETIME DEFAULT CURRENT_TIMESTAMP,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (student_id) REFERENCES Student(student_id) ON DELETE CASCADE,
                        FOREIGN KEY (teacher_id) REFERENCES Teacher(teacher_id) ON DELETE CASCADE
                    );";
                createPaymentTableCmd.ExecuteNonQuery();

                // Create indexes for better performance
                var createIndexesCmd = connection.CreateCommand();
                createIndexesCmd.CommandText = @"
                    CREATE INDEX IF NOT EXISTS idx_student_email ON Student(student_email);
                    CREATE INDEX IF NOT EXISTS idx_teacher_email ON Teacher(teacher_email);
                    CREATE INDEX IF NOT EXISTS idx_teacher_instrument ON Teacher(instrument);
                    CREATE INDEX IF NOT EXISTS idx_student_studying_student ON Student_Studying(student_id);
                    CREATE INDEX IF NOT EXISTS idx_student_studying_teacher ON Student_Studying(teacher_id);
                    CREATE INDEX IF NOT EXISTS idx_teacher_availability_teacher ON Teacher_Day_Availability(teacher_id);
                    CREATE INDEX IF NOT EXISTS idx_teacher_availability_day ON Teacher_Day_Availability(day);
                ";
                createIndexesCmd.ExecuteNonQuery();
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

                int userId;

                if (request.AccountType.ToLower() == "student")
                {
                    // Check if student email already exists
                var checkEmailCmd = connection.CreateCommand();
                    checkEmailCmd.CommandText = "SELECT COUNT(*) FROM Student WHERE student_email = @email";
                checkEmailCmd.Parameters.AddWithValue("@email", request.Email);
                if (Convert.ToInt32(checkEmailCmd.ExecuteScalar()) > 0)
                {
                    return BadRequest(new { success = false, error = "Email already registered." });
                }

                    // Insert new student
                    var insertStudentCmd = connection.CreateCommand();
                    insertStudentCmd.CommandText = @"
                        INSERT INTO Student (student_name, student_email, studentpassword, created_at, updated_at)
                        VALUES (@name, @email, @password, @createdAt, @updatedAt);
                    SELECT last_insert_rowid();";

                    var now = DateTime.UtcNow.ToString("o");
                    var fullName = $"{request.FirstName} {request.LastName}";
                    insertStudentCmd.Parameters.AddWithValue("@name", fullName);
                    insertStudentCmd.Parameters.AddWithValue("@email", request.Email);
                    insertStudentCmd.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(request.Password)); // Hash password with bcrypt
                    insertStudentCmd.Parameters.AddWithValue("@createdAt", now);
                    insertStudentCmd.Parameters.AddWithValue("@updatedAt", now);

                    userId = Convert.ToInt32(insertStudentCmd.ExecuteScalar());
                }
                else if (request.AccountType.ToLower() == "teacher")
                {
                    // Check if teacher email already exists
                    var checkEmailCmd = connection.CreateCommand();
                    checkEmailCmd.CommandText = "SELECT COUNT(*) FROM Teacher WHERE teacher_email = @email";
                    checkEmailCmd.Parameters.AddWithValue("@email", request.Email);
                    if (Convert.ToInt32(checkEmailCmd.ExecuteScalar()) > 0)
                    {
                        return BadRequest(new { success = false, error = "Email already registered." });
                    }

                    // Insert new teacher
                    var insertTeacherCmd = connection.CreateCommand();
                    insertTeacherCmd.CommandText = @"
                        INSERT INTO Teacher (teacher_name, teacher_email, teacher_password, instrument, class_full, class_limit, current_class, bio, created_at, updated_at)
                        VALUES (@name, @email, @password, @instrument, 0, 10, 0, @bio, @createdAt, @updatedAt);
                        SELECT last_insert_rowid();";

                    var now = DateTime.UtcNow.ToString("o");
                    var fullName = $"{request.FirstName} {request.LastName}";
                    insertTeacherCmd.Parameters.AddWithValue("@name", fullName);
                    insertTeacherCmd.Parameters.AddWithValue("@email", request.Email);
                    insertTeacherCmd.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(request.Password)); // Hash password with bcrypt
                    insertTeacherCmd.Parameters.AddWithValue("@instrument", "Not specified"); // Default value
                    insertTeacherCmd.Parameters.AddWithValue("@bio", "New teacher profile"); // Default value
                    insertTeacherCmd.Parameters.AddWithValue("@createdAt", now);
                    insertTeacherCmd.Parameters.AddWithValue("@updatedAt", now);

                    userId = Convert.ToInt32(insertTeacherCmd.ExecuteScalar());
                    }
                    else
                    {
                    return BadRequest(new { success = false, error = "Invalid account type. Must be 'student' or 'teacher'." });
                }

                return Ok(new { 
                    success = true, 
                    message = "Account created successfully", 
                    user = new { 
                        id = userId, 
                        firstName = request.FirstName, 
                        lastName = request.LastName, 
                        email = request.Email, 
                        accountType = request.AccountType 
                    }
                });
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

                // Try to find user in Student table first
                var getStudentCmd = connection.CreateCommand();
                getStudentCmd.CommandText = @"
                    SELECT student_id, student_name, student_email, studentpassword 
                    FROM Student 
                    WHERE student_email = @email";
                getStudentCmd.Parameters.AddWithValue("@email", request.Email);

                using (var reader = getStudentCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var userId = reader.GetInt32(0);
                        var fullName = reader.GetString(1);
                        var email = reader.GetString(2);
                        
                        // Check if password is NULL (for sample data without passwords)
                        if (reader.IsDBNull(3))
                        {
                            return Unauthorized(new { success = false, error = "This account has no password set. Please sign up with a new email or contact support." });
                        }
                        
                        var storedPasswordHash = reader.GetString(3);
                        
                        // Check if password is in old integer format or new BCrypt format
                        bool passwordValid = false;
                        if (storedPasswordHash.StartsWith("$2"))
                        {
                            // New BCrypt format
                            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, storedPasswordHash);
                        }
                        else
                        {
                            // Old format - clear the password and require re-signup
                            var clearPasswordCmd = connection.CreateCommand();
                            clearPasswordCmd.CommandText = "UPDATE Student SET studentpassword = NULL WHERE student_id = @userId";
                            clearPasswordCmd.Parameters.AddWithValue("@userId", userId);
                            clearPasswordCmd.ExecuteNonQuery();
                            
                            return Unauthorized(new { success = false, error = "Password format updated. Please sign up again with a new password." });
                        }
                        
                        if (passwordValid)
                        {
                            var nameParts = fullName.Split(' ');
                            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                            return Ok(new
                            {
                                success = true,
                                user = new { id = userId, firstName, lastName, email, accountType = "student" }
                            });
                        }
                    }
                }

                // If not found in Student table, try Teacher table
                var getTeacherCmd = connection.CreateCommand();
                getTeacherCmd.CommandText = @"
                    SELECT teacher_id, teacher_name, teacher_email, teacher_password 
                    FROM Teacher 
                    WHERE teacher_email = @email";
                getTeacherCmd.Parameters.AddWithValue("@email", request.Email);

                using (var teacherReader = getTeacherCmd.ExecuteReader())
                {
                    if (teacherReader.Read())
                    {
                        var userId = teacherReader.GetInt32(0);
                        var fullName = teacherReader.GetString(1);
                        var email = teacherReader.GetString(2);
                        
                        // Check if password is NULL (for sample data without passwords)
                        if (teacherReader.IsDBNull(3))
                        {
                            return Unauthorized(new { success = false, error = "This account has no password set. Please sign up with a new email or contact support." });
                        }
                        
                        var storedPasswordHash = teacherReader.GetString(3);
                        
                        // Check if password is in old integer format or new BCrypt format
                        bool passwordValid = false;
                        if (storedPasswordHash.StartsWith("$2"))
                        {
                            // New BCrypt format
                            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, storedPasswordHash);
                        }
                        else
                        {
                            // Old format - clear the password and require re-signup
                            var clearPasswordCmd = connection.CreateCommand();
                            clearPasswordCmd.CommandText = "UPDATE Teacher SET teacher_password = NULL WHERE teacher_id = @userId";
                            clearPasswordCmd.Parameters.AddWithValue("@userId", userId);
                            clearPasswordCmd.ExecuteNonQuery();
                            
                            return Unauthorized(new { success = false, error = "Password format updated. Please sign up again with a new password." });
                        }
                        
                        if (passwordValid)
                        {
                            var nameParts = fullName.Split(' ');
                            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                            return Ok(new
                            {
                                success = true,
                                user = new { id = userId, firstName, lastName, email, accountType = "teacher" }
                            });
                        }
                        else
                        {
                            return Unauthorized(new { success = false, error = "Invalid email or password." });
                        }
                    }
                    else
                    {
                        return Unauthorized(new { success = false, error = "Invalid email or password." });
                    }
                }
            }
        }

        [HttpPost("teacher-profile/create")]
        public IActionResult CreateTeacherProfile([FromBody] CreateTeacherProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Instrument) ||
                string.IsNullOrWhiteSpace(request.Bio) || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, error = "Name, instrument, bio, and email are required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Check if teacher profile exists (should exist from signup)
                var checkProfileCmd = connection.CreateCommand();
                checkProfileCmd.CommandText = "SELECT teacher_id FROM Teacher WHERE teacher_email = @email";
                checkProfileCmd.Parameters.AddWithValue("@email", request.Email);

                var existingTeacherId = checkProfileCmd.ExecuteScalar();
                if (existingTeacherId == null)
                {
                    return BadRequest(new { success = false, error = "No teacher account found with this email. Please sign up first." });
                }

                // Update existing teacher profile instead of creating new one
                var updateProfileCmd = connection.CreateCommand();
                updateProfileCmd.CommandText = @"
                    UPDATE Teacher 
                    SET teacher_name = @name, 
                        instrument = @instrument, 
                        class_full = @classFull, 
                        class_limit = @classLimit, 
                        bio = @bio, 
                        updated_at = @updatedAt
                    WHERE teacher_email = @email";

                var now = DateTime.UtcNow.ToString("o");
                updateProfileCmd.Parameters.AddWithValue("@name", request.Name);
                updateProfileCmd.Parameters.AddWithValue("@email", request.Email);
                updateProfileCmd.Parameters.AddWithValue("@instrument", request.Instrument);
                updateProfileCmd.Parameters.AddWithValue("@classFull", request.ClassFull ?? 0);
                updateProfileCmd.Parameters.AddWithValue("@classLimit", request.ClassLimit ?? 10);
                updateProfileCmd.Parameters.AddWithValue("@bio", request.Bio);
                updateProfileCmd.Parameters.AddWithValue("@updatedAt", now);

                var rowsAffected = updateProfileCmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new { success = true, message = "Teacher profile updated successfully", teacherId = existingTeacherId });
                }
                else
                {
                    return BadRequest(new { success = false, error = "Failed to update teacher profile." });
                }
            }
        }

        [HttpPost("teacher-profile/get")]
        public IActionResult GetTeacherProfile([FromBody] GetTeacherProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, error = "Email is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                    var getProfileCmd = connection.CreateCommand();
                    getProfileCmd.CommandText = @"
                    SELECT teacher_id, teacher_name, teacher_email, instrument, class_full, class_limit, current_class, bio, created_at, updated_at
                        FROM Teacher
                        WHERE teacher_email = @email";
                getProfileCmd.Parameters.AddWithValue("@email", request.Email);

                    using (var profileReader = getProfileCmd.ExecuteReader())
                    {
                        if (profileReader.Read())
                        {
                            var profile = new
                            {
                                id = profileReader.GetInt32(0),
                                name = profileReader.GetString(1),
                                email = profileReader.GetString(2),
                                instrument = profileReader.GetString(3),
                                classFull = profileReader.GetInt32(4),
                                classLimit = profileReader.GetInt32(5),
                            currentClass = profileReader.GetInt32(6),
                            bio = profileReader.GetString(7),
                                createdAt = profileReader.GetString(8),
                                updatedAt = profileReader.GetString(9)
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

        [HttpPost("teacher-profile/update")]
        public IActionResult UpdateTeacherProfile([FromBody] UpdateTeacherProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Instrument) ||
                string.IsNullOrWhiteSpace(request.Bio) || string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, error = "Name, instrument, bio, and email are required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                    // Check if teacher profile exists
                    var checkProfileCmd = connection.CreateCommand();
                    checkProfileCmd.CommandText = "SELECT COUNT(*) FROM Teacher WHERE teacher_email = @email";
                checkProfileCmd.Parameters.AddWithValue("@email", request.Email);

                    if (Convert.ToInt32(checkProfileCmd.ExecuteScalar()) == 0)
                    {
                        return NotFound(new { success = false, error = "Teacher profile not found. Please create a profile first." });
                    }

                // Update teacher profile in Teacher table
                    var updateProfileCmd = connection.CreateCommand();
                    updateProfileCmd.CommandText = @"
                        UPDATE Teacher 
                        SET teacher_name = @name, teacher_email = @email, instrument = @instrument, 
                            class_full = @classFull, class_limit = @classLimit, bio = @bio, 
                        updated_at = @updatedAt
                        WHERE teacher_email = @originalEmail";

                    var now = DateTime.UtcNow.ToString("o");
                    updateProfileCmd.Parameters.AddWithValue("@name", request.Name);
                    updateProfileCmd.Parameters.AddWithValue("@email", request.Email);
                    updateProfileCmd.Parameters.AddWithValue("@instrument", request.Instrument);
                    updateProfileCmd.Parameters.AddWithValue("@classFull", request.ClassFull ?? 0);
                    updateProfileCmd.Parameters.AddWithValue("@classLimit", request.ClassLimit ?? 10);
                    updateProfileCmd.Parameters.AddWithValue("@bio", request.Bio);
                    updateProfileCmd.Parameters.AddWithValue("@updatedAt", now);
                updateProfileCmd.Parameters.AddWithValue("@originalEmail", request.Email);

                    updateProfileCmd.ExecuteNonQuery();

                    return Ok(new { success = true, message = "Teacher profile updated successfully" });
                }
            }

        [HttpPost("teacher-availability/add")]
        public IActionResult AddTeacherAvailability([FromBody] AddTeacherAvailabilityRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Day) || 
                string.IsNullOrWhiteSpace(request.StartTime) || string.IsNullOrWhiteSpace(request.EndTime))
            {
                return BadRequest(new { success = false, error = "Email, day, start time, and end time are required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Get teacher ID from email
                var getTeacherCmd = connection.CreateCommand();
                getTeacherCmd.CommandText = "SELECT teacher_id FROM Teacher WHERE teacher_email = @email";
                getTeacherCmd.Parameters.AddWithValue("@email", request.Email);
                var teacherId = getTeacherCmd.ExecuteScalar();

                if (teacherId == null)
                {
                    return NotFound(new { success = false, error = "Teacher not found." });
                }

                // Insert or update availability
                var addAvailabilityCmd = connection.CreateCommand();
                addAvailabilityCmd.CommandText = @"
                    INSERT OR REPLACE INTO Teacher_Day_Availability (teacher_id, day, available, start_time, end_time, created_at, updated_at)
                    VALUES (@teacherId, @day, 1, @startTime, @endTime, @createdAt, @updatedAt)";

                var now = DateTime.UtcNow.ToString("o");
                addAvailabilityCmd.Parameters.AddWithValue("@teacherId", teacherId);
                addAvailabilityCmd.Parameters.AddWithValue("@day", request.Day);
                addAvailabilityCmd.Parameters.AddWithValue("@startTime", request.StartTime);
                addAvailabilityCmd.Parameters.AddWithValue("@endTime", request.EndTime);
                addAvailabilityCmd.Parameters.AddWithValue("@createdAt", now);
                addAvailabilityCmd.Parameters.AddWithValue("@updatedAt", now);

                addAvailabilityCmd.ExecuteNonQuery();

                return Ok(new { success = true, message = "Availability added successfully" });
            }
        }

        [HttpPost("teacher-availability/get")]
        public IActionResult GetTeacherAvailability([FromBody] GetTeacherAvailabilityRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, error = "Email is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Get teacher ID from email
                var getTeacherCmd = connection.CreateCommand();
                getTeacherCmd.CommandText = "SELECT teacher_id FROM Teacher WHERE teacher_email = @email";
                getTeacherCmd.Parameters.AddWithValue("@email", request.Email);
                var teacherId = getTeacherCmd.ExecuteScalar();

                if (teacherId == null)
                {
                    return NotFound(new { success = false, error = "Teacher not found." });
                }

                // Get availability
                var getAvailabilityCmd = connection.CreateCommand();
                getAvailabilityCmd.CommandText = @"
                    SELECT day, start_time, end_time, available
                    FROM Teacher_Day_Availability
                    WHERE teacher_id = @teacherId AND available = 1
                    ORDER BY 
                        CASE day
                            WHEN 'Monday' THEN 1
                            WHEN 'Tuesday' THEN 2
                            WHEN 'Wednesday' THEN 3
                            WHEN 'Thursday' THEN 4
                            WHEN 'Friday' THEN 5
                            WHEN 'Saturday' THEN 6
                            WHEN 'Sunday' THEN 7
                        END";

                getAvailabilityCmd.Parameters.AddWithValue("@teacherId", teacherId);

                var availability = new List<object>();
                using (var availabilityReader = getAvailabilityCmd.ExecuteReader())
                {
                    while (availabilityReader.Read())
                    {
                        availability.Add(new
                        {
                            day = availabilityReader.GetString(0),
                            startTime = availabilityReader.GetString(1),
                            endTime = availabilityReader.GetString(2),
                            available = availabilityReader.GetInt32(3) == 1
                        });
                    }
                }

                return Ok(new { success = true, availability = availability });
            }
        }

        [HttpPost("teacher-availability/set")]
        public IActionResult SetTeacherAvailability([FromBody] SetTeacherAvailabilityRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { success = false, error = "Email is required." });
            }

            if (request.Availability == null || !request.Availability.Any())
            {
                return BadRequest(new { success = false, error = "Availability data is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Get teacher ID from email
                var getTeacherCmd = connection.CreateCommand();
                getTeacherCmd.CommandText = "SELECT teacher_id FROM Teacher WHERE teacher_email = @email";
                getTeacherCmd.Parameters.AddWithValue("@email", request.Email);
                var teacherId = getTeacherCmd.ExecuteScalar();

                if (teacherId == null)
                {
                    return NotFound(new { success = false, error = "Teacher not found." });
                }

                    // Clear existing availability
                    var clearAvailabilityCmd = connection.CreateCommand();
                clearAvailabilityCmd.CommandText = "DELETE FROM Teacher_Day_Availability WHERE teacher_id = @teacherId";
                clearAvailabilityCmd.Parameters.AddWithValue("@teacherId", teacherId);
                    clearAvailabilityCmd.ExecuteNonQuery();

                    // Insert new availability
                    var now = DateTime.UtcNow.ToString("o");
                    foreach (var slot in request.Availability)
                    {
                        var insertAvailabilityCmd = connection.CreateCommand();
                        insertAvailabilityCmd.CommandText = @"
                        INSERT INTO Teacher_Day_Availability (teacher_id, day, start_time, end_time, available, created_at, updated_at)
                        VALUES (@teacherId, @day, @startTime, @endTime, @isAvailable, @createdAt, @updatedAt)";
                    insertAvailabilityCmd.Parameters.AddWithValue("@teacherId", teacherId);
                    insertAvailabilityCmd.Parameters.AddWithValue("@day", slot.DayOfWeek);
                        insertAvailabilityCmd.Parameters.AddWithValue("@startTime", slot.StartTime);
                        insertAvailabilityCmd.Parameters.AddWithValue("@endTime", slot.EndTime);
                    insertAvailabilityCmd.Parameters.AddWithValue("@isAvailable", slot.IsAvailable ? 1 : 0);
                        insertAvailabilityCmd.Parameters.AddWithValue("@createdAt", now);
                        insertAvailabilityCmd.Parameters.AddWithValue("@updatedAt", now);
                        insertAvailabilityCmd.ExecuteNonQuery();
                    }

                    return Ok(new { success = true, message = "Availability updated successfully" });
            }
        }

        [HttpPost("teachers/list")]
        public IActionResult GetTeachersList([FromBody] GetTeachersListRequest request)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                    var getTeachersCmd = connection.CreateCommand();
                    var whereClause = "";
                    var parameters = new List<SqliteParameter>();

                    if (!string.IsNullOrWhiteSpace(request.Instrument))
                    {
                    whereClause = "WHERE t.instrument LIKE @instrument";
                        parameters.Add(new SqliteParameter("@instrument", $"%{request.Instrument}%"));
                    }

                    getTeachersCmd.CommandText = $@"
                    SELECT t.teacher_id, t.teacher_name, t.teacher_email, t.instrument, t.bio,
                           t.class_limit, t.current_class, t.class_full,
                           COUNT(tda.day) as AvailabilityCount
                    FROM Teacher t
                    LEFT JOIN Teacher_Day_Availability tda ON t.teacher_id = tda.teacher_id 
                        AND tda.available = 1 
                        AND t.class_full = 0
                        {whereClause}
                    GROUP BY t.teacher_id, t.teacher_name, t.teacher_email, t.instrument, t.bio, 
                             t.class_limit, t.current_class, t.class_full
                    ORDER BY t.teacher_name";

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
                            email = teachersReader.GetString(2),
                            instrument = teachersReader.GetString(3),
                            bio = teachersReader.GetString(4),
                            classLimit = teachersReader.GetInt32(5),
                            currentClass = teachersReader.GetInt32(6),
                            classFull = teachersReader.GetInt32(7),
                            availabilityCount = teachersReader.GetInt32(8),
                            isAvailable = teachersReader.GetInt32(7) == 0 // class_full = 0 means available
                            });
                        }
                    }

                    return Ok(new { success = true, teachers = teachers });
            }
        }

        [HttpPost("teacher-schedule/get")]
        public IActionResult GetTeacherSchedule([FromBody] GetTeacherScheduleRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TeacherId))
            {
                return BadRequest(new { success = false, error = "Teacher ID is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                var getScheduleCmd = connection.CreateCommand();
                getScheduleCmd.CommandText = @"
                    SELECT day, start_time, end_time, available
                    FROM Teacher_Day_Availability
                    WHERE teacher_id = @teacherId AND available = 1
                        ORDER BY 
                        CASE day
                                WHEN 'Monday' THEN 1
                                WHEN 'Tuesday' THEN 2
                                WHEN 'Wednesday' THEN 3
                                WHEN 'Thursday' THEN 4
                                WHEN 'Friday' THEN 5
                                WHEN 'Saturday' THEN 6
                                WHEN 'Sunday' THEN 7
                            END";

                getScheduleCmd.Parameters.AddWithValue("@teacherId", request.TeacherId);

                var schedule = new List<object>();
                using (var scheduleReader = getScheduleCmd.ExecuteReader())
                {
                    while (scheduleReader.Read())
                    {
                        schedule.Add(new
                        {
                            day = scheduleReader.GetString(0),
                            startTime = scheduleReader.GetString(1),
                            endTime = scheduleReader.GetString(2),
                            available = scheduleReader.GetInt32(3) == 1
                            });
                        }
                    }

                return Ok(new { success = true, schedule = schedule });
            }
        }

        [HttpPost("student-studying/add")]
        public IActionResult AddStudentStudying([FromBody] AddStudentStudyingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentEmail) || string.IsNullOrWhiteSpace(request.TeacherEmail) ||
                string.IsNullOrWhiteSpace(request.Day))
            {
                return BadRequest(new { success = false, error = "Student email, teacher email, and day are required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Get student ID
                var getStudentCmd = connection.CreateCommand();
                getStudentCmd.CommandText = "SELECT student_id FROM Student WHERE student_email = @email";
                getStudentCmd.Parameters.AddWithValue("@email", request.StudentEmail);
                var studentId = getStudentCmd.ExecuteScalar();

                if (studentId == null)
                {
                    return NotFound(new { success = false, error = "Student not found." });
                }

                // Get teacher ID
                var getTeacherCmd = connection.CreateCommand();
                getTeacherCmd.CommandText = "SELECT teacher_id FROM Teacher WHERE teacher_email = @email";
                getTeacherCmd.Parameters.AddWithValue("@email", request.TeacherEmail);
                var teacherId = getTeacherCmd.ExecuteScalar();

                if (teacherId == null)
                {
                    return NotFound(new { success = false, error = "Teacher not found." });
                }

                // Check if teacher has reached class limit
                var checkClassLimitCmd = connection.CreateCommand();
                checkClassLimitCmd.CommandText = @"
                    SELECT class_limit, current_class FROM Teacher WHERE teacher_id = @teacherId";
                checkClassLimitCmd.Parameters.AddWithValue("@teacherId", teacherId);

                using (var limitReader = checkClassLimitCmd.ExecuteReader())
                {
                    if (limitReader.Read())
                    {
                        var classLimit = limitReader.GetInt32(0);
                        var currentClass = limitReader.GetInt32(1);
                        
                        if (currentClass > classLimit)
                        {
                            return BadRequest(new { success = false, error = "This teacher has reached their class limit and is no longer available." });
                        }
                    }
                }

                // Insert student studying relationship
                var insertStudyingCmd = connection.CreateCommand();
                insertStudyingCmd.CommandText = @"
                    INSERT OR REPLACE INTO Student_Studying (student_id, teacher_id, day, created_at)
                    VALUES (@studentId, @teacherId, @day, @createdAt)";

                var now = DateTime.UtcNow.ToString("o");
                insertStudyingCmd.Parameters.AddWithValue("@studentId", studentId);
                insertStudyingCmd.Parameters.AddWithValue("@teacherId", teacherId);
                insertStudyingCmd.Parameters.AddWithValue("@day", request.Day);
                insertStudyingCmd.Parameters.AddWithValue("@createdAt", now);

                insertStudyingCmd.ExecuteNonQuery();

                // Increment teacher's current class count
                var incrementClassCmd = connection.CreateCommand();
                incrementClassCmd.CommandText = @"
                    UPDATE Teacher 
                    SET current_class = current_class + 1, 
                        class_full = CASE WHEN (current_class + 1) >= class_limit THEN 1 ELSE 0 END,
                        updated_at = @updatedAt
                    WHERE teacher_id = @teacherId";
                incrementClassCmd.Parameters.AddWithValue("@teacherId", teacherId);
                incrementClassCmd.Parameters.AddWithValue("@updatedAt", now);
                incrementClassCmd.ExecuteNonQuery();

                return Ok(new { success = true, message = "Student enrolled successfully" });
            }
        }

        [HttpPost("teacher-students/get")]
        public IActionResult GetTeacherStudents([FromBody] GetStudentStudyingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentEmail)) // Reusing the same request class
            {
                return BadRequest(new { success = false, error = "Teacher email is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Get teacher ID
                var getTeacherCmd = connection.CreateCommand();
                getTeacherCmd.CommandText = "SELECT teacher_id FROM Teacher WHERE teacher_email = @email";
                getTeacherCmd.Parameters.AddWithValue("@email", request.StudentEmail); // Reusing the field name
                var teacherId = getTeacherCmd.ExecuteScalar();

                if (teacherId == null)
                {
                    return NotFound(new { success = false, error = "Teacher not found." });
                }

                // Get students studying with this teacher
                var getStudentsCmd = connection.CreateCommand();
                getStudentsCmd.CommandText = @"
                    SELECT ss.day, s.student_name, s.student_email, t.instrument, t.bio
                    FROM Student_Studying ss
                    JOIN Student s ON ss.student_id = s.student_id
                    JOIN Teacher t ON ss.teacher_id = t.teacher_id
                    WHERE ss.teacher_id = @teacherId
                    ORDER BY 
                        CASE ss.day
                            WHEN 'Monday' THEN 1
                            WHEN 'Tuesday' THEN 2
                            WHEN 'Wednesday' THEN 3
                            WHEN 'Thursday' THEN 4
                            WHEN 'Friday' THEN 5
                            WHEN 'Saturday' THEN 6
                            WHEN 'Sunday' THEN 7
                        END";

                getStudentsCmd.Parameters.AddWithValue("@teacherId", teacherId);

                var students = new List<object>();
                using (var studentsReader = getStudentsCmd.ExecuteReader())
                {
                    while (studentsReader.Read())
                    {
                        students.Add(new
                        {
                            day = studentsReader.GetString(0),
                            studentName = studentsReader.GetString(1),
                            studentEmail = studentsReader.GetString(2),
                            instrument = studentsReader.GetString(3),
                            bio = studentsReader.GetString(4)
                        });
                    }
                }

                return Ok(new { success = true, lessons = students });
            }
        }

        [HttpPost("student-studying/get")]
        public IActionResult GetStudentStudying([FromBody] GetStudentStudyingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentEmail))
            {
                return BadRequest(new { success = false, error = "Student email is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Get student ID
                var getStudentCmd = connection.CreateCommand();
                getStudentCmd.CommandText = "SELECT student_id FROM Student WHERE student_email = @email";
                getStudentCmd.Parameters.AddWithValue("@email", request.StudentEmail);
                var studentId = getStudentCmd.ExecuteScalar();

                if (studentId == null)
                {
                    return NotFound(new { success = false, error = "Student not found." });
                }

                // Get student studying relationships
                var getStudyingCmd = connection.CreateCommand();
                getStudyingCmd.CommandText = @"
                    SELECT ss.day, t.teacher_name, t.teacher_email, t.instrument, t.bio
                    FROM Student_Studying ss
                    JOIN Teacher t ON ss.teacher_id = t.teacher_id
                    WHERE ss.student_id = @studentId
                    ORDER BY 
                        CASE ss.day
                            WHEN 'Monday' THEN 1
                            WHEN 'Tuesday' THEN 2
                            WHEN 'Wednesday' THEN 3
                            WHEN 'Thursday' THEN 4
                            WHEN 'Friday' THEN 5
                            WHEN 'Saturday' THEN 6
                            WHEN 'Sunday' THEN 7
                        END";

                getStudyingCmd.Parameters.AddWithValue("@studentId", studentId);

                var studying = new List<object>();
                using (var studyingReader = getStudyingCmd.ExecuteReader())
                {
                    while (studyingReader.Read())
                    {
                        studying.Add(new
                        {
                            day = studyingReader.GetString(0),
                            teacherName = studyingReader.GetString(1),
                            teacherEmail = studyingReader.GetString(2),
                            instrument = studyingReader.GetString(3),
                            bio = studyingReader.GetString(4)
                        });
                    }
                }

                return Ok(new { success = true, lessons = studying });
            }
        }

        [HttpPost("check-password")]
        public IActionResult CheckPassword([FromBody] CheckPasswordRequest request)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Check Student table
                var getStudentCmd = connection.CreateCommand();
                getStudentCmd.CommandText = @"
                    SELECT student_id, student_name, student_email, studentpassword 
                    FROM Student 
                    WHERE student_email = @email";
                getStudentCmd.Parameters.AddWithValue("@email", request.Email);

                using (var reader = getStudentCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var userId = reader.GetInt32(0);
                        var fullName = reader.GetString(1);
                        var email = reader.GetString(2);
                        var storedPassword = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                        
                        return Ok(new { 
                            success = true, 
                            found = true,
                            table = "Student",
                            userId = userId,
                            fullName = fullName,
                            email = email,
                            storedPassword = storedPassword,
                            hasPassword = storedPassword.HasValue
                        });
                    }
                }

                // Check Teacher table
                var getTeacherCmd = connection.CreateCommand();
                getTeacherCmd.CommandText = @"
                    SELECT teacher_id, teacher_name, teacher_email, teacher_password 
                    FROM Teacher 
                    WHERE teacher_email = @email";
                getTeacherCmd.Parameters.AddWithValue("@email", request.Email);

                using (var teacherReader = getTeacherCmd.ExecuteReader())
                {
                    if (teacherReader.Read())
                    {
                        var userId = teacherReader.GetInt32(0);
                        var fullName = teacherReader.GetString(1);
                        var email = teacherReader.GetString(2);
                        var storedPassword = teacherReader.IsDBNull(3) ? (int?)null : teacherReader.GetInt32(3);
                        
                        return Ok(new { 
                            success = true, 
                            found = true,
                            table = "Teacher",
                            userId = userId,
                            fullName = fullName,
                            email = email,
                            storedPassword = storedPassword,
                            hasPassword = storedPassword.HasValue
                        });
                    }
                }

                return Ok(new { 
                    success = true, 
                    found = false,
                    message = "User not found"
                });
            }
        }

        [HttpPost("update-password")]
        public IActionResult UpdatePassword([FromBody] UpdatePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { success = false, error = "Email and new password are required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Try to update student password first
                var updateStudentCmd = connection.CreateCommand();
                updateStudentCmd.CommandText = @"
                    UPDATE Student 
                    SET studentpassword = @password, updated_at = @updatedAt
                    WHERE student_email = @email";
                updateStudentCmd.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
                updateStudentCmd.Parameters.AddWithValue("@email", request.Email);
                updateStudentCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));

                int studentRowsAffected = updateStudentCmd.ExecuteNonQuery();

                if (studentRowsAffected > 0)
                {
                    return Ok(new { success = true, message = "Student password updated successfully" });
                }

                // Try to update teacher password
                var updateTeacherCmd = connection.CreateCommand();
                updateTeacherCmd.CommandText = @"
                    UPDATE Teacher 
                    SET teacher_password = @password, updated_at = @updatedAt
                    WHERE teacher_email = @email";
                updateTeacherCmd.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
                updateTeacherCmd.Parameters.AddWithValue("@email", request.Email);
                updateTeacherCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));

                int teacherRowsAffected = updateTeacherCmd.ExecuteNonQuery();

                if (teacherRowsAffected > 0)
                {
                    return Ok(new { success = true, message = "Teacher password updated successfully" });
                }

                return NotFound(new { success = false, error = "User not found." });
            }
        }

        [HttpPost("migrate-passwords")]
        public IActionResult MigratePasswords([FromBody] object? request = null)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                try
                {
                    // Set default passwords for sample teachers
                    var updateSampleTeachersCmd = connection.CreateCommand();
                    updateSampleTeachersCmd.CommandText = @"
                        UPDATE Teacher 
                        SET teacher_password = @bcryptHash 
                        WHERE teacher_email IN ('alice.wilson@email.com', 'bob.brown@email.com', 'carol.green@email.com', 'david.lee@email.com')
                        AND (teacher_password IS NULL OR teacher_password = '' OR teacher_password = '0' OR teacher_password NOT LIKE '$2%');
                    ";
                    updateSampleTeachersCmd.Parameters.AddWithValue("@bcryptHash", BCrypt.Net.BCrypt.HashPassword("password123"));
                    updateSampleTeachersCmd.ExecuteNonQuery();

                    return Ok(new { 
                        success = true, 
                        message = "Database migration completed successfully. Sample teachers now have default password 'password123'." 
                    });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { 
                        success = false, 
                        error = $"Migration failed: {ex.Message}" 
                    });
                }
            }
        }

        [HttpPost("payment/process")]
        public IActionResult ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentEmail) || string.IsNullOrWhiteSpace(request.TeacherEmail) ||
                string.IsNullOrWhiteSpace(request.LessonDay) || request.Amount <= 0 ||
                string.IsNullOrWhiteSpace(request.PaymentMethod))
            {
                return BadRequest(new { success = false, error = "All payment fields are required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                try
                {
                    // Get student ID
                    var getStudentCmd = connection.CreateCommand();
                    getStudentCmd.CommandText = "SELECT student_id FROM Student WHERE student_email = @email";
                    getStudentCmd.Parameters.AddWithValue("@email", request.StudentEmail);
                    var studentId = getStudentCmd.ExecuteScalar();

                    if (studentId == null)
                    {
                        return NotFound(new { success = false, error = "Student not found." });
                    }

                    // Get teacher ID
                    var getTeacherCmd = connection.CreateCommand();
                    getTeacherCmd.CommandText = "SELECT teacher_id FROM Teacher WHERE teacher_email = @email";
                    getTeacherCmd.Parameters.AddWithValue("@email", request.TeacherEmail);
                    var teacherId = getTeacherCmd.ExecuteScalar();

                    if (teacherId == null)
                    {
                        return NotFound(new { success = false, error = "Teacher not found." });
                    }

                    // Generate transaction ID (simplified for demo)
                    var transactionId = $"TXN_{DateTime.UtcNow:yyyyMMddHHmmss}_{studentId}_{teacherId}";

                    // Insert payment record
                    var insertPaymentCmd = connection.CreateCommand();
                    insertPaymentCmd.CommandText = @"
                        INSERT INTO Payment (student_id, teacher_id, lesson_day, amount, payment_method, payment_status, transaction_id, payment_date, created_at, updated_at)
                        VALUES (@studentId, @teacherId, @lessonDay, @amount, @paymentMethod, @paymentStatus, @transactionId, @paymentDate, @createdAt, @updatedAt);
                        SELECT last_insert_rowid();";

                    var now = DateTime.UtcNow.ToString("o");
                    insertPaymentCmd.Parameters.AddWithValue("@studentId", studentId);
                    insertPaymentCmd.Parameters.AddWithValue("@teacherId", teacherId);
                    insertPaymentCmd.Parameters.AddWithValue("@lessonDay", request.LessonDay);
                    insertPaymentCmd.Parameters.AddWithValue("@amount", request.Amount);
                    insertPaymentCmd.Parameters.AddWithValue("@paymentMethod", request.PaymentMethod);
                    insertPaymentCmd.Parameters.AddWithValue("@paymentStatus", "Completed"); // For demo, assume payment succeeds
                    insertPaymentCmd.Parameters.AddWithValue("@transactionId", transactionId);
                    insertPaymentCmd.Parameters.AddWithValue("@paymentDate", now);
                    insertPaymentCmd.Parameters.AddWithValue("@createdAt", now);
                    insertPaymentCmd.Parameters.AddWithValue("@updatedAt", now);

                    var paymentId = Convert.ToInt32(insertPaymentCmd.ExecuteScalar());

                    return Ok(new { 
                        success = true, 
                        message = "Payment processed successfully",
                        paymentId = paymentId,
                        transactionId = transactionId,
                        amount = request.Amount
                    });
                }
                catch (Exception ex)
                {
                    return BadRequest(new { 
                        success = false, 
                        error = $"Payment processing failed: {ex.Message}" 
                    });
                }
            }
        }

        [HttpPost("payment/history")]
        public IActionResult GetPaymentHistory([FromBody] GetPaymentHistoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentEmail))
            {
                return BadRequest(new { success = false, error = "Student email is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Get student ID
                var getStudentCmd = connection.CreateCommand();
                getStudentCmd.CommandText = "SELECT student_id FROM Student WHERE student_email = @email";
                getStudentCmd.Parameters.AddWithValue("@email", request.StudentEmail);
                var studentId = getStudentCmd.ExecuteScalar();

                if (studentId == null)
                {
                    return NotFound(new { success = false, error = "Student not found." });
                }

                // Get payment history
                var getPaymentsCmd = connection.CreateCommand();
                getPaymentsCmd.CommandText = @"
                    SELECT p.payment_id, p.lesson_day, p.amount, p.payment_method, p.payment_status, 
                           p.transaction_id, p.payment_date, t.teacher_name, t.instrument
                    FROM Payment p
                    JOIN Teacher t ON p.teacher_id = t.teacher_id
                    WHERE p.student_id = @studentId
                    ORDER BY p.payment_date DESC";

                getPaymentsCmd.Parameters.AddWithValue("@studentId", studentId);

                var payments = new List<object>();
                using (var reader = getPaymentsCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        payments.Add(new
                        {
                            paymentId = reader.GetInt32(0),
                            lessonDay = reader.GetString(1),
                            amount = reader.GetDecimal(2),
                            paymentMethod = reader.GetString(3),
                            paymentStatus = reader.GetString(4),
                            transactionId = reader.GetString(5),
                            paymentDate = reader.GetString(6),
                            teacherName = reader.GetString(7),
                            instrument = reader.GetString(8)
                        });
                    }
                }

                return Ok(new { success = true, payments = payments });
            }
        }

        [HttpPost("update-student-password")]
        public IActionResult UpdateStudentPassword([FromBody] UpdatePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { success = false, error = "Email and password are required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                try
                {
                    // Update student password
                    var updatePasswordCmd = connection.CreateCommand();
                    updatePasswordCmd.CommandText = @"
                        UPDATE Student 
                        SET studentpassword = @bcryptHash, updated_at = @updatedAt
                        WHERE student_email = @email";
                    
                    var now = DateTime.UtcNow.ToString("o");
                    updatePasswordCmd.Parameters.AddWithValue("@bcryptHash", BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
                    updatePasswordCmd.Parameters.AddWithValue("@email", request.Email);
                    updatePasswordCmd.Parameters.AddWithValue("@updatedAt", now);

                    int rowsAffected = updatePasswordCmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        return Ok(new { 
                            success = true, 
                            message = "Student password updated successfully." 
                        });
                    }
                    else
                    {
                        return NotFound(new { 
                            success = false, 
                            error = "Student not found." 
                        });
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(new { 
                        success = false, 
                        error = $"Password update failed: {ex.Message}" 
                    });
                }
            }
        }

        [HttpPost("update-teacher-password")]
        public IActionResult UpdateTeacherPassword([FromBody] UpdatePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { success = false, error = "Email and password are required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                try
                {
                    // Update teacher password
                    var updatePasswordCmd = connection.CreateCommand();
                    updatePasswordCmd.CommandText = @"
                        UPDATE Teacher 
                        SET teacher_password = @bcryptHash, updated_at = @updatedAt
                        WHERE teacher_email = @email";
                    
                    var now = DateTime.UtcNow.ToString("o");
                    updatePasswordCmd.Parameters.AddWithValue("@bcryptHash", BCrypt.Net.BCrypt.HashPassword(request.NewPassword));
                    updatePasswordCmd.Parameters.AddWithValue("@email", request.Email);
                    updatePasswordCmd.Parameters.AddWithValue("@updatedAt", now);

                    int rowsAffected = updatePasswordCmd.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                    return Ok(new { 
                        success = true, 
                            message = "Password updated successfully." 
                        });
                    }
                    else
                    {
                        return NotFound(new { 
                            success = false, 
                            error = "Teacher not found." 
                        });
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(new { 
                        success = false, 
                        error = $"Password update failed: {ex.Message}" 
                    });
                }
            }
        }

        [HttpPost("payment/teacher-earnings")]
        public IActionResult GetTeacherEarnings([FromBody] GetPaymentHistoryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentEmail)) // Reusing the same request class
            {
                return BadRequest(new { success = false, error = "Teacher email is required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Get teacher ID
                var getTeacherCmd = connection.CreateCommand();
                getTeacherCmd.CommandText = "SELECT teacher_id FROM Teacher WHERE teacher_email = @email";
                getTeacherCmd.Parameters.AddWithValue("@email", request.StudentEmail); // Reusing the field name
                var teacherId = getTeacherCmd.ExecuteScalar();

                if (teacherId == null)
                {
                    return NotFound(new { success = false, error = "Teacher not found." });
                }

                // Get teacher earnings
                var getEarningsCmd = connection.CreateCommand();
                getEarningsCmd.CommandText = @"
                    SELECT p.payment_id, p.lesson_day, p.amount, p.payment_method, p.payment_status, 
                           p.transaction_id, p.payment_date, s.student_name, s.student_email
                    FROM Payment p
                    JOIN Student s ON p.student_id = s.student_id
                    WHERE p.teacher_id = @teacherId
                    ORDER BY p.payment_date DESC";

                getEarningsCmd.Parameters.AddWithValue("@teacherId", teacherId);

                var earnings = new List<object>();
                using (var reader = getEarningsCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        earnings.Add(new
                        {
                            paymentId = reader.GetInt32(0),
                            lessonDay = reader.GetString(1),
                            amount = reader.GetDecimal(2),
                            paymentMethod = reader.GetString(3),
                            paymentStatus = reader.GetString(4),
                            transactionId = reader.GetString(5),
                            paymentDate = reader.GetString(6),
                            studentName = reader.GetString(7),
                            studentEmail = reader.GetString(8)
                        });
                    }
                }

                // Calculate total earnings
                var totalEarningsCmd = connection.CreateCommand();
                totalEarningsCmd.CommandText = @"
                    SELECT COALESCE(SUM(amount), 0) as total_earnings, COUNT(*) as total_lessons
                    FROM Payment 
                    WHERE teacher_id = @teacherId AND payment_status = 'Completed'";
                totalEarningsCmd.Parameters.AddWithValue("@teacherId", teacherId);

                decimal totalEarnings = 0;
                int totalLessons = 0;
                using (var reader = totalEarningsCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        totalEarnings = reader.GetDecimal(0);
                        totalLessons = reader.GetInt32(1);
                    }
                }

                return Ok(new { 
                    success = true, 
                    earnings = earnings,
                    totalEarnings = totalEarnings,
                    totalLessons = totalLessons
                });
            }
        }
    }

        [HttpPost("payment/admin-fees")]
        public IActionResult GetAdminFeeRevenue([FromBody] object request)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Get total admin fee payments (description = 'Admin Fee Payment')
                var getAdminFeesCmd = connection.CreateCommand();
                getAdminFeesCmd.CommandText = @"
                    SELECT COALESCE(SUM(amount), 0) as totalAdminFees, COUNT(*) as totalPayments
                    FROM Payment 
                    WHERE description = 'Admin Fee Payment' AND payment_status = 'Completed'";

                using (var reader = getAdminFeesCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var totalAdminFees = reader.GetDecimal(0);
                        var totalPayments = reader.GetInt32(1);

                        return Ok(new { 
                            success = true, 
                            totalAdminFees = totalAdminFees,
                            totalPayments = totalPayments
                        });
                    }
                }

                return Ok(new { success = true, totalAdminFees = 0.0, totalPayments = 0 });
            }
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

    public class CreateTeacherProfileRequest
    {
        public string? Name { get; set; }
        public string? Instrument { get; set; }
        public string? Bio { get; set; }
        public string? Email { get; set; }
        public int? ClassFull { get; set; }
        public int? ClassLimit { get; set; }
    }

    public class GetTeacherProfileRequest
    {
        public string? Email { get; set; }
    }

    public class UpdateTeacherProfileRequest
    {
        public string? Name { get; set; }
        public string? Instrument { get; set; }
        public string? Bio { get; set; }
        public string? Email { get; set; }
        public int? ClassFull { get; set; }
        public int? ClassLimit { get; set; }
    }

    public class AddTeacherAvailabilityRequest
    {
        public string? Email { get; set; }
        public string? Day { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
    }

    public class GetTeacherAvailabilityRequest
    {
        public string? Email { get; set; }
    }

    public class SetTeacherAvailabilityRequest
    {
        public string? Email { get; set; }
        public List<AvailabilitySlot>? Availability { get; set; }
    }

    public class AvailabilitySlot
    {
        public string? DayOfWeek { get; set; }
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class GetTeachersListRequest
    {
        public string? Instrument { get; set; }
    }

    public class GetTeacherScheduleRequest
    {
        public string? TeacherId { get; set; }
    }

    public class AddStudentStudyingRequest
    {
        public string? StudentEmail { get; set; }
        public string? TeacherEmail { get; set; }
        public string? Day { get; set; }
    }

    public class GetStudentStudyingRequest
    {
        public string? StudentEmail { get; set; }
    }

    public class TestHashRequest
    {
        public string? Password { get; set; }
    }

    public class CheckPasswordRequest
    {
        public string? Email { get; set; }
    }

    public class UpdatePasswordRequest
    {
        public string? Email { get; set; }
        public string? NewPassword { get; set; }
    }

    public class ProcessPaymentRequest
    {
        public string? StudentEmail { get; set; }
        public string? TeacherEmail { get; set; }
        public string? LessonDay { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
    }

    public class GetPaymentHistoryRequest
    {
        public string? StudentEmail { get; set; }
    }
}
