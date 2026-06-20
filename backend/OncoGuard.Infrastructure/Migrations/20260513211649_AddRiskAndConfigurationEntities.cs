using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OncoGuard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskAndConfigurationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CalculatedFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    LabCycleId = table.Column<int>(type: "int", nullable: true),
                    FeatureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProteinRatio = table.Column<double>(type: "float", nullable: true),
                    CalorieRatio = table.Column<double>(type: "float", nullable: true),
                    ProteinDeficit3Day = table.Column<double>(type: "float", nullable: true),
                    ProteinDeficit7Day = table.Column<double>(type: "float", nullable: true),
                    CalorieDeficit7Day = table.Column<double>(type: "float", nullable: true),
                    AppetiteTrend7Day = table.Column<double>(type: "float", nullable: true),
                    MealSkippingFrequency = table.Column<int>(type: "int", nullable: true),
                    WeightLoss7DayPercent = table.Column<double>(type: "float", nullable: true),
                    WeightLoss30DayPercent = table.Column<double>(type: "float", nullable: true),
                    WeightLoss90DayPercent = table.Column<double>(type: "float", nullable: true),
                    CrpAlbuminRatio = table.Column<double>(type: "float", nullable: true),
                    AlbuminTrend = table.Column<double>(type: "float", nullable: true),
                    FunctionalDeclineScore = table.Column<double>(type: "float", nullable: true),
                    AncTrend = table.Column<double>(type: "float", nullable: true),
                    WbcTrend = table.Column<double>(type: "float", nullable: true),
                    LymphocyteTrend = table.Column<double>(type: "float", nullable: true),
                    FeverFlag = table.Column<bool>(type: "bit", nullable: false),
                    FeverAndLowAncFlag = table.Column<bool>(type: "bit", nullable: false),
                    CrpIncreaseFlag = table.Column<bool>(type: "bit", nullable: false),
                    PreviousNeutropeniaFlag = table.Column<bool>(type: "bit", nullable: false),
                    InfectionSymptomScore = table.Column<double>(type: "float", nullable: true),
                    WaterRatio = table.Column<double>(type: "float", nullable: true),
                    VomitingDiarrheaFluidLossScore = table.Column<double>(type: "float", nullable: true),
                    CreatinineTrend = table.Column<double>(type: "float", nullable: true),
                    UreaCreatinineRatio = table.Column<double>(type: "float", nullable: true),
                    ElectrolyteImbalanceScore = table.Column<double>(type: "float", nullable: true),
                    DizzinessDryMouthScore = table.Column<double>(type: "float", nullable: true),
                    AstTrend = table.Column<double>(type: "float", nullable: true),
                    AltTrend = table.Column<double>(type: "float", nullable: true),
                    BilirubinTrend = table.Column<double>(type: "float", nullable: true),
                    EgfrDropFlag = table.Column<bool>(type: "bit", nullable: false),
                    TshAbnormalFlag = table.Column<bool>(type: "bit", nullable: false),
                    FreeT4AbnormalFlag = table.Column<bool>(type: "bit", nullable: false),
                    DiarrheaTrend = table.Column<double>(type: "float", nullable: true),
                    SkinRashTrend = table.Column<double>(type: "float", nullable: true),
                    DyspneaTrend = table.Column<double>(type: "float", nullable: true),
                    MedicationAdherenceScore = table.Column<double>(type: "float", nullable: true),
                    MissedDoseCount3Day = table.Column<int>(type: "int", nullable: true),
                    MissedDoseCount7Day = table.Column<int>(type: "int", nullable: true),
                    AntibioticUseFlag = table.Column<bool>(type: "bit", nullable: false),
                    SteroidUseFlag = table.Column<bool>(type: "bit", nullable: false),
                    AntiemeticUseFlag = table.Column<bool>(type: "bit", nullable: false),
                    MissingLogCount3Day = table.Column<int>(type: "int", nullable: false),
                    MissingLogCount7Day = table.Column<int>(type: "int", nullable: false),
                    MissingNutritionFlag = table.Column<bool>(type: "bit", nullable: false),
                    MissingSymptomFlag = table.Column<bool>(type: "bit", nullable: false),
                    MissingMedicationFlag = table.Column<bool>(type: "bit", nullable: false),
                    PatientBurdenScore = table.Column<double>(type: "float", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculatedFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CalculatedFeatures_LabCycles_LabCycleId",
                        column: x => x.LabCycleId,
                        principalTable: "LabCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CalculatedFeatures_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CancerTypeConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CancerType = table.Column<int>(type: "int", nullable: false),
                    RequiresFrequentInfectionMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    RequiresNutritionMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    RequiresHydrationMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    RequiresPulmonaryMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    RequiresImmunotherapyMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    IsHighCachexiaRisk = table.Column<bool>(type: "bit", nullable: false),
                    RecommendedLabPanel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClinicalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancerTypeConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    LabCycleId = table.Column<int>(type: "int", nullable: true),
                    ReportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReportContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedByAI = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedByDoctor = table.Column<bool>(type: "bit", nullable: false),
                    DoctorComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalReports_LabCycles_LabCycleId",
                        column: x => x.LabCycleId,
                        principalTable: "LabCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalReports_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FoodLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyLogId = table.Column<int>(type: "int", nullable: false),
                    FoodName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AmountGram = table.Column<double>(type: "float", nullable: false),
                    Calories = table.Column<double>(type: "float", nullable: false),
                    Protein = table.Column<double>(type: "float", nullable: false),
                    Carbohydrate = table.Column<double>(type: "float", nullable: false),
                    Fat = table.Column<double>(type: "float", nullable: false),
                    Fiber = table.Column<double>(type: "float", nullable: true),
                    Iron = table.Column<double>(type: "float", nullable: true),
                    VitaminB12 = table.Column<double>(type: "float", nullable: true),
                    Folate = table.Column<double>(type: "float", nullable: true),
                    VitaminD = table.Column<double>(type: "float", nullable: true),
                    Zinc = table.Column<double>(type: "float", nullable: true),
                    Magnesium = table.Column<double>(type: "float", nullable: true),
                    Selenium = table.Column<double>(type: "float", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FoodLogs_DailyLogs_DailyLogId",
                        column: x => x.DailyLogId,
                        principalTable: "DailyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModelVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrainingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DatasetVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationAccuracy = table.Column<double>(type: "float", nullable: true),
                    ValidationF1Score = table.Column<double>(type: "float", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NutritionSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyLogId = table.Column<int>(type: "int", nullable: false),
                    TotalCalories = table.Column<double>(type: "float", nullable: false),
                    TotalProtein = table.Column<double>(type: "float", nullable: false),
                    TotalCarbohydrate = table.Column<double>(type: "float", nullable: false),
                    TotalFat = table.Column<double>(type: "float", nullable: false),
                    TotalWaterMl = table.Column<double>(type: "float", nullable: false),
                    ProteinTarget = table.Column<double>(type: "float", nullable: false),
                    CalorieTarget = table.Column<double>(type: "float", nullable: false),
                    ProteinRatio = table.Column<double>(type: "float", nullable: false),
                    CalorieRatio = table.Column<double>(type: "float", nullable: false),
                    MealCompletionRatio = table.Column<double>(type: "float", nullable: false),
                    IsProteinDeficient = table.Column<bool>(type: "bit", nullable: false),
                    IsCalorieDeficient = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NutritionSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NutritionSummaries_DailyLogs_DailyLogId",
                        column: x => x.DailyLogId,
                        principalTable: "DailyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiskScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    LabCycleId = table.Column<int>(type: "int", nullable: true),
                    CalculationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RiskType = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<double>(type: "float", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
                    RequiresDoctorReview = table.Column<bool>(type: "bit", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskScores_LabCycles_LabCycleId",
                        column: x => x.LabCycleId,
                        principalTable: "LabCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskScores_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiskWeights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskType = table.Column<int>(type: "int", nullable: false),
                    CancerType = table.Column<int>(type: "int", nullable: true),
                    TreatmentType = table.Column<int>(type: "int", nullable: true),
                    FeatureName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WeightValue = table.Column<double>(type: "float", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskWeights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentTypeConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TreatmentType = table.Column<int>(type: "int", nullable: false),
                    RequiresInfectionMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    RequiresLiverMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    RequiresKidneyMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    RequiresEndocrineMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    RequiresCardiacMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    RequiresPulmonaryMonitoring = table.Column<bool>(type: "bit", nullable: false),
                    HasHighNeutropeniaRisk = table.Column<bool>(type: "bit", nullable: false),
                    HasHighCachexiaRisk = table.Column<bool>(type: "bit", nullable: false),
                    HasHighDehydrationRisk = table.Column<bool>(type: "bit", nullable: false),
                    RecommendedSupportiveCare = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClinicalNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentTypeConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AlertNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    RiskScoreId = table.Column<int>(type: "int", nullable: true),
                    RiskType = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    TriggeredByRule = table.Column<bool>(type: "bit", nullable: false),
                    TriggeredByModel = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertNotifications_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AlertNotifications_RiskScores_RiskScoreId",
                        column: x => x.RiskScoreId,
                        principalTable: "RiskScores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiskExplanations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskScoreId = table.Column<int>(type: "int", nullable: false),
                    ExplanationTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExplanationText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContributionScore = table.Column<double>(type: "float", nullable: false),
                    IsPrimaryReason = table.Column<bool>(type: "bit", nullable: false),
                    SuggestedAction = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskExplanations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskExplanations_RiskScores_RiskScoreId",
                        column: x => x.RiskScoreId,
                        principalTable: "RiskScores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlertNotifications_PatientId",
                table: "AlertNotifications",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertNotifications_RiskScoreId",
                table: "AlertNotifications",
                column: "RiskScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedFeatures_LabCycleId",
                table: "CalculatedFeatures",
                column: "LabCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_CalculatedFeatures_PatientId",
                table: "CalculatedFeatures",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalReports_LabCycleId",
                table: "ClinicalReports",
                column: "LabCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalReports_PatientId",
                table: "ClinicalReports",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodLogs_DailyLogId",
                table: "FoodLogs",
                column: "DailyLogId");

            migrationBuilder.CreateIndex(
                name: "IX_NutritionSummaries_DailyLogId",
                table: "NutritionSummaries",
                column: "DailyLogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskExplanations_RiskScoreId",
                table: "RiskExplanations",
                column: "RiskScoreId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskScores_LabCycleId",
                table: "RiskScores",
                column: "LabCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskScores_PatientId",
                table: "RiskScores",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertNotifications");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CalculatedFeatures");

            migrationBuilder.DropTable(
                name: "CancerTypeConfigs");

            migrationBuilder.DropTable(
                name: "ClinicalReports");

            migrationBuilder.DropTable(
                name: "FoodLogs");

            migrationBuilder.DropTable(
                name: "ModelVersions");

            migrationBuilder.DropTable(
                name: "NutritionSummaries");

            migrationBuilder.DropTable(
                name: "RiskExplanations");

            migrationBuilder.DropTable(
                name: "RiskWeights");

            migrationBuilder.DropTable(
                name: "TreatmentTypeConfigs");

            migrationBuilder.DropTable(
                name: "RiskScores");
        }
    }
}
