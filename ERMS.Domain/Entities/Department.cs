using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace ERMS.Domain.Entities
{
    public class Department
    {
        public Guid DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public ICollection<User> Users { get; set; } = new List<User>();




        public Guid? ManagerId { get; set; }
        public User? Manager { get; set; }

    }
}
