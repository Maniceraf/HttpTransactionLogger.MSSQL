using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using static Dapper.SqlMapper;

namespace Extension
{
    public class LoggerService : ILoggerService
    {
        private readonly string _connectionString;
        public LoggerService(IOptions<HttpTransactionLoggerOptions> options)
        {
            _connectionString = options.Value.ConnectionString;
        }

        public void InitTable()
        {
            var query = @"IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AccessLog' AND type = 'U')
                BEGIN
                    CREATE TABLE AccessLog (
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Method NVARCHAR(10) NOT NULL,
                        Url NVARCHAR(255) NOT NULL,
                        StatusCode INT NOT NULL,
                        RequestBody NVARCHAR(MAX),
                        ResponseBody NVARCHAR(MAX),
                        IpAddress NVARCHAR(45),
                        Timestamp DATETIME DEFAULT GETDATE()
                    );
                END";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(query);
            }

            
        }

        public void Log(string method, string requestPath, int statusCode, string request, string response, string ipAddress)
        {
            string sql = @"INSERT INTO AccessLog (Method, Url, StatusCode, RequestBody, ResponseBody, IpAddress)
                VALUES (@Method, @Url, @StatusCode, @RequestBody, @ResponseBody, @IpAddress);";

            object[] parameters = {
                    new
                    {
                        Method = method,
                        Url = requestPath,
                        StatusCode = statusCode,
                        RequestBody = request,
                        ResponseBody = response,
                        IpAddress = ipAddress
                    }
                };

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Execute(sql, parameters);
            }
        }
    }
}
