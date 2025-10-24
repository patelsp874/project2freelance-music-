using Microsoft.Data.Sqlite;

namespace FreelanceMusicAPI
{
    public class DataSeeder
    {
        private readonly string _connectionString;

        public DataSeeder(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void SeedDatabase()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                connection.Open();

                // Clear existing data
                ClearExistingData(connection);

                // Seed students
                SeedStudents(connection);

                // Seed teachers
                SeedTeachers(connection);

                // Seed teacher availability
                SeedTeacherAvailability(connection);

                // Seed student-teacher relationships
                SeedStudentStudying(connection);
            }
        }

        private void ClearExistingData(SqliteConnection connection)
        {
            var clearCommands = new[]
            {
                "DELETE FROM Student_Studying",
                "DELETE FROM Teacher_Day_Availability",
                "DELETE FROM Teacher",
                "DELETE FROM Student"
            };

            foreach (var cmd in clearCommands)
            {
                var command = connection.CreateCommand();
                command.CommandText = cmd;
                command.ExecuteNonQuery();
            }
        }

        private void SeedStudents(SqliteConnection connection)
        {
            var students = new[]
            {
                new { Name = "Alex Smith", Email = "alex.smith@student.com", Password = "password123" },
                new { Name = "Emma Davis", Email = "emma.davis@student.com", Password = "password123" },
                new { Name = "Noah Wilson", Email = "noah.wilson@student.com", Password = "password123" },
                new { Name = "Olivia Martinez", Email = "olivia.martinez@student.com", Password = "password123" },
                new { Name = "Liam Thompson", Email = "liam.thompson@student.com", Password = "password123" },
                new { Name = "Sophia White", Email = "sophia.white@student.com", Password = "password123" },
                new { Name = "William Harris", Email = "william.harris@student.com", Password = "password123" },
                new { Name = "Isabella Clark", Email = "isabella.clark@student.com", Password = "password123" }
            };

            var insertStudentCmd = connection.CreateCommand();
            insertStudentCmd.CommandText = @"
                INSERT INTO Student (student_name, student_email, studentpassword, created_at, updated_at)
                VALUES (@name, @email, @password, @createdAt, @updatedAt)";

            var now = DateTime.UtcNow.ToString("o");

            foreach (var student in students)
            {
                insertStudentCmd.Parameters.Clear();
                insertStudentCmd.Parameters.AddWithValue("@name", student.Name);
                insertStudentCmd.Parameters.AddWithValue("@email", student.Email);
                insertStudentCmd.Parameters.AddWithValue("@password", student.Password.GetHashCode());
                insertStudentCmd.Parameters.AddWithValue("@createdAt", now);
                insertStudentCmd.Parameters.AddWithValue("@updatedAt", now);
                insertStudentCmd.ExecuteNonQuery();
            }
        }

        private void SeedTeachers(SqliteConnection connection)
        {
            var teachers = new[]
            {
                new { Name = "Sarah Johnson", Email = "sarah.johnson@music.com", Password = "password123", Instrument = "Piano", Bio = "Classical pianist with 15 years of teaching experience. Specializes in classical and contemporary piano techniques.", ClassLimit = 8, CurrentClass = 0, ClassFull = 0 },
                new { Name = "Michael Chen", Email = "michael.chen@music.com", Password = "password123", Instrument = "Guitar", Bio = "Professional guitarist and music producer. Expert in acoustic, electric, and classical guitar styles.", ClassLimit = 10, CurrentClass = 0, ClassFull = 0 },
                new { Name = "Emily Rodriguez", Email = "emily.rodriguez@music.com", Password = "password123", Instrument = "Violin", Bio = "Orchestral violinist with extensive teaching background. Focuses on classical technique and performance skills.", ClassLimit = 6, CurrentClass = 0, ClassFull = 0 },
                new { Name = "David Williams", Email = "david.williams@music.com", Password = "password123", Instrument = "Drums", Bio = "Session drummer and percussion instructor. Specializes in rock, jazz, and world music styles.", ClassLimit = 12, CurrentClass = 0, ClassFull = 0 },
                new { Name = "Lisa Anderson", Email = "lisa.anderson@music.com", Password = "password123", Instrument = "Voice", Bio = "Opera singer and vocal coach. Helps students develop proper technique and performance confidence.", ClassLimit = 8, CurrentClass = 0, ClassFull = 0 },
                new { Name = "James Taylor", Email = "james.taylor@music.com", Password = "password123", Instrument = "Bass", Bio = "Professional bassist with jazz and rock background. Teaches both electric and upright bass techniques.", ClassLimit = 10, CurrentClass = 0, ClassFull = 0 },
                new { Name = "Maria Garcia", Email = "maria.garcia@music.com", Password = "password123", Instrument = "Flute", Bio = "Classical flutist and chamber music specialist. Experienced in teaching all skill levels.", ClassLimit = 8, CurrentClass = 0, ClassFull = 0 },
                new { Name = "Robert Brown", Email = "robert.brown@music.com", Password = "password123", Instrument = "Saxophone", Bio = "Jazz saxophonist and music educator. Specializes in jazz improvisation and contemporary styles.", ClassLimit = 10, CurrentClass = 0, ClassFull = 0 }
            };

            var insertTeacherCmd = connection.CreateCommand();
            insertTeacherCmd.CommandText = @"
                INSERT INTO Teacher (teacher_name, teacher_email, teacher_password, instrument, class_full, class_limit, current_class, bio, created_at, updated_at)
                VALUES (@name, @email, @password, @instrument, @classFull, @classLimit, @currentClass, @bio, @createdAt, @updatedAt)";

            var now = DateTime.UtcNow.ToString("o");

            foreach (var teacher in teachers)
            {
                insertTeacherCmd.Parameters.Clear();
                insertTeacherCmd.Parameters.AddWithValue("@name", teacher.Name);
                insertTeacherCmd.Parameters.AddWithValue("@email", teacher.Email);
                insertTeacherCmd.Parameters.AddWithValue("@password", teacher.Password.GetHashCode());
                insertTeacherCmd.Parameters.AddWithValue("@instrument", teacher.Instrument);
                insertTeacherCmd.Parameters.AddWithValue("@classFull", teacher.ClassFull);
                insertTeacherCmd.Parameters.AddWithValue("@classLimit", teacher.ClassLimit);
                insertTeacherCmd.Parameters.AddWithValue("@currentClass", teacher.CurrentClass);
                insertTeacherCmd.Parameters.AddWithValue("@bio", teacher.Bio);
                insertTeacherCmd.Parameters.AddWithValue("@createdAt", now);
                insertTeacherCmd.Parameters.AddWithValue("@updatedAt", now);
                insertTeacherCmd.ExecuteNonQuery();
            }
        }

        private void SeedTeacherAvailability(SqliteConnection connection)
        {
            var daysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            var timeSlots = new[]
            {
                new { StartTime = "09:00", EndTime = "17:00" },
                new { StartTime = "10:00", EndTime = "18:00" },
                new { StartTime = "08:00", EndTime = "16:00" },
                new { StartTime = "11:00", EndTime = "19:00" }
            };

            var insertAvailabilityCmd = connection.CreateCommand();
            insertAvailabilityCmd.CommandText = @"
                INSERT INTO Teacher_Day_Availability (teacher_id, day, available, start_time, end_time, created_at, updated_at)
                VALUES (@teacherId, @day, @available, @startTime, @endTime, @createdAt, @updatedAt)";

            var now = DateTime.UtcNow.ToString("o");

            // Generate availability for each teacher (1-8)
            for (int teacherId = 1; teacherId <= 8; teacherId++)
            {
                var timeSlot = timeSlots[(teacherId - 1) % timeSlots.Length];
                
                foreach (var day in daysOfWeek)
                {
                    insertAvailabilityCmd.Parameters.Clear();
                    insertAvailabilityCmd.Parameters.AddWithValue("@teacherId", teacherId);
                    insertAvailabilityCmd.Parameters.AddWithValue("@day", day);
                    insertAvailabilityCmd.Parameters.AddWithValue("@available", 1);
                    insertAvailabilityCmd.Parameters.AddWithValue("@startTime", timeSlot.StartTime);
                    insertAvailabilityCmd.Parameters.AddWithValue("@endTime", timeSlot.EndTime);
                    insertAvailabilityCmd.Parameters.AddWithValue("@createdAt", now);
                    insertAvailabilityCmd.Parameters.AddWithValue("@updatedAt", now);
                    insertAvailabilityCmd.ExecuteNonQuery();
                }
            }
        }

        private void SeedStudentStudying(SqliteConnection connection)
        {
            var relationships = new[]
            {
                new { StudentId = 1, TeacherId = 1, Day = "Monday" },
                new { StudentId = 1, TeacherId = 1, Day = "Wednesday" },
                new { StudentId = 2, TeacherId = 2, Day = "Tuesday" },
                new { StudentId = 2, TeacherId = 2, Day = "Thursday" },
                new { StudentId = 3, TeacherId = 4, Day = "Monday" },
                new { StudentId = 3, TeacherId = 4, Day = "Friday" },
                new { StudentId = 4, TeacherId = 3, Day = "Tuesday" },
                new { StudentId = 4, TeacherId = 3, Day = "Thursday" },
                new { StudentId = 5, TeacherId = 5, Day = "Wednesday" },
                new { StudentId = 5, TeacherId = 5, Day = "Friday" }
            };

            var insertRelationshipCmd = connection.CreateCommand();
            insertRelationshipCmd.CommandText = @"
                INSERT INTO Student_Studying (student_id, teacher_id, day, created_at)
                VALUES (@studentId, @teacherId, @day, @createdAt)";

            var now = DateTime.UtcNow.ToString("o");

            foreach (var relationship in relationships)
            {
                insertRelationshipCmd.Parameters.Clear();
                insertRelationshipCmd.Parameters.AddWithValue("@studentId", relationship.StudentId);
                insertRelationshipCmd.Parameters.AddWithValue("@teacherId", relationship.TeacherId);
                insertRelationshipCmd.Parameters.AddWithValue("@day", relationship.Day);
                insertRelationshipCmd.Parameters.AddWithValue("@createdAt", now);
                insertRelationshipCmd.ExecuteNonQuery();
            }
        }
    }
}
