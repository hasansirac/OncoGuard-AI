using Microsoft.EntityFrameworkCore;
using OncoGuard.Domain.Entities;

namespace OncoGuard.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Hospital> Hospitals { get; set; }
    public DbSet<Doctor> Doctors { get; set; }
    public DbSet<Patient> Patients { get; set; }

    public DbSet<CancerProfile> CancerProfiles { get; set; }
    public DbSet<TreatmentPlan> TreatmentPlans { get; set; }
    public DbSet<LabResult> LabResults { get; set; }
    public DbSet<LabCycle> LabCycles { get; set; }

    public DbSet<DailyLog> DailyLogs { get; set; }
    public DbSet<SymptomLog> SymptomLogs { get; set; }
    public DbSet<HydrationLog> HydrationLogs { get; set; }
    public DbSet<MedicationLog> MedicationLogs { get; set; }
    public DbSet<VitalSignsLog> VitalSignsLogs { get; set; }

    public DbSet<FoodLog> FoodLogs { get; set; }
    public DbSet<NutritionSummary> NutritionSummaries { get; set; }
    public DbSet<CalculatedFeature> CalculatedFeatures { get; set; }

    public DbSet<RiskScore> RiskScores { get; set; }
    public DbSet<RiskExplanation> RiskExplanations { get; set; }
    public DbSet<AlertNotification> AlertNotifications { get; set; }
    public DbSet<ClinicalReport> ClinicalReports { get; set; }

    public DbSet<CancerTypeConfig> CancerTypeConfigs { get; set; }
    public DbSet<TreatmentTypeConfig> TreatmentTypeConfigs { get; set; }
    public DbSet<RiskWeight> RiskWeights { get; set; }
    public DbSet<ModelVersion> ModelVersions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>().HasData(
    new Role
    {
        Id = 1,
        Name = "Admin",
        CreatedDate = new DateTime(2026, 1, 1)
    },
    new Role
    {
        Id = 2,
        Name = "Doctor",
        CreatedDate = new DateTime(2026, 1, 1)
    },
    new Role
    {
        Id = 3,
        Name = "Patient",
        CreatedDate = new DateTime(2026, 1, 1)
    }
);

        modelBuilder.Entity<Hospital>().HasData(
            new Hospital
            {
                Id = 1,
                Name = "İstanbul Onkoloji Hastanesi",
                City = "İstanbul",
                CreatedDate = new DateTime(2026, 1, 1)
            },
            new Hospital
            {
                Id = 2,
                Name = "Medipol Hastanesi",
                City = "İstanbul",
                CreatedDate = new DateTime(2026, 1, 1)
            },
            new Hospital
            {
                Id = 3,
                Name = "Acıbadem Hastanesi",
                City = "İstanbul",
                CreatedDate = new DateTime(2026, 1, 1)
            }
        );

        modelBuilder.Entity<Patient>()
            .HasOne(p => p.Hospital)
            .WithMany()
            .HasForeignKey(p => p.HospitalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Patient>()
            .HasOne(p => p.Doctor)
            .WithMany(d => d.Patients)
            .HasForeignKey(p => p.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Doctor>()
            .HasOne(d => d.Hospital)
            .WithMany(h => h.Doctors)
            .HasForeignKey(d => d.HospitalId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CancerProfile>()
        .HasOne(cp => cp.Patient)
        .WithMany()
        .HasForeignKey(cp => cp.PatientId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TreatmentPlan>()
        .HasOne(tp => tp.Patient)
        .WithMany()
        .HasForeignKey(tp => tp.PatientId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LabResult>()
        .HasOne(lr => lr.Patient)
        .WithMany()
        .HasForeignKey(lr => lr.PatientId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LabCycle>()
        .HasOne(lc => lc.Patient)
        .WithMany()
        .HasForeignKey(lc => lc.PatientId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LabCycle>()
        .HasOne(lc => lc.BaselineLab)
        .WithMany()
        .HasForeignKey(lc => lc.BaselineLabId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LabCycle>()
        .HasOne(lc => lc.PreviousCycle)
        .WithMany()
        .HasForeignKey(lc => lc.PreviousCycleId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DailyLog>()
        .HasOne(dl => dl.Patient)
        .WithMany()
        .HasForeignKey(dl => dl.PatientId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SymptomLog>()
        .HasOne(sl => sl.DailyLog)
        .WithOne()
        .HasForeignKey<SymptomLog>(sl => sl.DailyLogId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HydrationLog>()
        .HasOne(hl => hl.DailyLog)
        .WithOne()
        .HasForeignKey<HydrationLog>(hl => hl.DailyLogId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MedicationLog>()
        .HasOne(ml => ml.DailyLog)
        .WithOne()
        .HasForeignKey<MedicationLog>(ml => ml.DailyLogId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<VitalSignsLog>()
        .HasOne(vl => vl.DailyLog)
        .WithOne()
        .HasForeignKey<VitalSignsLog>(vl => vl.DailyLogId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FoodLog>()
    .HasOne(fl => fl.DailyLog)
    .WithMany()
    .HasForeignKey(fl => fl.DailyLogId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NutritionSummary>()
            .HasOne(ns => ns.DailyLog)
            .WithOne()
            .HasForeignKey<NutritionSummary>(ns => ns.DailyLogId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CalculatedFeature>()
            .HasOne(cf => cf.Patient)
            .WithMany()
            .HasForeignKey(cf => cf.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CalculatedFeature>()
            .HasOne(cf => cf.LabCycle)
            .WithMany()
            .HasForeignKey(cf => cf.LabCycleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RiskScore>()
            .HasOne(rs => rs.Patient)
            .WithMany()
            .HasForeignKey(rs => rs.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RiskScore>()
            .HasOne(rs => rs.LabCycle)
            .WithMany()
            .HasForeignKey(rs => rs.LabCycleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RiskExplanation>()
            .HasOne(re => re.RiskScore)
            .WithMany()
            .HasForeignKey(re => re.RiskScoreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AlertNotification>()
            .HasOne(an => an.Patient)
            .WithMany()
            .HasForeignKey(an => an.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AlertNotification>()
            .HasOne(an => an.RiskScore)
            .WithMany()
            .HasForeignKey(an => an.RiskScoreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClinicalReport>()
            .HasOne(cr => cr.Patient)
            .WithMany()
            .HasForeignKey(cr => cr.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClinicalReport>()
            .HasOne(cr => cr.LabCycle)
            .WithMany()
            .HasForeignKey(cr => cr.LabCycleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}