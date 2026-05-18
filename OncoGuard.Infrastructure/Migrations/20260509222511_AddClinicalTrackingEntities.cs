using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OncoGuard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalTrackingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CancerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    CancerType = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasMetastasis = table.Column<bool>(type: "bit", nullable: false),
                    DiagnosisDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comorbidities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmokingStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlcoholStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EcogPerformanceScore = table.Column<int>(type: "int", nullable: false),
                    TreatmentGoal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancerProfiles_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    LogDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GeneralConditionScore = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    IsMissingLog = table.Column<bool>(type: "bit", nullable: false),
                    PatientNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyLogs_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    LabDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Wbc = table.Column<double>(type: "float", nullable: true),
                    Anc = table.Column<double>(type: "float", nullable: true),
                    Lymphocyte = table.Column<double>(type: "float", nullable: true),
                    Monocyte = table.Column<double>(type: "float", nullable: true),
                    Crp = table.Column<double>(type: "float", nullable: true),
                    Esr = table.Column<double>(type: "float", nullable: true),
                    Procalcitonin = table.Column<double>(type: "float", nullable: true),
                    Hemoglobin = table.Column<double>(type: "float", nullable: true),
                    Hematocrit = table.Column<double>(type: "float", nullable: true),
                    Platelet = table.Column<double>(type: "float", nullable: true),
                    Mcv = table.Column<double>(type: "float", nullable: true),
                    Rdw = table.Column<double>(type: "float", nullable: true),
                    Creatinine = table.Column<double>(type: "float", nullable: true),
                    Urea = table.Column<double>(type: "float", nullable: true),
                    Egfr = table.Column<double>(type: "float", nullable: true),
                    Sodium = table.Column<double>(type: "float", nullable: true),
                    Potassium = table.Column<double>(type: "float", nullable: true),
                    Magnesium = table.Column<double>(type: "float", nullable: true),
                    Calcium = table.Column<double>(type: "float", nullable: true),
                    Chloride = table.Column<double>(type: "float", nullable: true),
                    Ast = table.Column<double>(type: "float", nullable: true),
                    Alt = table.Column<double>(type: "float", nullable: true),
                    Alp = table.Column<double>(type: "float", nullable: true),
                    Ggt = table.Column<double>(type: "float", nullable: true),
                    TotalBilirubin = table.Column<double>(type: "float", nullable: true),
                    DirectBilirubin = table.Column<double>(type: "float", nullable: true),
                    Ldh = table.Column<double>(type: "float", nullable: true),
                    Albumin = table.Column<double>(type: "float", nullable: true),
                    TotalProtein = table.Column<double>(type: "float", nullable: true),
                    Prealbumin = table.Column<double>(type: "float", nullable: true),
                    Ferritin = table.Column<double>(type: "float", nullable: true),
                    Iron = table.Column<double>(type: "float", nullable: true),
                    TransferrinSaturation = table.Column<double>(type: "float", nullable: true),
                    VitaminB12 = table.Column<double>(type: "float", nullable: true),
                    Folate = table.Column<double>(type: "float", nullable: true),
                    VitaminD = table.Column<double>(type: "float", nullable: true),
                    Zinc = table.Column<double>(type: "float", nullable: true),
                    Selenium = table.Column<double>(type: "float", nullable: true),
                    Tsh = table.Column<double>(type: "float", nullable: true),
                    FreeT4 = table.Column<double>(type: "float", nullable: true),
                    Glucose = table.Column<double>(type: "float", nullable: true),
                    MorningCortisol = table.Column<double>(type: "float", nullable: true),
                    Amylase = table.Column<double>(type: "float", nullable: true),
                    Lipase = table.Column<double>(type: "float", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabResults_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    TreatmentType = table.Column<int>(type: "int", nullable: false),
                    ProtocolName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CycleNumber = table.Column<int>(type: "int", nullable: false),
                    LastTreatmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextPlannedTreatmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HasPreviousNeutropenia = table.Column<bool>(type: "bit", nullable: false),
                    UsesGcsfSupport = table.Column<bool>(type: "bit", nullable: false),
                    UsesAntibiotic = table.Column<bool>(type: "bit", nullable: false),
                    UsesSteroid = table.Column<bool>(type: "bit", nullable: false),
                    UsesAntiemetic = table.Column<bool>(type: "bit", nullable: false),
                    UsesPainkiller = table.Column<bool>(type: "bit", nullable: false),
                    HasDoseReduction = table.Column<bool>(type: "bit", nullable: false),
                    HadPreviousTreatmentDelay = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentPlans_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HydrationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyLogId = table.Column<int>(type: "int", nullable: false),
                    WaterIntakeMl = table.Column<double>(type: "float", nullable: false),
                    DryMouth = table.Column<int>(type: "int", nullable: false),
                    UrineColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UrinationCount = table.Column<int>(type: "int", nullable: false),
                    HasDizziness = table.Column<bool>(type: "bit", nullable: false),
                    HasVomitingOrDiarrhea = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HydrationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HydrationLogs_DailyLogs_DailyLogId",
                        column: x => x.DailyLogId,
                        principalTable: "DailyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MedicationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyLogId = table.Column<int>(type: "int", nullable: false),
                    TookMainMedication = table.Column<bool>(type: "bit", nullable: false),
                    MissedDoseCount = table.Column<int>(type: "int", nullable: false),
                    UsedAntibiotic = table.Column<bool>(type: "bit", nullable: false),
                    UsedSteroid = table.Column<bool>(type: "bit", nullable: false),
                    UsedAntiemetic = table.Column<bool>(type: "bit", nullable: false),
                    UsedPainkiller = table.Column<bool>(type: "bit", nullable: false),
                    HadSideEffect = table.Column<bool>(type: "bit", nullable: false),
                    SideEffectDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationLogs_DailyLogs_DailyLogId",
                        column: x => x.DailyLogId,
                        principalTable: "DailyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SymptomLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyLogId = table.Column<int>(type: "int", nullable: false),
                    BodyTemperature = table.Column<double>(type: "float", nullable: true),
                    Fatigue = table.Column<int>(type: "int", nullable: false),
                    Pain = table.Column<int>(type: "int", nullable: false),
                    Nausea = table.Column<int>(type: "int", nullable: false),
                    VomitingCount = table.Column<int>(type: "int", nullable: false),
                    DiarrheaCount = table.Column<int>(type: "int", nullable: false),
                    Constipation = table.Column<int>(type: "int", nullable: false),
                    Cough = table.Column<int>(type: "int", nullable: false),
                    Dyspnea = table.Column<int>(type: "int", nullable: false),
                    MouthSore = table.Column<int>(type: "int", nullable: false),
                    SwallowingDifficulty = table.Column<int>(type: "int", nullable: false),
                    SkinRash = table.Column<int>(type: "int", nullable: false),
                    HasBleedingOrBruising = table.Column<bool>(type: "bit", nullable: false),
                    HasDizziness = table.Column<bool>(type: "bit", nullable: false),
                    HasConfusion = table.Column<bool>(type: "bit", nullable: false),
                    OtherSymptoms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SymptomLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SymptomLogs_DailyLogs_DailyLogId",
                        column: x => x.DailyLogId,
                        principalTable: "DailyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VitalSignsLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DailyLogId = table.Column<int>(type: "int", nullable: false),
                    SystolicBloodPressure = table.Column<double>(type: "float", nullable: true),
                    DiastolicBloodPressure = table.Column<double>(type: "float", nullable: true),
                    HeartRate = table.Column<double>(type: "float", nullable: true),
                    OxygenSaturation = table.Column<double>(type: "float", nullable: true),
                    BodyTemperature = table.Column<double>(type: "float", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VitalSignsLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VitalSignsLogs_DailyLogs_DailyLogId",
                        column: x => x.DailyLogId,
                        principalTable: "DailyLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LabCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    BaselineLabId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PreviousCycleId = table.Column<int>(type: "int", nullable: true),
                    CycleSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LabCycles_LabCycles_PreviousCycleId",
                        column: x => x.PreviousCycleId,
                        principalTable: "LabCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabCycles_LabResults_BaselineLabId",
                        column: x => x.BaselineLabId,
                        principalTable: "LabResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LabCycles_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CancerProfiles_PatientId",
                table: "CancerProfiles",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyLogs_PatientId",
                table: "DailyLogs",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_HydrationLogs_DailyLogId",
                table: "HydrationLogs",
                column: "DailyLogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabCycles_BaselineLabId",
                table: "LabCycles",
                column: "BaselineLabId");

            migrationBuilder.CreateIndex(
                name: "IX_LabCycles_PatientId",
                table: "LabCycles",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LabCycles_PreviousCycleId",
                table: "LabCycles",
                column: "PreviousCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_LabResults_PatientId",
                table: "LabResults",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLogs_DailyLogId",
                table: "MedicationLogs",
                column: "DailyLogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SymptomLogs_DailyLogId",
                table: "SymptomLogs",
                column: "DailyLogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PatientId",
                table: "TreatmentPlans",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_VitalSignsLogs_DailyLogId",
                table: "VitalSignsLogs",
                column: "DailyLogId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CancerProfiles");

            migrationBuilder.DropTable(
                name: "HydrationLogs");

            migrationBuilder.DropTable(
                name: "LabCycles");

            migrationBuilder.DropTable(
                name: "MedicationLogs");

            migrationBuilder.DropTable(
                name: "SymptomLogs");

            migrationBuilder.DropTable(
                name: "TreatmentPlans");

            migrationBuilder.DropTable(
                name: "VitalSignsLogs");

            migrationBuilder.DropTable(
                name: "LabResults");

            migrationBuilder.DropTable(
                name: "DailyLogs");
        }
    }
}
