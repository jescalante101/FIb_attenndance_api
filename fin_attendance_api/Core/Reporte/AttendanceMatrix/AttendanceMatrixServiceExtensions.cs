namespace FibAttendanceApi.Core.Reporte.AttendanceMatrix
{
    /// <summary>
    /// Extensiones para configurar servicios en Startup.cs
    /// </summary>
    public static class AttendanceMatrixServiceExtensions
    {
        public static IServiceCollection AddAttendanceMatrixServices(this IServiceCollection services)
        {
            services.AddScoped<IAttendanceMatrixService, AttendanceMatrixService>();
            return services;
        }
    }
}
