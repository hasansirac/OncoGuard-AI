namespace OncoGuard.Application.DTOs.Labs;

public class CreateLabResultRequest
{
    public int PatientId { get; set; }

    public double? Wbc { get; set; }
    public double? Anc { get; set; }
    public double? Lymphocyte { get; set; }
    public double? Monocyte { get; set; }
    public double? Crp { get; set; }
    public double? Esr { get; set; }
    public double? Procalcitonin { get; set; }

    public double? Hemoglobin { get; set; }
    public double? Hematocrit { get; set; }
    public double? Platelet { get; set; }
    public double? Mcv { get; set; }
    public double? Rdw { get; set; }

    public double? Creatinine { get; set; }
    public double? Urea { get; set; }
    public double? Egfr { get; set; }
    public double? Sodium { get; set; }
    public double? Potassium { get; set; }
    public double? Magnesium { get; set; }
    public double? Calcium { get; set; }
    public double? Chloride { get; set; }

    public double? Ast { get; set; }
    public double? Alt { get; set; }
    public double? Alp { get; set; }
    public double? Ggt { get; set; }
    public double? TotalBilirubin { get; set; }
    public double? DirectBilirubin { get; set; }
    public double? Ldh { get; set; }

    public double? Albumin { get; set; }
    public double? TotalProtein { get; set; }
    public double? Prealbumin { get; set; }
    public double? Ferritin { get; set; }
    public double? Iron { get; set; }
    public double? TransferrinSaturation { get; set; }
    public double? VitaminB12 { get; set; }
    public double? Folate { get; set; }
    public double? VitaminD { get; set; }
    public double? Zinc { get; set; }
    public double? Selenium { get; set; }

    public double? Tsh { get; set; }
    public double? FreeT4 { get; set; }
    public double? Glucose { get; set; }
    public double? MorningCortisol { get; set; }
    public double? Amylase { get; set; }
    public double? Lipase { get; set; }

    public DateTime LabDate { get; set; }

    public string? Notes { get; set; }
}