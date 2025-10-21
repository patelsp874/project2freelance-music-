using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace FreelanceMusicAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DatabaseTestController : ControllerBase
    {
        private static string ConnectionString => "Data Source=freelancemusic.db;";

        [HttpGet("test-connection")]
        public IActionResult TestConnection()
        {
            try
            {
                using (var connection = new SqliteConnection(ConnectionString))
                {
                    connection.Open();
                    
                    // Test basic queries
                    var studentCount = 0;
                    var teacherCount = 0;
                    var availabilityCount = 0;
                    var studyingCount = 0;

                    using (var command = new SqliteCommand("SELECT COUNT(*) FROM Student", connection))
                    {
                        studentCount = Convert.ToInt32(command.ExecuteScalar());
                    }

                    using (var command = new SqliteCommand("SELECT COUNT(*) FROM Teacher", connection))
                    {
                        teacherCount = Convert.ToInt32(command.ExecuteScalar());
                    }

                    using (var command = new SqliteCommand("SELECT COUNT(*) FROM Teacher_Day_Availability", connection))
                    {
                        availabilityCount = Convert.ToInt32(command.ExecuteScalar());
                    }

                    using (var command = new SqliteCommand("SELECT COUNT(*) FROM Student_Studying", connection))
                    {
                        studyingCount = Convert.ToInt32(command.ExecuteScalar());
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Database connection successful!",
                        data = new
                        {
                            students = studentCount,
                            teachers = teacherCount,
                            availability_records = availabilityCount,
                            student_teacher_relationships = studyingCount
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Database connection failed",
                    error = ex.Message
                });
            }
        }

        [HttpPost("initialize")]
        public IActionResult InitializeDatabase()
        {
            try
            {
                FreelanceMusicAPI.Database.DatabaseInitializer.InitializeDatabase();
                return Ok(new
                {
                    success = true,
                    message = "Database initialized successfully!"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Database initialization failed",
                    error = ex.Message
                });
            }
        }
    }
}
