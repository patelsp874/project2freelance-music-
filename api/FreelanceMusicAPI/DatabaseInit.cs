using System;
using FreelanceMusicAPI.Database;

namespace FreelanceMusicAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("🎵 Freelance Music Database Initializer");
            Console.WriteLine("==========================================");
            
            try
            {
                // Initialize the database
                DatabaseInitializer.InitializeDatabase();
                
                Console.WriteLine("\n🧪 Testing database connection...");
                DatabaseInitializer.TestConnection();
                
                Console.WriteLine("\n✅ Database setup completed successfully!");
                Console.WriteLine("📁 Database file location: freelancemusic.db");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
