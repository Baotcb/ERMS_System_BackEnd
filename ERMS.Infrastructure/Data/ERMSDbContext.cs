using ERMS.Application.Interface;
using ERMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ERMS.Infrastructure.Data
{
    
    public class ERMSDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IERMSDbContext
    {
        public ERMSDbContext(DbContextOptions<ERMSDbContext> options) : base(options)
        {
        }
        public DbSet<Department> Departments { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>().ToTable("Users");
            builder.Entity<IdentityRole<Guid>>().ToTable("Roles");
            builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");

            builder.Entity<User>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Department>()
                .HasOne(d => d.Manager)
                .WithOne()
                .HasForeignKey<Department>(d => d.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
