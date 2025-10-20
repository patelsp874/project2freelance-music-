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

                // Seed users (teachers and students)
                SeedUsers(connection);

                // Seed teacher profiles
                SeedTeacherProfiles(connection);

                // Seed teacher availability
                SeedTeacherAvailability(connection);

                // Seed some existing lessons
                SeedLessons(connection);
            }
        }

        private void ClearExistingData(SqliteConnection connection)
        {
            var clearCommands = new[]
            {
                "DELETE FROM Lessons",
                "DELETE FROM TeacherAvailability",
                "DELETE FROM TeacherProfiles",
                "DELETE FROM Sessions",
                "DELETE FROM Users"
            };

            foreach (var cmd in clearCommands)
            {
                var command = connection.CreateCommand();
                command.CommandText = cmd;
                command.ExecuteNonQuery();
            }
        }

        private void SeedUsers(SqliteConnection connection)
        {
            var users = new[]
            {
                // Teachers
                new { FirstName = "Sarah", LastName = "Johnson", Email = "sarah.johnson@music.com", Password = "password123", AccountType = "teacher" },
                new { FirstName = "Michael", LastName = "Chen", Email = "michael.chen@music.com", Password = "password123", AccountType = "teacher" },
                new { FirstName = "Emily", LastName = "Rodriguez", Email = "emily.rodriguez@music.com", Password = "password123", AccountType = "teacher" },
                new { FirstName = "David", LastName = "Williams", Email = "david.williams@music.com", Password = "password123", AccountType = "teacher" },
                new { FirstName = "Lisa", LastName = "Anderson", Email = "lisa.anderson@music.com", Password = "password123", AccountType = "teacher" },
                new { FirstName = "James", LastName = "Taylor", Email = "james.taylor@music.com", Password = "password123", AccountType = "teacher" },
                new { FirstName = "Maria", LastName = "Garcia", Email = "maria.garcia@music.com", Password = "password123", AccountType = "teacher" },
                new { FirstName = "Robert", LastName = "Brown", Email = "robert.brown@music.com", Password = "password123", AccountType = "teacher" },
                
                // Students
                new { FirstName = "Alex", LastName = "Smith", Email = "alex.smith@student.com", Password = "password123", AccountType = "student" },
                new { FirstName = "Emma", LastName = "Davis", Email = "emma.davis@student.com", Password = "password123", AccountType = "student" },
                new { FirstName = "Noah", LastName = "Wilson", Email = "noah.wilson@student.com", Password = "password123", AccountType = "student" },
                new { FirstName = "Olivia", LastName = "Martinez", Email = "olivia.martinez@student.com", Password = "password123", AccountType = "student" },
                new { FirstName = "Liam", LastName = "Thompson", Email = "liam.thompson@student.com", Password = "password123", AccountType = "student" },
                new { FirstName = "Sophia", LastName = "White", Email = "sophia.white@student.com", Password = "password123", AccountType = "student" },
                new { FirstName = "William", LastName = "Harris", Email = "william.harris@student.com", Password = "password123", AccountType = "student" },
                new { FirstName = "Isabella", LastName = "Clark", Email = "isabella.clark@student.com", Password = "password123", AccountType = "student" }
            };

            var insertUserCmd = connection.CreateCommand();
            insertUserCmd.CommandText = @"
                INSERT INTO Users (FirstName, LastName, Email, Password, AccountType, CreatedAt)
                VALUES (@firstName, @lastName, @email, @password, @accountType, @createdAt)";

            foreach (var user in users)
            {
                insertUserCmd.Parameters.Clear();
                insertUserCmd.Parameters.AddWithValue("@firstName", user.FirstName);
                insertUserCmd.Parameters.AddWithValue("@lastName", user.LastName);
                insertUserCmd.Parameters.AddWithValue("@email", user.Email);
                insertUserCmd.Parameters.AddWithValue("@password", user.Password);
                insertUserCmd.Parameters.AddWithValue("@accountType", user.AccountType);
                insertUserCmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
                insertUserCmd.ExecuteNonQuery();
            }
        }

        private void SeedTeacherProfiles(SqliteConnection connection)
        {
            var teacherProfiles = new[]
            {
                new { UserId = 1, Name = "Sarah Johnson", Instrument = "Piano", Bio = "Classical pianist with 15 years of teaching experience. Specializes in classical and contemporary piano techniques.", ContactInfo = "sarah.johnson@music.com | (555) 123-4567" },
                new { UserId = 2, Name = "Michael Chen", Instrument = "Guitar", Bio = "Professional guitarist and music producer. Expert in acoustic, electric, and classical guitar styles.", ContactInfo = "michael.chen@music.com | (555) 234-5678" },
                new { UserId = 3, Name = "Emily Rodriguez", Instrument = "Violin", Bio = "Orchestral violinist with extensive teaching background. Focuses on classical technique and performance skills.", ContactInfo = "emily.rodriguez@music.com | (555) 345-6789" },
                new { UserId = 4, Name = "David Williams", Instrument = "Drums", Bio = "Session drummer and percussion instructor. Specializes in rock, jazz, and world music styles.", ContactInfo = "david.williams@music.com | (555) 456-7890" },
                new { UserId = 5, Name = "Lisa Anderson", Instrument = "Voice", Bio = "Opera singer and vocal coach. Helps students develop proper technique and performance confidence.", ContactInfo = "lisa.anderson@music.com | (555) 567-8901" },
                new { UserId = 6, Name = "James Taylor", Instrument = "Bass", Bio = "Professional bassist with jazz and rock background. Teaches both electric and upright bass techniques.", ContactInfo = "james.taylor@music.com | (555) 678-9012" },
                new { UserId = 7, Name = "Maria Garcia", Instrument = "Flute", Bio = "Classical flutist and chamber music specialist. Experienced in teaching all skill levels.", ContactInfo = "maria.garcia@music.com | (555) 789-0123" },
                new { UserId = 8, Name = "Robert Brown", Instrument = "Saxophone", Bio = "Jazz saxophonist and music educator. Specializes in jazz improvisation and contemporary styles.", ContactInfo = "robert.brown@music.com | (555) 890-1234" }
            };

            var insertProfileCmd = connection.CreateCommand();
            insertProfileCmd.CommandText = @"
                INSERT INTO TeacherProfiles (UserId, Name, Instrument, Bio, ContactInfo, CreatedAt, UpdatedAt)
                VALUES (@userId, @name, @instrument, @bio, @contactInfo, @createdAt, @updatedAt)";

            foreach (var profile in teacherProfiles)
            {
                insertProfileCmd.Parameters.Clear();
                insertProfileCmd.Parameters.AddWithValue("@userId", profile.UserId);
                insertProfileCmd.Parameters.AddWithValue("@name", profile.Name);
                insertProfileCmd.Parameters.AddWithValue("@instrument", profile.Instrument);
                insertProfileCmd.Parameters.AddWithValue("@bio", profile.Bio);
                insertProfileCmd.Parameters.AddWithValue("@contactInfo", profile.ContactInfo);
                insertProfileCmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
                insertProfileCmd.Parameters.AddWithValue("@updatedAt", DateTime.UtcNow.ToString("o"));
                insertProfileCmd.ExecuteNonQuery();
            }
        }

        private void SeedTeacherAvailability(SqliteConnection connection)
        {
            var daysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
            var timeSlots = new[]
            {
                new { StartTime = "09:00", EndTime = "10:00" },
                new { StartTime = "10:00", EndTime = "11:00" },
                new { StartTime = "11:00", EndTime = "12:00" },
                new { StartTime = "14:00", EndTime = "15:00" },
                new { StartTime = "15:00", EndTime = "16:00" },
                new { StartTime = "16:00", EndTime = "17:00" },
                new { StartTime = "17:00", EndTime = "18:00" },
                new { StartTime = "18:00", EndTime = "19:00" },
                new { StartTime = "19:00", EndTime = "20:00" }
            };

            var insertAvailabilityCmd = connection.CreateCommand();
            insertAvailabilityCmd.CommandText = @"
                INSERT INTO TeacherAvailability (TeacherId, DayOfWeek, StartTime, EndTime, IsAvailable, CreatedAt, UpdatedAt)
                VALUES (@teacherId, @dayOfWeek, @startTime, @endTime, @isAvailable, @createdAt, @updatedAt)";

            var random = new Random();
            var now = DateTime.UtcNow.ToString("o");

            // Generate availability for each teacher (1-8)
            for (int teacherId = 1; teacherId <= 8; teacherId++)
            {
                foreach (var day in daysOfWeek)
                {
                    // Each teacher has 3-6 random time slots per day
                    var numSlots = random.Next(3, 7);
                    var selectedSlots = timeSlots.OrderBy(x => random.Next()).Take(numSlots);

                    foreach (var slot in selectedSlots)
                    {
                        insertAvailabilityCmd.Parameters.Clear();
                        insertAvailabilityCmd.Parameters.AddWithValue("@teacherId", teacherId);
                        insertAvailabilityCmd.Parameters.AddWithValue("@dayOfWeek", day);
                        insertAvailabilityCmd.Parameters.AddWithValue("@startTime", slot.StartTime);
                        insertAvailabilityCmd.Parameters.AddWithValue("@endTime", slot.EndTime);
                        insertAvailabilityCmd.Parameters.AddWithValue("@isAvailable", true);
                        insertAvailabilityCmd.Parameters.AddWithValue("@createdAt", now);
                        insertAvailabilityCmd.Parameters.AddWithValue("@updatedAt", now);
                        insertAvailabilityCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void SeedLessons(SqliteConnection connection)
        {
            var lessons = new[]
            {
                new { TeacherId = 1, StudentId = 9, StudentName = "Alex Smith", Instrument = "Piano", LessonDate = "2024-01-15", LessonTime = "10:00", LessonType = "Beginner" },
                new { TeacherId = 2, StudentId = 10, StudentName = "Emma Davis", Instrument = "Guitar", LessonDate = "2024-01-16", LessonTime = "15:00", LessonType = "Intermediate" },
                new { TeacherId = 3, StudentId = 11, StudentName = "Noah Wilson", Instrument = "Violin", LessonDate = "2024-01-17", LessonTime = "16:00", LessonType = "Advanced" },
                new { TeacherId = 4, StudentId = 12, StudentName = "Olivia Martinez", Instrument = "Drums", LessonDate = "2024-01-18", LessonTime = "14:00", LessonType = "Beginner" },
                new { TeacherId = 5, StudentId = 13, StudentName = "Liam Thompson", Instrument = "Voice", LessonDate = "2024-01-19", LessonTime = "17:00", LessonType = "Intermediate" }
            };

            var insertLessonCmd = connection.CreateCommand();
            insertLessonCmd.CommandText = @"
                INSERT INTO Lessons (TeacherId, StudentId, StudentName, Instrument, LessonDate, LessonTime, LessonType, Status, CreatedAt, UpdatedAt)
                VALUES (@teacherId, @studentId, @studentName, @instrument, @lessonDate, @lessonTime, @lessonType, 'scheduled', @createdAt, @updatedAt)";

            var now = DateTime.UtcNow.ToString("o");

            foreach (var lesson in lessons)
            {
                insertLessonCmd.Parameters.Clear();
                insertLessonCmd.Parameters.AddWithValue("@teacherId", lesson.TeacherId);
                insertLessonCmd.Parameters.AddWithValue("@studentId", lesson.StudentId);
                insertLessonCmd.Parameters.AddWithValue("@studentName", lesson.StudentName);
                insertLessonCmd.Parameters.AddWithValue("@instrument", lesson.Instrument);
                insertLessonCmd.Parameters.AddWithValue("@lessonDate", lesson.LessonDate);
                insertLessonCmd.Parameters.AddWithValue("@lessonTime", lesson.LessonTime);
                insertLessonCmd.Parameters.AddWithValue("@lessonType", lesson.LessonType);
                insertLessonCmd.Parameters.AddWithValue("@createdAt", now);
                insertLessonCmd.Parameters.AddWithValue("@updatedAt", now);
                insertLessonCmd.ExecuteNonQuery();
            }
        }
    }
}
