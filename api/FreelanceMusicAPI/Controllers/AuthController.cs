using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Security.Cryptography;

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
                        studentpassword INTEGER UNIQUE
                    );";
                createStudentTableCmd.ExecuteNonQuery();

                // Create Teacher table (matching your schema with current_class and password added)
                var createTeacherTableCmd = connection.CreateCommand();
                createTeacherTableCmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Teacher (
                        teacher_id INTEGER PRIMARY KEY AUTOINCREMENT,
                        teacher_name TEXT NOT NULL,
                        teacher_email TEXT NOT NULL UNIQUE,
                        teacher_password INTEGER UNIQUE,
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
                    insertStudentCmd.Parameters.AddWithValue("@password", request.Password.GetHashCode()); // Simple hash
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
                    insertTeacherCmd.Parameters.AddWithValue("@password", request.Password.GetHashCode()); // Same hash as student
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

                // Try to find user in Student table first
                var getStudentCmd = connection.CreateCommand();
                getStudentCmd.CommandText = @"
                    SELECT student_id, student_name, student_email 
                    FROM Student 
                    WHERE student_email = @email AND studentpassword = @password";
                getStudentCmd.Parameters.AddWithValue("@email", request.Email);
                getStudentCmd.Parameters.AddWithValue("@password", request.Password.GetHashCode());

                using (var reader = getStudentCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var userId = reader.GetInt32(0);
                        var fullName = reader.GetString(1);
                        var email = reader.GetString(2);
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

                // If not found in Student table, try Teacher table
                    var getTeacherCmd = connection.CreateCommand();
                getTeacherCmd.CommandText = @"
                    SELECT teacher_id, teacher_name, teacher_email 
                    FROM Teacher 
                    WHERE teacher_email = @email AND teacher_password = @password";
                getTeacherCmd.Parameters.AddWithValue("@email", request.Email);
                getTeacherCmd.Parameters.AddWithValue("@password", request.Password.GetHashCode());

                using (var teacherReader = getTeacherCmd.ExecuteReader())
                {
                    if (teacherReader.Read())
                    {
                        var userId = teacherReader.GetInt32(0);
                        var fullName = teacherReader.GetString(1);
                        var email = teacherReader.GetString(2);
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

                    // Check if teacher profile already exists
                    var checkProfileCmd = connection.CreateCommand();
                    checkProfileCmd.CommandText = "SELECT COUNT(*) FROM Teacher WHERE teacher_email = @email";
                    checkProfileCmd.Parameters.AddWithValue("@email", request.Email);

                    if (Convert.ToInt32(checkProfileCmd.ExecuteScalar()) > 0)
                    {
                        return Conflict(new { success = false, error = "Teacher profile with this email already exists." });
                    }

                // Create teacher profile in Teacher table
                    var createProfileCmd = connection.CreateCommand();
                    createProfileCmd.CommandText = @"
                    INSERT INTO Teacher (teacher_name, teacher_email, instrument, class_full, class_limit, current_class, bio, created_at, updated_at)
                    VALUES (@name, @email, @instrument, @classFull, @classLimit, 0, @bio, @createdAt, @updatedAt);
                        SELECT last_insert_rowid();";

                    var now = DateTime.UtcNow.ToString("o");
                    createProfileCmd.Parameters.AddWithValue("@name", request.Name);
                    createProfileCmd.Parameters.AddWithValue("@email", request.Email);
                    createProfileCmd.Parameters.AddWithValue("@instrument", request.Instrument);
                    createProfileCmd.Parameters.AddWithValue("@classFull", request.ClassFull ?? 0);
                    createProfileCmd.Parameters.AddWithValue("@classLimit", request.ClassLimit ?? 10);
                    createProfileCmd.Parameters.AddWithValue("@bio", request.Bio);
                    createProfileCmd.Parameters.AddWithValue("@createdAt", now);
                    createProfileCmd.Parameters.AddWithValue("@updatedAt", now);

                    var teacherId = Convert.ToInt32(createProfileCmd.ExecuteScalar());

                    return Ok(new { success = true, message = "Teacher profile created successfully", teacherId = teacherId });
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
}
