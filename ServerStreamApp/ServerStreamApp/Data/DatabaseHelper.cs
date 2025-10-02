using Microsoft.Data.Sqlite;
using ServerStreamApp.Models;

namespace ServerStreamApp.Data
{
    public class DatabaseHelper
    {
        private readonly string connectionString;

        public DatabaseHelper()
        {
            // SỬ DỤNG DATABASE CÓ SẴN
            string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "StreamApp.db");
            connectionString = $"Data Source={dbPath}";

            // Debug info
            System.Diagnostics.Debug.WriteLine($"Database path: {dbPath}");
            System.Diagnostics.Debug.WriteLine($"Database exists: {File.Exists(dbPath)}");

            // Copy database từ project nếu chưa có trong output
            EnsureDatabaseExists();
        }

        private void EnsureDatabaseExists()
        {
            try
            {
                string outputDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "StreamApp.db");

                // Tạo thư mục Data nếu chưa có
                string outputDataDir = Path.GetDirectoryName(outputDbPath);
                if (!Directory.Exists(outputDataDir))
                {
                    Directory.CreateDirectory(outputDataDir);
                    System.Diagnostics.Debug.WriteLine($"Created directory: {outputDataDir}");
                }

                // Nếu database chưa có trong output directory
                if (!File.Exists(outputDbPath))
                {
                    // Tìm database trong project
                    string projectRoot = GetProjectRoot();
                    string sourceDbPath = Path.Combine(projectRoot, "Data", "StreamApp.db");

                    if (File.Exists(sourceDbPath))
                    {
                        File.Copy(sourceDbPath, outputDbPath, true);
                        System.Diagnostics.Debug.WriteLine($"Copied database from {sourceDbPath} to {outputDbPath}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Source database not found at: {sourceDbPath}");
                        System.Diagnostics.Debug.WriteLine("Database will be loaded from Copy to Output Directory setting");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error ensuring database exists: {ex.Message}");
            }
        }

        private string GetProjectRoot()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            DirectoryInfo dir = new DirectoryInfo(currentDir);

            // Đi ngược lên để tìm thư mục project (chứa file .csproj)
            while (dir != null && !dir.GetFiles("*.csproj").Any())
            {
                dir = dir.Parent;
            }

            return dir?.FullName ?? currentDir;
        }

        public User? ValidateUser(string username, string password)
        {
            try
            {
                // KHÔNG gọi InitializeDatabase nữa - chỉ sử dụng database có sẵn
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "SELECT userId, username, password FROM Users WHERE username = $username AND password = $password";
                command.Parameters.AddWithValue("$username", username);
                command.Parameters.AddWithValue("$password", password);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return new User
                    {
                        UserId = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        Password = reader.GetString(2)
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database error: {ex.Message}");
                return null;
            }
        }

        public void LogLogin(int userId, string ipAddress)
        {
            try
            {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Login_History (userId, login_time, ip_address) VALUES ($userId, datetime('now'), $ipAddress)";
                command.Parameters.AddWithValue("$userId", userId);
                command.Parameters.AddWithValue("$ipAddress", ipAddress);
                command.ExecuteNonQuery();

                System.Diagnostics.Debug.WriteLine($"Login logged for user {userId} from {ipAddress}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Log error: {ex.Message}");
            }
        }

        // Thêm method logging activity chính
        public void LogActivity(ActivityType activityType, int? userId = null, string ipAddress = "")
        {
            try
            {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO ActivityLog (UserId, ActivityType, Timestamp, IpAddress) VALUES ($userId, $activityType, datetime('now'), $ipAddress)";
                
                command.Parameters.AddWithValue("$userId", userId.HasValue ? userId.Value : DBNull.Value);
                command.Parameters.AddWithValue("$activityType", activityType.ToString());
                command.Parameters.AddWithValue("$ipAddress", ipAddress ?? string.Empty);
                
                command.ExecuteNonQuery();

                System.Diagnostics.Debug.WriteLine($"Activity logged: {activityType} - UserId: {userId} - IP: {ipAddress}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Activity log error: {ex.Message}");
            }
        }

        // Overload method cho trường hợp có user object
        public void LogActivity(ActivityType activityType, User user, string ipAddress = "")
        {
            LogActivity(activityType, user?.UserId, ipAddress);
        }

        // Method để lấy activity logs (để hiển thị nếu cần)
        public List<ActivityLog> GetRecentActivityLogs(int limit = 100)
        {
            var logs = new List<ActivityLog>();
            try
            {
                using var connection = new SqliteConnection(connectionString);
                connection.Open();

                var command = connection.CreateCommand();
                command.CommandText = @"
                    SELECT LogId, UserId, ActivityType, Timestamp, IpAddress 
                    FROM ActivityLog 
                    ORDER BY Timestamp DESC 
                    LIMIT $limit";
                command.Parameters.AddWithValue("$limit", limit);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var log = new ActivityLog
                    {
                        LogId = reader.GetInt32(0),
                        UserId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                        ActivityType = Enum.Parse<ActivityType>(reader.GetString(2)),
                        Timestamp = DateTime.Parse(reader.GetString(3)),
                        IpAddress = reader.GetString(4)
                    };
                    logs.Add(log);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get activity logs error: {ex.Message}");
            }
            return logs;
        }
    }
}