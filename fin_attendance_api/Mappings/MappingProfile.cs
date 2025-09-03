using AutoMapper;
using Dtos.BreakTime;
using Dtos.Compensatory;
using Dtos.Manager;
using Dtos.Personal;
using Dtos.ShiftDto;
using Dtos.TimeIntervalDto;
using Entities.Compensatory;
using Entities.Manager;
using Entities.Personal;
using Entities.Shifts;
using FibAttendanceApi.Util;

namespace FibAttendanceApi.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<AppUser, AppUserDto>();

            // New Mappings for Permission
            CreateMap<Permission, PermissionDto>();
            CreateMap<PermissionCreateDto, Permission>();
            // For update, we map DTO to entity, AutoMapper will handle nulls if properties are nullable in entity
            CreateMap<PermissionUpdateDto, Permission>()
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null)); // Only map non-null properties from DTO

            // New Mappings for UserPermission
            // Note: For UserPermissionDto, we are manually projecting in the controller to include related data (UserName, PermissionKey, PermissionName)
            // However, a basic mapping from entity to DTO is still good practice.
            CreateMap<UserPermission, UserPermissionDto>();


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

                // --- NUEVOS MAPEOS AÑADIDOS ---
                .ForMember(dest => dest.PorcentajeNivel1, opt => opt.MapFrom(src => src.OvertimeLv1Percentage))
                .ForMember(dest => dest.PorcentajeNivel2, opt => opt.MapFrom(src => src.OvertimeLv2Percentage))
                .ForMember(dest => dest.PorcentajeNivel3, opt => opt.MapFrom(src => src.OvertimeLv3Percentage))
                .ForMember(dest => dest.CompaniaId, opt => opt.MapFrom(src => src.CompaniaId))
                // --- FIN DE NUEVOS MAPEOS ---

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


            // 2. Mapeo ACTUALIZADO para el horario (Entidad -> DTO principal)
            CreateMap<AttTimeinterval, AttTimeIntervalDto>()
            // ... other .ForMember mappings

            // This mapping for Breaks is correct
            .ForMember(dest => dest.Breaks,
                       opt => opt.MapFrom(src => src.AttTimeintervalBreakTimes.Select(b => b.Breaktime)))

            // Simplified mapping for PunchInWindow
            .ForMember(dest => dest.PunchInWindow, opt => opt.MapFrom(src =>
                $"{src.InTime.AddMinutes(-src.InAheadMargin):HH:mm} - {src.InTime.AddMinutes(src.InAboveMargin):HH:mm}"
            ))

            // CORRECTED: Mapping for PunchOutWindow as a single expression
            .ForMember(dest => dest.PunchOutWindow, opt => opt.MapFrom(src =>
                $"{src.InTime.AddMinutes(src.Duration - src.OutAheadMargin):HH:mm} - {src.InTime.AddMinutes(src.Duration + src.OutAboveMargin):HH:mm}"
            ));


            // ... Mapeos para Create y Update (estos no cambian)
            CreateMap<AttTimeIntervalCreateDto, AttTimeinterval>()
                .ForMember(desc=>desc.CompaniaId,opt=>opt.MapFrom(src=>
                src.CompanyId))
                .ForMember(dest => dest.AttTimeintervalBreakTimes, opt => opt.Ignore());

            CreateMap<AttTimeIntervalUpdateDto, AttTimeinterval>()
                .ForMember(desc => desc.CompaniaId, opt => opt.MapFrom(src =>
                src.CompanyId))
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.AttTimeintervalBreakTimes, opt => opt.Ignore());

            ///
            // 1. NUEVO: Mapeo para el descanso (Entidad -> DTO)
            CreateMap<AttBreaktime, BreakTimeDto>();

            // In your MappingProfile.cs

            // Assumes a BreakTimeDto exists and has a mapping from AttBreaktime
            CreateMap<AttBreaktime, BreakTimeDto>();

            // Main mapping from the AttTimeinterval entity to the new read DTO
            CreateMap<AttTimeinterval, TimeIntervalDetailDto>()
                .ForMember(dest => dest.FormattedStartTime,
                           opt => opt.MapFrom(src => src.InTime.ToString("HH:mm")))
                .ForMember(dest => dest.CompanyId,
                           opt => opt.MapFrom(src => src.CompaniaId))
                .ForMember(dest => dest.TotalDurationMinutes,
                           opt => opt.MapFrom(src => src.Duration))
                .ForMember(dest => dest.ScheduledEndTime,
                           opt => opt.MapFrom(src => src.InTime.AddMinutes(src.Duration).ToString("HH:mm")))
                .ForMember(dest => dest.NormalWorkDay,
                           opt => opt.MapFrom(src => $"{src.WorkTimeDuration / 60}h {src.WorkTimeDuration % 60}m"))
                .ForMember(dest => dest.Breaks,
                           opt => opt.MapFrom(src => src.AttTimeintervalBreakTimes.Select(b => b.Breaktime)));

            // En MappingProfile.cs

            // Mapeo para el detalle
            CreateMap<AttShiftdetail, ShiftDayDto>()
                .ForMember(dest => dest.TimeIntervalAlias, opt => opt.MapFrom(src => src.TimeInterval.Alias))
                .ForMember(dest=>dest.TimeIntervalId,opt=>opt.MapFrom(src=>src.TimeInterval.Id))
                .ForMember(dest => dest.InTime, opt => opt.MapFrom(src => src.TimeInterval.InTime.ToString("HH:mm")))

                // Calcula la hora de salida usando la DURACIÓN TOTAL
                .ForMember(dest => dest.OutTime, opt => opt.MapFrom(src =>
                    src.TimeInterval.InTime.AddMinutes(src.TimeInterval.Duration).ToString("HH:mm")))

                // Calcula las horas de trabajo normal
                .ForMember(dest => dest.WorkHours, opt => opt.MapFrom(src =>
                    FormatMinutes(src.TimeInterval.WorkTimeDuration)))

                // Calcula las horas de descanso
                .ForMember(dest => dest.BreakHours, opt => opt.MapFrom(src =>
                    FormatMinutes(src.TimeInterval.Duration - src.TimeInterval.WorkTimeDuration -
                                  (src.TimeInterval.OvertimeLv1 + src.TimeInterval.OvertimeLv2 + src.TimeInterval.OvertimeLv3))))

                // Calcula el total de horas extras programadas
                .ForMember(dest => dest.OvertimeHours, opt => opt.MapFrom(src =>
                    FormatMinutes(src.TimeInterval.OvertimeLv1 + src.TimeInterval.OvertimeLv2 + src.TimeInterval.OvertimeLv3)))

                // Muestra la duración total del turno para ese día
                .ForMember(dest => dest.TotalDuration, opt => opt.MapFrom(src =>
                    FormatMinutes(src.TimeInterval.Duration)));

            // Mapeo para el turno principal
            CreateMap<AttAttshift, ShiftListDto>()
                .ForMember(dest => dest.Horario, opt => opt.MapFrom(src => src.AttShiftdetails));



            /**
             * 
             * personal dto
             */
            CreateMap<PersonalEntity, PersonalDto>();

            CreateMap<CreatePersonalDto, PersonalEntity>();

                        CreateMap<UpdatePersonalDto, PersonalEntity>()
                .ForMember(dest => dest.StartDate, opt => opt.Condition(src => src.StartDate.HasValue));

            // Compensatory Day Mappings
            CreateMap<CompensatoryDay, CompensatoryDayDto>()
                .ForMember(dest => dest.EmployeeFullName, opt => opt.MapFrom(src => src.EmployeeAssignment.FullNameEmployee))
                .ForMember(dest => dest.EmployeeArea, opt => opt.MapFrom(src => src.EmployeeAssignment.AreaDescription))
                .ForMember(dest => dest.EmployeeLocation, opt => opt.MapFrom(src => src.EmployeeAssignment.LocationName));
            CreateMap<CreateCompensatoryDayDto, CompensatoryDay>();
            CreateMap<UpdateCompensatoryDayDto, CompensatoryDay>();


            // Maps from the Create/Edit DTO to the main Entity.
            // This is used when a user sends data to create or update a record.
            CreateMap<PersonnelWhitelistCreateEditDto, PersonnelWhitelist>();

            // Maps from the main Entity to the Create/Edit DTO.
            // This is useful when you fetch a record from the database
            // to show it in an "edit" form.
            //CreateMap<PersonnelWhitelist, PersonnelWhitelistCreateEditDto>();

            CreateMap<PersonnelWhitelist, PersonnelWhitelistDto>();



        }

  

        private string FormatMinutes(int totalMinutes)
        {
            if (totalMinutes < 0) totalMinutes = 0;
            var hours = totalMinutes / 60;
            var minutes = totalMinutes % 60;
            return $"{hours}h {minutes}m";
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