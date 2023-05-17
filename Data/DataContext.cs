using System;
using Microsoft.EntityFrameworkCore;
using AceOSSLT_AssessmentAPI.Models;

namespace AceOSSLT_AssessmentAPI.Data
{
	public class DataContext : DbContext
	{
        // Constructor with dependency injection
        public DataContext(DbContextOptions<DataContext> options) : base(options)
		{ }

        // Database tables
        public DbSet<Assessment> Assessments { get; set; }
		public DbSet<Question> Questions { get; set; }
		public DbSet<Option> Options { get; set; }
		public DbSet<Passage> Passages { get; set; }
	}
}

