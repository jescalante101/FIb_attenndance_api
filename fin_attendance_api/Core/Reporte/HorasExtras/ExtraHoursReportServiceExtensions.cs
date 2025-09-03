namespace FibAttendanceApi.Core.Reporte.HorasExtras
{
    public static class ExtraHoursReportServiceExtensions
    {
        public static IServiceCollection AddExtraHoursReportServices(this IServiceCollection services)
        {
            services.AddScoped<IExtraHoursReportService, ExtraHoursReportService>();
            return services;
        }
    }
}