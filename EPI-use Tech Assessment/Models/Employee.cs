//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace EPI_use_Tech_Assessment.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Employee
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Employee()
        {
            this.Passwords = new HashSet<Password>();
        }
    
        public int EmployeeID { get; set; }
        public string FName { get; set; }
        public string LName { get; set; }
        public string DOB { get; set; }
        public Nullable<int> EmpNumber { get; set; }
        public Nullable<decimal> Salary { get; set; }
        public string Position { get; set; }
        public Nullable<int> Manager { get; set; }
        public string email { get; set; }
        public string emailHash { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Password> Passwords { get; set; }
    }
}
