using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dtos.Personal
{


    public class PersonnelWhitelistDto
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string? Remarks { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Data Transfer Object (DTO) for creating and editing PersonnelWhitelist records.
    /// It contains only the properties that should be set or modified by the user.
    /// </summary>
    public class PersonnelWhitelistCreateEditDto
    {
        /// <summary>
        /// The employee's unique identifier.
        /// It's required and has a maximum length of 15 characters.
        /// </summary>
        [Required(ErrorMessage = "Employee ID is required.")]
        [StringLength(15, ErrorMessage = "Employee ID cannot be longer than 15 characters.")]
        public string EmployeeId { get; set; }

        /// <summary>
        /// The full name of the employee.
        /// It's required and has a maximum length of 200 characters.
        /// </summary>
        [Required(ErrorMessage = "Employee Name is required.")]
        [StringLength(200, ErrorMessage = "Employee Name cannot be longer than 200 characters.")]
        public string EmployeeName { get; set; }

        /// <summary>
        /// The employee's job title or position.
        /// This field is optional and has a maximum length of 100 characters.
        /// </summary>
        [StringLength(100, ErrorMessage = "Position cannot be longer than 100 characters.")]
        public string Position { get; set; }

        /// <summary>
        /// Optional remarks or reasons for including the employee in the whitelist.
        /// </summary>
        public string Remarks { get; set; }
    }



    public class PaginationFilterPerosalWLDto
    {
        private const int MaxPageSize = 100;

        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;

        [Range(1, MaxPageSize)]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }

        /// <summary>
        /// Texto para filtrar por EmployeeId, EmployeeName o Remarks.
        /// </summary>
        public string? FilterText { get; set; }

        /// <summary>
        /// Campo por el cual ordenar. Valores permitidos: "employeeName", "employeeId", "createdAt".
        /// </summary>
        public string? SortBy { get; set; }

        /// <summary>
        /// Define si el ordenamiento es ascendente (true) o descendente (false).
        /// </summary>
        public bool IsAscending { get; set; } = false; // Por defecto descendente (los más nuevos primero)
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

}
