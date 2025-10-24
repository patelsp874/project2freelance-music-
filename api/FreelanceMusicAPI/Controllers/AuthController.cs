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

                // Create Student table (matching your exact schema)
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

                // Create Teacher table (matching your exact schema)
                var createTeacherTableCmd = connection.CreateCommand();
                createTeacherTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Teacher (
                        teacher_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        teacher_name TEXT NOT NULL,
                        teacher_password TEXT,
                        teacher_email TEXT NOT NULL UNIQUE,
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

                // Add user_role column to Student table if it doesn't exist
                try
                {
                    var alterStudentRoleCmd = connection.CreateCommand();
                    alterStudentRoleCmd.CommandText = "ALTER TABLE Student ADD COLUMN user_role TEXT DEFAULT 'student';";
                    alterStudentRoleCmd.ExecuteNonQuery();
                }
                catch (SqliteException)
                {
                    // Column already exists, ignore the error
                }

                // Add user_role column to Teacher table if it doesn't exist
                try
                {
                    var alterTeacherRoleCmd = connection.CreateCommand();
                    alterTeacherRoleCmd.CommandText = "ALTER TABLE Teacher ADD COLUMN user_role TEXT DEFAULT 'teacher';";
                    alterTeacherRoleCmd.ExecuteNonQuery();
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

                // Create Payment table (matching your exact schema)
                var createPaymentTableCmd = connection.CreateCommand();
                createPaymentTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Payment (
                        pmtID INTEGER PRIMARY KEY AUTOINCREMENT,
                        stdID INTEGER NOT NULL,
                        teacherID INTEGER NOT NULL,
                        pmtAMT DECIMAL(10,2) NOT NULL,
                        admin_fee DECIMAL(10,2) NOT NULL,
                        CREATEDat DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (stdID) REFERENCES Student(student_id) ON DELETE CASCADE,
                        FOREIGN KEY (teacherID) REFERENCES Teacher(teacher_id) ON DELETE CASCADE
                    );";
                createPaymentTableCmd.ExecuteNonQuery();

                // Create Lesson_Files table (matching your exact schema)
                var createLessonFilesTableCmd = connection.CreateCommand();
                createLessonFilesTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Lesson_Files (
                        file_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        lesson_id INTEGER,
                        student_id INTEGER NOT NULL,
                        teacher_id INTEGER NOT NULL,
                        file_name TEXT NOT NULL,
                        file_path TEXT NOT NULL,
                        FOREIGN KEY (student_id) REFERENCES Student(student_id) ON DELETE CASCADE,
                        FOREIGN KEY (teacher_id) REFERENCES Teacher(teacher_id) ON DELETE CASCADE
                    );";
                createLessonFilesTableCmd.ExecuteNonQuery();

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
                    CREATE INDEX IF NOT EXISTS idx_payment_student ON Payment(stdID);
                    CREATE INDEX IF NOT EXISTS idx_payment_teacher ON Payment(teacherID);
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
                string userType;

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
                    insertStudentCmd.Parameters.AddWithValue("@password", int.Parse(request.Password)); // Password already hashed by frontend
                    insertStudentCmd.Parameters.AddWithValue("@createdAt", now);
                    insertStudentCmd.Parameters.AddWithValue("@updatedAt", now);

                    userId = Convert.ToInt32(insertStudentCmd.ExecuteScalar());
                    userType = "student";
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
                    insertTeacherCmd.Parameters.AddWithValue("@password", int.Parse(request.Password)); // Password already hashed by frontend
                    insertTeacherCmd.Parameters.AddWithValue("@instrument", "Not specified"); // Default value
                    insertTeacherCmd.Parameters.AddWithValue("@bio", "New teacher profile"); // Default value
                    insertTeacherCmd.Parameters.AddWithValue("@createdAt", now);
                    insertTeacherCmd.Parameters.AddWithValue("@updatedAt", now);

                    userId = Convert.ToInt32(insertTeacherCmd.ExecuteScalar());
                    userType = "teacher";
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

                // Try to find user in Admin table first
                var getAdminCmd = connection.CreateCommand();
                getAdminCmd.CommandText = @"
                    SELECT admin_id, admin_name, admin_email, admin_password
                    FROM Admin 
                    WHERE admin_email = @email";
                getAdminCmd.Parameters.AddWithValue("@email", request.Email);

                using (var adminReader = getAdminCmd.ExecuteReader())
                {
                    if (adminReader.Read())
                    {
                        var adminId = adminReader.GetInt32(0);
                        var adminName = adminReader.GetString(1);
                        var adminEmail = adminReader.GetString(2);
                        var adminPassword = adminReader.GetString(3);
                        
                        // Check password - handle BCrypt, plain text, and old integer format
                        bool passwordValid = false;
                        
                        Console.WriteLine($"Admin login attempt - Email: {request.Email}, Password: {request.Password}");
                        Console.WriteLine($"Stored admin password: {adminPassword}");
                        Console.WriteLine($"Password starts with $2: {adminPassword.StartsWith("$2")}");
                        Console.WriteLine($"Password is integer: {int.TryParse(adminPassword, out int test)}");
                        
                        if (adminPassword.StartsWith("$2")) // BCrypt format
                        {
                            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, adminPassword);
                            Console.WriteLine($"BCrypt verification result: {passwordValid}");
                        }
                        else if (int.TryParse(adminPassword, out int oldPasswordHash))
                        {
                            // Old format - check if password matches the hash
                            passwordValid = (oldPasswordHash.ToString() == request.Password);
                            Console.WriteLine($"Old format verification result: {passwordValid}");
                        }
                        else
                        {
                            // Plain text format - direct comparison
                            passwordValid = (adminPassword == request.Password);
                            Console.WriteLine($"Plain text verification result: {passwordValid}");
                        }
                        
                        if (passwordValid)
                        {
                            var nameParts = adminName.Split(' ');
                            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                            return Ok(new
                            {
                                success = true,
                                message = "Admin login successful",
                                user = new
                                {
                                    id = adminId,
                                    firstName = firstName,
                                    lastName = lastName,
                                    email = adminEmail,
                                    accountType = "admin",
                                    userRole = "admin"
                                }
                            });
                        }
                    }
                }

                // If not found in Admin table, try Student table
                var getStudentCmd = connection.CreateCommand();
                getStudentCmd.CommandText = @"
                    SELECT student_id, student_name, student_email, studentpassword, COALESCE(user_role, 'student') as user_role
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
                        var storedPassword = reader.GetString(3);
                        var userRole = reader.GetString(4);
                        
                        // Check password - handle both old integer format and new BCrypt format
                        bool passwordValid = false;
                        
                        if (storedPassword.StartsWith("$2")) // BCrypt format
                        {
                            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, storedPassword);
                        }
                        else if (int.TryParse(storedPassword, out int oldPasswordHash))
                        {
                            // Old format - check if password matches the hash
                            passwordValid = (oldPasswordHash.ToString() == request.Password);
                        }
                        
                        if (passwordValid)
                        {
                            var nameParts = fullName.Split(' ');
                            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                            return Ok(new
                            {
                                success = true,
                                user = new { id = userId, firstName, lastName, email, accountType = "student", userRole = userRole }
                            });
                        }
                    }
                }

                // If not found in Student table, try Teacher table
                var getTeacherCmd = connection.CreateCommand();
                getTeacherCmd.CommandText = @"
                    SELECT teacher_id, teacher_name, teacher_email, teacher_password, COALESCE(user_role, 'teacher') as user_role
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
                        var storedPassword = teacherReader.GetString(3);
                        var userRole = teacherReader.GetString(4);
                        
                        // Check password - handle both old integer format and new BCrypt format
                        bool passwordValid = false;
                        
                        if (storedPassword.StartsWith("$2")) // BCrypt format
                        {
                            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, storedPassword);
                        }
                        else if (int.TryParse(storedPassword, out int oldPasswordHash))
                        {
                            // Old format - check if password matches the hash
                            passwordValid = (oldPasswordHash.ToString() == request.Password);
                        }
                        
                        if (passwordValid)
                        {
                            var nameParts = fullName.Split(' ');
                            var firstName = nameParts.Length > 0 ? nameParts[0] : "";
                            var lastName = nameParts.Length > 1 ? string.Join(" ", nameParts.Skip(1)) : "";

                            return Ok(new
                            {
                                success = true,
                                user = new { id = userId, firstName, lastName, email, accountType = "teacher", userRole = userRole }
                            });
                        }
                        }
                    }
                
                        return Unauthorized(new { success = false, error = "Invalid email or password." });
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
                        
                        if (currentClass >= classLimit)
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

                return Ok(new { success = true, studying = studying });
            }
        }

        [HttpPost("admin/dashboard-stats")]
        public IActionResult GetAdminDashboardStats([FromBody] object request)
        {
            try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                    // Get total students
                    var totalStudentsCmd = connection.CreateCommand();
                    totalStudentsCmd.CommandText = "SELECT COUNT(*) FROM Student";
                    var totalStudents = totalStudentsCmd.ExecuteScalar();

                    // Get total teachers
                    var totalTeachersCmd = connection.CreateCommand();
                    totalTeachersCmd.CommandText = "SELECT COUNT(*) FROM Teacher";
                    var totalTeachers = totalTeachersCmd.ExecuteScalar();

                    // Get total lessons booked
                    var totalLessonsCmd = connection.CreateCommand();
                    totalLessonsCmd.CommandText = "SELECT COUNT(*) FROM Student_Studying";
                    var totalLessons = totalLessonsCmd.ExecuteScalar();

                    // Get total revenue from admin fees
                    var totalRevenueCmd = connection.CreateCommand();
                    totalRevenueCmd.CommandText = @"
                        SELECT COALESCE(SUM(pmtAMT), 0) 
                        FROM Payment";
                    var totalRevenue = totalRevenueCmd.ExecuteScalar();

                    // Get total instruments offered
                    var totalInstrumentsCmd = connection.CreateCommand();
                    totalInstrumentsCmd.CommandText = "SELECT COUNT(DISTINCT instrument) FROM Teacher";
                    var totalInstruments = totalInstrumentsCmd.ExecuteScalar();

                    // Get repeat lesson percentage
                    var repeatLessonsCmd = connection.CreateCommand();
                    repeatLessonsCmd.CommandText = @"
                        SELECT 
                            CASE 
                                WHEN COUNT(DISTINCT student_id) = 0 THEN 0
                                ELSE ROUND(
                                    (COUNT(DISTINCT CASE WHEN lesson_count > 1 THEN student_id END) * 100.0) / 
                                    COUNT(DISTINCT student_id), 2
                                )
                            END as repeat_percentage
                        FROM (
                            SELECT student_id, COUNT(*) as lesson_count
                            FROM Student_Studying
                            GROUP BY student_id
                        )";
                    var repeatPercentage = repeatLessonsCmd.ExecuteScalar();

                    Console.WriteLine($"Admin Dashboard Stats - Students: {totalStudents}, Teachers: {totalTeachers}, Lessons: {totalLessons}, Revenue: {totalRevenue}, Instruments: {totalInstruments}, Repeat: {repeatPercentage}%");

                    return Ok(new
                    {
                            success = true, 
                        stats = new
                        {
                            totalStudents = Convert.ToInt32(totalStudents),
                            totalTeachers = Convert.ToInt32(totalTeachers),
                            totalLessons = Convert.ToInt32(totalLessons),
                            totalRevenue = Convert.ToDecimal(totalRevenue),
                            totalInstruments = Convert.ToInt32(totalInstruments),
                            repeatLessonPercentage = Convert.ToDouble(repeatPercentage)
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to get admin stats: {ex.Message}" });
            }
        }

        // Revenue Report - Last 4 Quarters
        [HttpPost("admin/revenue-report")]
        public IActionResult GetRevenueReport([FromBody] object request)
        {
            try
            {
                Console.WriteLine("Getting revenue report...");

                // Create sample quarterly data for demonstration
                var quarters = new List<object>
                {
                    new { quarter = "Q1", year = "2024", revenue = 25.00m },
                    new { quarter = "Q2", year = "2024", revenue = 50.00m },
                    new { quarter = "Q3", year = "2024", revenue = 75.00m },
                    new { quarter = "Q4", year = "2024", revenue = 0.00m }
                };

                Console.WriteLine($"Revenue report returning {quarters.Count} quarters");

                return Ok(new { success = true, quarters = quarters });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Revenue report error: {ex.Message}");
                return StatusCode(500, new { success = false, error = $"Failed to get revenue report: {ex.Message}" });
            }
        }

        // Referral Report - How students heard about us
        [HttpPost("admin/referral-report")]
        public IActionResult GetReferralReport([FromBody] object request)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // For now, we'll simulate referral data since we don't have a referral field
                    // In a real system, you'd have a referral_source field in the Student table
                    var referralCmd = connection.CreateCommand();
                    referralCmd.CommandText = @"
                        SELECT 
                            'Social Media' as source,
                            COUNT(*) * 0.4 as count
                        FROM Student
                        UNION ALL
                        SELECT 
                            'Word of Mouth' as source,
                            COUNT(*) * 0.3 as count
                        FROM Student
                        UNION ALL
                        SELECT 
                            'Google Search' as source,
                            COUNT(*) * 0.2 as count
                        FROM Student
                        UNION ALL
                        SELECT 
                            'Other' as source,
                            COUNT(*) * 0.1 as count
                        FROM Student";

                    var referrals = new List<object>();
                    using (var reader = referralCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            referrals.Add(new
                            {
                                source = reader.GetString(0),
                                count = Math.Round(reader.GetDouble(1))
                            });
                        }
                    }

                    return Ok(new { success = true, referrals = referrals });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to get referral report: {ex.Message}" });
            }
        }

        // Popular Instruments
        [HttpPost("admin/popular-instruments")]
        public IActionResult GetPopularInstruments([FromBody] object request)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    var instrumentsCmd = connection.CreateCommand();
                    instrumentsCmd.CommandText = @"
                        SELECT 
                            t.instrument,
                            COUNT(ss.student_id) as lesson_count,
                            COUNT(DISTINCT ss.student_id) as unique_students
                        FROM Teacher t
                        LEFT JOIN Student_Studying ss ON t.teacher_id = ss.teacher_id
                        WHERE t.instrument IS NOT NULL AND t.instrument != ''
                        GROUP BY t.instrument
                        ORDER BY lesson_count DESC";

                    var instruments = new List<object>();
                    using (var reader = instrumentsCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            instruments.Add(new
                            {
                                instrument = reader.GetString(0),
                                lessonCount = reader.GetInt32(1),
                                uniqueStudents = reader.GetInt32(2)
                            });
                        }
                    }

                    return Ok(new { success = true, instruments = instruments });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to get popular instruments: {ex.Message}" });
            }
        }

        // Lessons Booked - Sortable and Filterable
        [HttpPost("admin/lessons-booked")]
        public IActionResult GetLessonsBooked([FromBody] AdminDashboardStatsRequest request)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    var lessonsCmd = connection.CreateCommand();
                    lessonsCmd.CommandText = @"
                        SELECT 
                            s.student_name,
                            s.student_email,
                            t.teacher_name,
                            t.teacher_email,
                            t.instrument,
                            ss.day as lesson_date,
                            ss.created_at as booking_date
                        FROM Student_Studying ss
                        JOIN Student s ON ss.student_id = s.student_id
                        JOIN Teacher t ON ss.teacher_id = t.teacher_id
                        ORDER BY ss.created_at DESC";

                    var lessons = new List<object>();
                    using (var reader = lessonsCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            lessons.Add(new
                            {
                                studentName = reader.GetString(0),
                                studentEmail = reader.GetString(1),
                                teacherName = reader.GetString(2),
                                teacherEmail = reader.GetString(3),
                                instrument = reader.GetString(4),
                                lessonDate = reader.GetString(5),
                                bookingDate = reader.GetString(6)
                            });
                        }
                    }

                    return Ok(new { success = true, lessons = lessons });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to get lessons booked: {ex.Message}" });
            }
        }

        // Users Joined - Students and Teachers
        [HttpPost("admin/users-joined")]
        public IActionResult GetUsersJoined([FromBody] AdminDashboardStatsRequest request)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    var usersCmd = connection.CreateCommand();
                    usersCmd.CommandText = @"
                        SELECT 
                            'student' as role,
                            student_name as name,
                            student_email as email,
                            created_at as joined_date
                        FROM Student
                        WHERE user_role != 'admin'
                        UNION ALL
                        SELECT 
                            'teacher' as role,
                            teacher_name as name,
                            teacher_email as email,
                            created_at as joined_date
                        FROM Teacher
                        ORDER BY joined_date DESC";

                    var users = new List<object>();
                    using (var reader = usersCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new
                            {
                                role = reader.GetString(0),
                                name = reader.GetString(1),
                                email = reader.GetString(2),
                                joinedDate = reader.GetString(3)
                            });
                        }
                    }

                    return Ok(new { success = true, users = users });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to get users joined: {ex.Message}" });
            }
        }

        // Repeat Lessons Percentage
        [HttpPost("admin/repeat-lessons")]
        public IActionResult GetRepeatLessons([FromBody] object request)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    var repeatCmd = connection.CreateCommand();
                    repeatCmd.CommandText = @"
                        WITH lesson_counts AS (
                            SELECT 
                                student_id,
                                COUNT(*) as lesson_count
                            FROM Student_Studying
                            GROUP BY student_id
                        )
                        SELECT 
                            COUNT(*) as total_students,
                            COUNT(CASE WHEN lesson_count > 1 THEN 1 END) as repeat_students,
                            ROUND(
                                (COUNT(CASE WHEN lesson_count > 1 THEN 1 END) * 100.0) / 
                                COUNT(*), 2
                            ) as repeat_percentage
                        FROM lesson_counts";

                    using (var reader = repeatCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new
                            {
                                success = true,
                                totalStudents = reader.GetInt32(0),
                                repeatStudents = reader.GetInt32(1),
                                repeatPercentage = reader.GetDouble(2)
                            });
                        }
                    }

                    return Ok(new { success = true, totalStudents = 0, repeatStudents = 0, repeatPercentage = 0.0 });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to get repeat lessons: {ex.Message}" });
            }
        }

        // Revenue Distribution
        [HttpPost("admin/revenue-distribution")]
        public IActionResult GetRevenueDistribution([FromBody] object request)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Revenue by Instrument
                    var instrumentRevenueCmd = connection.CreateCommand();
                    instrumentRevenueCmd.CommandText = @"
                        SELECT 
                            t.instrument,
                            COALESCE(SUM(p.pmtAMT), 0) as revenue,
                            COUNT(p.payment_id) as payment_count
                        FROM Teacher t
                        LEFT JOIN Student_Studying ss ON t.teacher_id = ss.teacher_id
                        LEFT JOIN Payment p ON ss.student_id = p.stdID AND ss.teacher_id = p.teacherID
                        WHERE t.instrument IS NOT NULL AND t.instrument != ''
                        GROUP BY t.instrument
                        ORDER BY revenue DESC";

                    var instrumentRevenue = new List<object>();
                    using (var reader = instrumentRevenueCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            instrumentRevenue.Add(new
                            {
                                instrument = reader.GetString(0),
                                revenue = reader.GetDecimal(1),
                                paymentCount = reader.GetInt32(2)
                            });
                        }
                    }

                    // Top Contributing Students
                    var studentRevenueCmd = connection.CreateCommand();
                    studentRevenueCmd.CommandText = @"
                        SELECT 
                            s.student_name,
                            s.student_email,
                            COALESCE(SUM(p.pmtAMT), 0) as total_spent,
                            COUNT(p.payment_id) as payment_count
                        FROM Student s
                        LEFT JOIN Payment p ON s.student_id = p.stdID
                        WHERE s.user_role != 'admin'
                        GROUP BY s.student_id, s.student_name, s.student_email
                        HAVING SUM(p.pmtAMT) > 0
                        ORDER BY total_spent DESC
                        LIMIT 10";

                    var topStudents = new List<object>();
                    using (var reader = studentRevenueCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            topStudents.Add(new
                            {
                                studentName = reader.GetString(0),
                                studentEmail = reader.GetString(1),
                                totalSpent = reader.GetDecimal(2),
                                paymentCount = reader.GetInt32(3)
                            });
                        }
                    }

                    return Ok(new
                    {
                        success = true,
                        instrumentRevenue = instrumentRevenue,
                        topStudents = topStudents
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to get revenue distribution: {ex.Message}" });
            }
        }

        [HttpPost("admin/check-admin")]
        public IActionResult CheckAdmin()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT admin_id, admin_name, admin_email, admin_password FROM Admin WHERE admin_email = 'admin@freelancemusic.com'";

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new
                            {
                                success = true,
                                admin_id = reader.GetInt32(0),
                                admin_name = reader.GetString(1),
                                admin_email = reader.GetString(2),
                                admin_password = reader.GetString(3),
                                password_length = reader.GetString(3).Length,
                                starts_with_dollar = reader.GetString(3).StartsWith("$2")
                            });
                        }
                        else
                        {
                            return Ok(new { success = false, message = "No admin record found" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("admin/fix-password")]
        public IActionResult FixAdminPassword()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Generate correct BCrypt hash for "Admin123"
                    var correctHash = BCrypt.Net.BCrypt.HashPassword("Admin123");
                    
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE Admin SET admin_password = @password WHERE admin_email = 'admin@freelancemusic.com'";
                    command.Parameters.AddWithValue("@password", correctHash);
                    
                    int rowsAffected = command.ExecuteNonQuery();
                    
                    return Ok(new
                    {
                        success = true,
                        message = "Admin password updated successfully",
                        new_hash = correctHash,
                        rows_affected = rowsAffected
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        [HttpPost("admin/create-admin")]
        public IActionResult CreateAdminUser([FromBody] CreateAdminRequest request)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Check if admin already exists in Admin table
                    var checkAdminCmd = connection.CreateCommand();
                    checkAdminCmd.CommandText = "SELECT COUNT(*) FROM Admin WHERE admin_email = @email";
                    checkAdminCmd.Parameters.AddWithValue("@email", request.Email);
                    
                    var existingAdmins = Convert.ToInt32(checkAdminCmd.ExecuteScalar());

                    if (existingAdmins > 0)
                    {
                        return BadRequest(new { success = false, error = "Admin user already exists." });
                    }

                    // Hash the password with BCrypt
                    var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                    // Create admin user in Admin table
                    var createAdminCmd = connection.CreateCommand();
                    createAdminCmd.CommandText = @"
                        INSERT INTO Admin (admin_name, admin_email, admin_password)
                        VALUES (@name, @email, @password)";
                    createAdminCmd.Parameters.AddWithValue("@name", request.Name);
                    createAdminCmd.Parameters.AddWithValue("@email", request.Email);
                    createAdminCmd.Parameters.AddWithValue("@password", hashedPassword);

                    createAdminCmd.ExecuteNonQuery();

                    return Ok(new { success = true, message = "Admin user created successfully." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to create admin: {ex.Message}" });
            }
        }

        [HttpPost("payment/process")]
        public IActionResult ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.StudentEmail) || 
                    string.IsNullOrWhiteSpace(request.TeacherEmail) ||
                    string.IsNullOrWhiteSpace(request.CardNumber) ||
                    string.IsNullOrWhiteSpace(request.ExpiryMonth) ||
                    string.IsNullOrWhiteSpace(request.ExpiryYear) ||
                    string.IsNullOrWhiteSpace(request.CVV) ||
                    string.IsNullOrWhiteSpace(request.CardholderName))
                {
                    return BadRequest(new { success = false, error = "All payment fields are required." });
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                    // Get student and teacher IDs
                    var getStudentIdCmd = connection.CreateCommand();
                    getStudentIdCmd.CommandText = "SELECT student_id FROM Student WHERE student_email = @email";
                    getStudentIdCmd.Parameters.AddWithValue("@email", request.StudentEmail);
                    var studentId = getStudentIdCmd.ExecuteScalar();

                    var getTeacherIdCmd = connection.CreateCommand();
                    getTeacherIdCmd.CommandText = "SELECT teacher_id FROM Teacher WHERE teacher_email = @email";
                    getTeacherIdCmd.Parameters.AddWithValue("@email", request.TeacherEmail);
                    var teacherId = getTeacherIdCmd.ExecuteScalar();

                    if (studentId == null || teacherId == null)
                    {
                        return BadRequest(new { success = false, error = "Student or teacher not found." });
                    }

                    // Simulate payment processing (in real app, integrate with payment gateway)
                    var paymentId = Guid.NewGuid().ToString();
                    var amount = 50.00m; // $50 per lesson
                    var adminFee = 7.50m; // 15% admin fee

                    // Insert payment record
                    var insertPaymentCmd = connection.CreateCommand();
                    insertPaymentCmd.CommandText = @"
                        INSERT INTO Payment (stdID, teacherID, pmtAMT, admin_fee, CREATEDat)
                        VALUES (@studentId, @teacherId, @amount, @adminFee, @createdAt)";
                    insertPaymentCmd.Parameters.AddWithValue("@studentId", studentId);
                    insertPaymentCmd.Parameters.AddWithValue("@teacherId", teacherId);
                    insertPaymentCmd.Parameters.AddWithValue("@amount", amount);
                    insertPaymentCmd.Parameters.AddWithValue("@adminFee", adminFee);
                    insertPaymentCmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                    insertPaymentCmd.ExecuteNonQuery();

                return Ok(new { 
                    success = true, 
                        message = "Payment processed successfully.",
                        paymentId = paymentId,
                        amount = amount,
                        adminFee = adminFee
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Payment processing failed: {ex.Message}" });
            }
        }

        [HttpPost("payment/history")]
        public IActionResult GetPaymentHistory([FromBody] PaymentHistoryRequest request)
        {
            try
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                    var getPaymentsCmd = connection.CreateCommand();
                    getPaymentsCmd.CommandText = @"
                        SELECT p.pmtID, p.pmtAMT, p.admin_fee, p.CREATEDat,
                               s.student_name, t.teacher_name
                        FROM Payment p
                        JOIN Student s ON p.stdID = s.student_id
                        JOIN Teacher t ON p.teacherID = t.teacher_id
                        WHERE s.student_email = @email OR t.teacher_email = @email
                        ORDER BY p.CREATEDat DESC";
                    getPaymentsCmd.Parameters.AddWithValue("@email", request.Email);

                    var payments = new List<object>();
                    using (var reader = getPaymentsCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            payments.Add(new
                            {
                                paymentId = reader.GetString(0),
                                amount = reader.GetDecimal(1),
                                adminFee = reader.GetDecimal(2),
                                status = reader.GetString(3),
                                createdAt = reader.GetDateTime(4),
                                studentName = reader.GetString(5),
                                teacherName = reader.GetString(6)
                            });
                        }
                    }

                    return Ok(new { success = true, payments = payments });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to get payment history: {ex.Message}" });
            }
        }

        [HttpPost("teacher-students/get")]
        public IActionResult GetTeacherStudents([FromBody] GetTeacherStudentsRequest request)
        {
            try
            {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                    var getStudentsCmd = connection.CreateCommand();
                    getStudentsCmd.CommandText = @"
                        SELECT 
                            ss.student_id,
                            s.student_name,
                            s.student_email,
                            ss.day,
                            ss.created_at
                        FROM Student_Studying ss
                        JOIN Student s ON ss.student_id = s.student_id
                        JOIN Teacher t ON ss.teacher_id = t.teacher_id
                        WHERE t.teacher_email = @teacherEmail
                        ORDER BY ss.created_at DESC";
                    getStudentsCmd.Parameters.AddWithValue("@teacherEmail", request.TeacherEmail);

                    var students = new List<object>();
                    using (var reader = getStudentsCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            students.Add(new
                            {
                                studentId = reader.GetInt32(0),
                                studentName = reader.GetString(1),
                                studentEmail = reader.GetString(2),
                                day = reader.GetString(3),
                                createdAt = reader.GetString(4)
                            });
                        }
                    }

                    Console.WriteLine($"Found {students.Count} students for teacher {request.TeacherEmail}");
                    return Ok(new { success = true, lessons = students });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to get teacher students: {ex.Message}" });
            }
        }

        [HttpPost("test-upload")]
        public async Task<IActionResult> TestUpload()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                return Ok(new { 
                    success = true, 
                    message = "Test upload successful",
                    formKeys = form.Keys.ToList(),
                    filesCount = form.Files.Count,
                    hasFile = form.Files["file"] != null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("upload-lesson-file")]
        public async Task<IActionResult> UploadLessonFile()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                
                var file = form.Files["file"];
                var studentEmail = form["studentEmail"].ToString();
                var teacherEmail = form["teacherEmail"].ToString();
                var lessonDay = form["lessonDay"].ToString();

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = false, error = "No file uploaded" });
                }

                if (string.IsNullOrEmpty(studentEmail) || string.IsNullOrEmpty(teacherEmail) || string.IsNullOrEmpty(lessonDay))
                {
                    return BadRequest(new { success = false, error = "Missing required parameters" });
                }

                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "resources", "uploads", "teacher_files");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { 
                    success = true, 
                    message = "File uploaded successfully",
                    fileName = file.FileName,
                    filePath = filePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    success = false, 
                    error = $"Upload failed: {ex.Message}"
                });
            }
        }

        [HttpPost("payment/admin-fee-process")]
        public IActionResult ProcessAdminFeePayment([FromBody] AdminFeePaymentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TeacherEmail) ||
                    string.IsNullOrWhiteSpace(request.CardNumber) ||
                    string.IsNullOrWhiteSpace(request.ExpiryMonth) ||
                    string.IsNullOrWhiteSpace(request.ExpiryYear) ||
                    string.IsNullOrWhiteSpace(request.CVV) ||
                    string.IsNullOrWhiteSpace(request.CardholderName))
                {
                    return BadRequest(new { success = false, error = "All payment fields are required." });
                }

                // For now, just simulate successful payment
                // In a real application, you would integrate with a payment gateway
                return Ok(new { 
                    success = true, 
                    message = "Admin fee payment processed successfully",
                    transactionId = Guid.NewGuid().ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Payment processing failed: {ex.Message}" });
            }
        }

        [HttpPost("payment/teacher-earnings")]
        public IActionResult GetTeacherEarnings([FromBody] TeacherEarningsRequest request)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    var getEarningsCmd = connection.CreateCommand();
                    getEarningsCmd.CommandText = @"
                        SELECT 
                            COALESCE(SUM(pmtAMT - admin_fee), 0) as totalEarnings,
                            COUNT(*) as totalLessons
                        FROM Payment p
                        JOIN Teacher t ON p.teacherID = t.teacher_id
                        WHERE t.teacher_email = @email";
                    getEarningsCmd.Parameters.AddWithValue("@email", request.TeacherEmail);

                    using (var reader = getEarningsCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new
                            {
                        success = true, 
                                totalEarnings = reader.GetDecimal(0),
                                totalLessons = reader.GetInt32(1)
                    });
                        }
                    }

                    return Ok(new { success = true, totalEarnings = 0, totalLessons = 0 });
                }
                }
                catch (Exception ex)
                {
                return StatusCode(500, new { success = false, error = $"Failed to get teacher earnings: {ex.Message}" });
            }
        }

        [HttpPost("payment/admin-fees")]
        public IActionResult GetAdminFeeRevenue([FromBody] object request)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    var getAdminFeesCmd = connection.CreateCommand();
                    getAdminFeesCmd.CommandText = @"
                        SELECT COALESCE(SUM(admin_fee), 0) 
                        FROM Payment";
                    var totalAdminFees = getAdminFeesCmd.ExecuteScalar();

                    return Ok(new { success = true, totalAdminFees = Convert.ToDecimal(totalAdminFees) });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = $"Failed to get admin fee revenue: {ex.Message}" });
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

    public class GetTeacherStudentsRequest
    {
        public string? TeacherEmail { get; set; }
    }

    public class AdminFeePaymentRequest
    {
        public string? TeacherEmail { get; set; }
        public string? CardNumber { get; set; }
        public string? ExpiryMonth { get; set; }
        public string? ExpiryYear { get; set; }
        public string? CVV { get; set; }
        public string? CardholderName { get; set; }
        public decimal? Amount { get; set; }
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

    public class CreateAdminRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class ProcessPaymentRequest
    {
        public string? StudentEmail { get; set; }
        public string? TeacherEmail { get; set; }
        public string? CardNumber { get; set; }
        public string? ExpiryMonth { get; set; }
        public string? ExpiryYear { get; set; }
        public string? CVV { get; set; }
        public string? CardholderName { get; set; }
    }

    public class PaymentHistoryRequest
    {
        public string? Email { get; set; }
    }

    public class TeacherEarningsRequest
    {
        public string? TeacherEmail { get; set; }
    }

    public class AdminDashboardStatsRequest
    {
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
    }
}
