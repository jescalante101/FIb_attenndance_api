using AutoMapper;
using Dtos.ShiftDto;
using Dtos.TimeIntervalDto;
using Entities.Shifts;
using FibAttendanceApi.Util;

namespace FibAttendanceApi
{
    public class MappingProfile:Profile
    {
        public MappingProfile()
        {
            CreateMap<AttBreaktime, BreakTimeInfoDto>()
                  .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                 .ForMember(dest => dest.Alias, opt => opt.MapFrom(src => src.Alias))
                 .ForMember(dest => dest.PeriodStart, opt => opt.MapFrom(src => src.PeriodStart.ToString("HH:mm:ss")))
                 .ForMember(dest => dest.Duration, opt => opt.MapFrom(src => src.Duration))
                 .ForMember(dest => dest.FuncKey, opt => opt.MapFrom(src => src.FuncKey))
                 .ForMember(dest => dest.AvailableInterval, opt => opt.MapFrom(src => src.AvailableInterval))
                 .ForMember(dest => dest.AvailableIntervalType, opt => opt.MapFrom(src => src.AvailableIntervalType))
                 .ForMember(dest => dest.MultiplePunch, opt => opt.MapFrom(src => src.MultiplePunch))
                 .ForMember(dest => dest.CalcType, opt => opt.MapFrom(src => src.CalcType))
                 .ForMember(dest => dest.MinimumDuration, opt => opt.MapFrom(src => src.MinimumDuration))
                 .ForMember(dest => dest.EarlyIn, opt => opt.MapFrom(src => src.EarlyIn))
                 .ForMember(dest => dest.EndMargin, opt => opt.MapFrom(src => src.PeriodStart.AddMinutes(src.EndMargin).ToString("HH:mm:ss")))
                 .ForMember(dest => dest.LateIn, opt => opt.MapFrom(src => src.LateIn))
                 .ForMember(dest => dest.MinEarlyIn, opt => opt.MapFrom(src => src.MinEarlyIn))
                 .ForMember(dest => dest.MinLateIn, opt => opt.MapFrom(src => src.MinLateIn))
                 ;


            CreateMap<AttTimeinterval, HorarioInfoDto>()
                .ForMember(dest => dest.IdHorio, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.Alias))
                .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => src.UseMode))
                .ForMember(dest => dest.HoraEntrada, opt => opt.MapFrom(src => src.InTime.ToString("HH:mm:ss")))
                .ForMember(dest => dest.HoraSalida, opt => opt.MapFrom(src => Utils.AgregarMinutos(src.WorkTimeDuration, src.InTime)))

                .ForMember(dest => dest.HoraEntradaDesde, opt => opt.MapFrom(src => Utils.RestarMinutos(src.InAheadMargin, src.InTime)))
                .ForMember(dest => dest.HoraEntradaHasta, opt => opt.MapFrom(src => Utils.AgregarMinutos(src.InAboveMargin, src.InTime)))
                .ForMember(dest => dest.HoraSalidaDesde, opt => opt.MapFrom(src => Utils.AgregarMinutos(src.WorkTimeDuration - src.OutAheadMargin, src.InTime)))
                .ForMember(dest => dest.HoraSalidaHasta, opt => opt.MapFrom(src => Utils.AgregarMinutos(src.WorkTimeDuration + src.OutAboveMargin, src.InTime)))


                .ForMember(dest => dest.TiempoTrabajo, opt => opt.MapFrom(src => src.WorkTimeDuration))
                .ForMember(dest => dest.Descanso, opt => opt.MapFrom(src => GetBreakDuration(src)))
                .ForMember(dest => dest.DiasLaboral, opt => opt.MapFrom(src => src.WorkDay))
                .ForMember(dest => dest.TipoTrabajo, opt => opt.MapFrom(src => GetWorkType(src.WorkType)))
                .ForMember(dest => dest.EntradaTemprana, opt => opt.MapFrom(src => src.EarlyIn))
                .ForMember(dest => dest.EntradaTarde, opt => opt.MapFrom(src => src.LateOut))
                .ForMember(dest => dest.MinEntradaTemprana, opt => opt.MapFrom(src => src.MinEarlyIn))
                .ForMember(dest => dest.MinSalidaTarde, opt => opt.MapFrom(src => src.MinLateOut))
                .ForMember(dest => dest.Hnivel, opt => opt.MapFrom(src => src.OvertimeLv))
                .ForMember(dest => dest.HNivel1, opt => opt.MapFrom(src => src.OvertimeLv1))
                .ForMember(dest => dest.HNivel2, opt => opt.MapFrom(src => src.OvertimeLv2))
                .ForMember(dest => dest.HNivel3, opt => opt.MapFrom(src => src.OvertimeLv3))
                .ForMember(dest => dest.MarcarEntrada, opt => opt.MapFrom(src => src.InRequired))
                .ForMember(dest => dest.MarcarSalida, opt => opt.MapFrom(src => src.OutRequired))
                .ForMember(dest => dest.PLlegadaT, opt => opt.MapFrom(src => src.AllowLeaveEarly))
                .ForMember(dest => dest.PSalidaT, opt => opt.MapFrom(src => src.AllowLate))
                .ForMember(dest => dest.TipoIntervalo, opt => opt.MapFrom(src => src.AvailableIntervalType))
                .ForMember(dest => dest.PeriodoMarcacion, opt => opt.MapFrom(src => src.AvailableInterval))
                .ForMember(dest => dest.HCambioDia, opt => opt.MapFrom(src => Utils.ExtraerHoras(src.DayChange)))
                .ForMember(dest => dest.BasadoM, opt => opt.MapFrom(src => src.MultiplePunch))
                .ForMember(dest => dest.TotalMarcaciones, opt => opt.MapFrom(src => src.TotalMarkings))
                ;

        }

        private int GetBreakDuration(AttTimeinterval source)
        {
            var breakTime = source.AttTimeintervalBreakTimes.FirstOrDefault();
            return breakTime?.Breaktime?.Duration ?? 0;
        }

        private string GetWorkType(int workType)
        {
            if (workType == 0)
            {
                return "Trabajo normal";
            }
            else if (workType == 1)
            {
                return "Día libre";
            }
            else
            {
                return "F.Semanal";
            }
        }
    }
}
