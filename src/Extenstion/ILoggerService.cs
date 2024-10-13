namespace Extension
{
    public interface ILoggerService
    {
        void InitTable();
        void Log(string method, string requestPath, int statusCode, string request, string response, string ipAddress);
    }
}
