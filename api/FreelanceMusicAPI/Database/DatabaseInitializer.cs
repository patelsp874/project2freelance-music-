using System;
using Microsoft.Data.Sqlite;
using System.IO;

namespace FreelanceMusicAPI.Database
{
    public class DatabaseInitializer
    {
        private static string ConnectionString => $"Data Source=freelancemusic.db;";

        public static void InitializeDatabase()
        {
            try
            {
                // Only initialize if database file doesn't exist
                if (!File.Exists("freelancemusic.db"))
                {
                    Console.WriteLine("✅ Database file not found, creating: freelancemusic.db");

                    using (var connection = new SqliteConnection(ConnectionString))
                    {
                        connection.Open();
                        Console.WriteLine("✅ Connected to SQLite database");

                        // Enable foreign key constraints
                        ExecuteCommand(connection, "PRAGMA foreign_keys = ON;");

                        // Create Student Table
                        ExecuteCommand(connection, @"
                            CREATE TABLE Student (
                                student_id INTEGER PRIMARY KEY AUTOINCREMENT,
                                student_name TEXT NOT NULL,
                                student_email TEXT NOT NULL UNIQUE,
                                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                            );");

                        // Create Teacher Table
                        ExecuteCommand(connection, @"
                            CREATE TABLE Teacher (
                                teacher_id INTEGER PRIMARY KEY AUTOINCREMENT,
                                teacher_name TEXT NOT NULL,
                                teacher_email TEXT NOT NULL UNIQUE,
                                instrument TEXT NOT NULL,
                                class_full INTEGER DEFAULT 0 CHECK (class_full IN (0, 1)),
                                class_limit INTEGER DEFAULT 10,
                                bio TEXT,
                                charges_per_session DECIMAL(10,2) DEFAULT 0.00,
                                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                            );");

                        // Create Student_Studying Table
                        ExecuteCommand(connection, @"
                            CREATE TABLE Student_Studying (
                                student_id INTEGER NOT NULL,
                                teacher_id INTEGER NOT NULL,
                                day TEXT NOT NULL CHECK (day IN ('Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday')),
                                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                                PRIMARY KEY (student_id, teacher_id, day),
                                FOREIGN KEY (student_id) REFERENCES Student(student_id) ON DELETE CASCADE,
                                FOREIGN KEY (teacher_id) REFERENCES Teacher(teacher_id) ON DELETE CASCADE
                            );");

                        // Create Teacher_Day_Availability Table
                        ExecuteCommand(connection, @"
                            CREATE TABLE Teacher_Day_Availability (
                                teacher_id INTEGER NOT NULL,
                                day TEXT NOT NULL CHECK (day IN ('Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday')),
                                available INTEGER DEFAULT 1 CHECK (available IN (0, 1)),
                                start_time TIME,
                                end_time TIME,
                                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                                updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                                PRIMARY KEY (teacher_id, day),
                                FOREIGN KEY (teacher_id) REFERENCES Teacher(teacher_id) ON DELETE CASCADE
                            );");

                        // Create indexes for better performance
                        ExecuteCommand(connection, "CREATE INDEX idx_student_email ON Student(student_email);");
                        ExecuteCommand(connection, "CREATE INDEX idx_teacher_email ON Teacher(teacher_email);");
                        ExecuteCommand(connection, "CREATE INDEX idx_teacher_instrument ON Teacher(instrument);");
                        ExecuteCommand(connection, "CREATE INDEX idx_student_studying_student ON Student_Studying(student_id);");
                        ExecuteCommand(connection, "CREATE INDEX idx_student_studying_teacher ON Student_Studying(teacher_id);");
                        ExecuteCommand(connection, "CREATE INDEX idx_teacher_availability_teacher ON Teacher_Day_Availability(teacher_id);");
                        ExecuteCommand(connection, "CREATE INDEX idx_teacher_availability_day ON Teacher_Day_Availability(day);");

                        Console.WriteLine("✅ All tables created successfully");

                        // Insert sample data
                        InsertSampleData(connection);

                        Console.WriteLine("✅ Sample data inserted successfully");
                        Console.WriteLine("✅ Database initialization completed!");
                    }
                }
                else
                {
                    Console.WriteLine("✅ Database already exists, skipping initialization");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error initializing database: {ex.Message}");
                throw;
            }
        }

        private static void ExecuteCommand(SqliteConnection connection, string commandText)
        {
            using (var command = new SqliteCommand(commandText, connection))
            {
                command.ExecuteNonQuery();
            }
        }

        private static void InsertSampleData(SqliteConnection connection)
        {
            // Insert sample students
            ExecuteCommand(connection, @"
                INSERT INTO Student (student_name, student_email) VALUES 
                ('John Smith', 'john.smith@email.com'),
                ('Sarah Johnson', 'sarah.johnson@email.com'),
                ('Mike Davis', 'mike.davis@email.com');");

            // Insert sample teachers
            ExecuteCommand(connection, @"
                INSERT INTO Teacher (teacher_name, teacher_email, instrument, class_full, class_limit, bio, charges_per_session) VALUES 
                ('Alice Wilson', 'alice.wilson@email.com', 'Piano', 0, 8, 'Professional pianist with 15 years of teaching experience', 45.00),
                ('Bob Brown', 'bob.brown@email.com', 'Guitar', 0, 10, 'Classical and acoustic guitar specialist', 40.00),
                ('Carol Green', 'carol.green@email.com', 'Violin', 1, 6, 'Orchestral violinist and music theory expert', 50.00),
                ('David Lee', 'david.lee@email.com', 'Drums', 0, 12, 'Jazz and rock drumming instructor', 35.00);");

            // Insert teacher availability
            ExecuteCommand(connection, @"
                INSERT INTO Teacher_Day_Availability (teacher_id, day, available, start_time, end_time) VALUES 
                (1, 'Monday', 1, '09:00', '17:00'),
                (1, 'Tuesday', 1, '09:00', '17:00'),
                (1, 'Wednesday', 1, '09:00', '17:00'),
                (1, 'Thursday', 1, '09:00', '17:00'),
                (1, 'Friday', 1, '09:00', '15:00'),
                (2, 'Monday', 1, '10:00', '18:00'),
                (2, 'Tuesday', 1, '10:00', '18:00'),
                (2, 'Wednesday', 1, '10:00', '18:00'),
                (2, 'Thursday', 1, '10:00', '18:00'),
                (2, 'Friday', 1, '10:00', '16:00'),
                (3, 'Monday', 1, '08:00', '16:00'),
                (3, 'Tuesday', 1, '08:00', '16:00'),
                (3, 'Wednesday', 1, '08:00', '16:00'),
                (3, 'Thursday', 1, '08:00', '16:00'),
                (3, 'Friday', 1, '08:00', '14:00'),
                (4, 'Monday', 1, '11:00', '19:00'),
                (4, 'Tuesday', 1, '11:00', '19:00'),
                (4, 'Wednesday', 1, '11:00', '19:00'),
                (4, 'Thursday', 1, '11:00', '19:00'),
                (4, 'Friday', 1, '11:00', '17:00');");

            // Insert student studying relationships
            ExecuteCommand(connection, @"
                INSERT INTO Student_Studying (student_id, teacher_id, day) VALUES 
                (1, 1, 'Monday'),
                (1, 1, 'Wednesday'),
                (2, 2, 'Tuesday'),
                (2, 2, 'Thursday'),
                (3, 4, 'Monday'),
                (3, 4, 'Friday');");
        }

        public static void TestConnection()
        {
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    Console.WriteLine("✅ Database connection test successful!");

                    // Test basic queries
                    using (var command = new SqliteCommand("SELECT COUNT(*) FROM Student", connection))
                    {
                        var studentCount = command.ExecuteScalar();
                        Console.WriteLine($"📊 Students in database: {studentCount}");
                    }

                    using (var command = new SqliteCommand("SELECT COUNT(*) FROM Teacher", connection))
                    {
                        var teacherCount = command.ExecuteScalar();
                        Console.WriteLine($"📊 Teachers in database: {teacherCount}");
                    }

                    using (var command = new SqliteCommand("SELECT COUNT(*) FROM Teacher_Day_Availability", connection))
                    {
                        var availabilityCount = command.ExecuteScalar();
                        Console.WriteLine($"📊 Availability records: {availabilityCount}");
                    }

                    using (var command = new SqliteCommand("SELECT COUNT(*) FROM Student_Studying", connection))
                    {
                        var studyingCount = command.ExecuteScalar();
                        Console.WriteLine($"📊 Student-Teacher relationships: {studyingCount}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database connection test failed: {ex.Message}");
                throw;
            }
        }
    }
}
