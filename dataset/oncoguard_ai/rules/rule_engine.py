"""
rule_engine.py
==============
Deterministik klinik guvenlik katmani. "AI yanilabilir; sert kurallar olmali"
(FAZ 9, Bolum 19). Bu motor SADECE yayinlanmis esiklere dayanir ve nihai
fuzyonda ML'i OVERRIDE eder (Rule > AI).

Tasarim:
- Her kural seffaf: tetiklenince INSAN-OKUNUR bir aciklama uretir.
- Cikti: her risk turu icin (RiskLevel, [tetiklenen kurallar]).
- Karar VERMEZ, tedavi ONERMEZ -> sadece "Clinical review recommended".

KAYNAKLAR: clinical_constants.py icindeki [1]-[4]. Akut kurallar
(konfuzyon, siddetli dispne, kanama+dusuk trombosit) klasik onkolojik
aciliyet kriterleridir (febril notropeni: IDSA 2011).
"""

from __future__ import annotations
from dataclasses import dataclass, field

from oncoguard_ai.core import clinical_constants as cc
from oncoguard_ai.core.clinical_constants import RiskLevel


RISK_TYPES = [
    "InfectionRisk", "FebrileNeutropeniaRisk", "MalnutritionRisk",
    "CachexiaRisk", "DehydrationRisk", "RenalToxicityRisk",
    "HepaticToxicityRisk", "ImmunotherapyAdverseEventRisk",
    "TreatmentDelayRisk", "OverallClinicalDeteriorationRisk",
]


@dataclass
class RuleResult:
    levels: dict[str, RiskLevel] = field(default_factory=dict)
    triggers: dict[str, list[str]] = field(default_factory=dict)
    critical_alerts: list[str] = field(default_factory=list)

    def bump(self, risk: str, level: RiskLevel, msg: str):
        """Bir riski en az verilen seviyeye yukselt + aciklama ekle."""
        cur = self.levels.get(risk, RiskLevel.GREEN)
        self.levels[risk] = RiskLevel(max(int(cur), int(level)))
        self.triggers.setdefault(risk, []).append(msg)
        if level == RiskLevel.RED:
            self.critical_alerts.append(f"{risk}: {msg}")


def evaluate_rules(features: dict, latest_log: dict | None = None,
                   patient: dict | None = None) -> RuleResult:
    """features = build_features ciktisi; latest_log = bugunku ham log
    (akut tek-gun sinyaller icin); patient = profil (tedavi turu vb.)."""
    latest_log = latest_log or {}
    patient = patient or {}
    r = RuleResult()
    for rt in RISK_TYPES:
        r.levels[rt] = RiskLevel.GREEN

    anc = features.get("BaselineANC")
    temp = latest_log.get("Temperature") or features.get("MaxTemp3")

    # === AKUT KRITIK KURALLAR (24 saat penceresi - Bolum 5.1) =============
    # 1) Febril notropeni: ates + ANC<1000  (Kaynak [2])
    if cc.is_febrile_neutropenia(temp, anc):
        r.bump("FebrileNeutropeniaRisk", RiskLevel.RED,
               f"Febrile neutropenia rule: fever (>={cc.FEVER_SINGLE_C} C) + "
               f"ANC {anc}/uL (<{cc.NEUTROPENIA_FN_ANC}). Clinical review recommended.")
        r.bump("InfectionRisk", RiskLevel.RED, "Concurrent FN -> infection RED.")

    # 2) Siddetli dispne (grade>=3) veya dusuk SpO2
    dyspnea = latest_log.get("Dyspnea", 0) or 0
    spo2 = features.get("MinSpO2_3")
    if dyspnea >= 3 or (spo2 is not None and spo2 < 92):
        r.bump("ImmunotherapyAdverseEventRisk", RiskLevel.RED,
               "Severe dyspnea / SpO2<92% -> possible pneumonitis. Critical alert.")
        r.bump("OverallClinicalDeteriorationRisk", RiskLevel.RED,
               "Respiratory compromise detected.")

    # 3) Konfuzyon (bilinc bulanikligi)
    if (latest_log.get("Confusion", 0) or 0) >= 2:
        r.bump("OverallClinicalDeteriorationRisk", RiskLevel.RED,
               "New confusion reported -> critical alert.")

    # 4) Kanama/morarma + dusuk trombosit
    plt_grade = features.get("PlateletGrade", 0)
    if (latest_log.get("BleedingBruising", 0) or 0) >= 2 and plt_grade >= 3:
        r.bump("OverallClinicalDeteriorationRisk", RiskLevel.RED,
               "Bleeding/bruising + severe thrombocytopenia (Grade>=3). Critical.")

    # === ENFEKSIYON (akut degil, trend/grade) =============================
    anc_grade = features.get("ANCGrade", 0)
    crp = features.get("BaselineCRP")
    inf_score = features.get("InfectionSymptomScore", 0) or 0
    if anc_grade >= 3 and r.levels["InfectionRisk"] < RiskLevel.RED:
        r.bump("InfectionRisk", RiskLevel.ORANGE,
               f"Severe neutropenia (ANC Grade {anc_grade}) -> high infection risk.")
    elif anc_grade == 2 or (crp and crp > 50) or inf_score >= 3:
        r.bump("InfectionRisk", RiskLevel.YELLOW,
               "Moderate neutropenia / elevated CRP / infection symptoms.")

    # === MALNUTRISYON (ESPEN + albumin) ===================================
    pr7 = features.get("ProteinRatioMean7")
    appetite_delta = features.get("AppetiteDelta", 0) or 0
    alb_grade = features.get("AlbuminGrade", 0)
    wl7 = features.get("WeightLossPct7") or 0
    if (pr7 is not None and pr7 < 0.6) and appetite_delta < -1 and alb_grade >= 1:
        r.bump("MalnutritionRisk", RiskLevel.ORANGE,
               "Protein intake <60% target + appetite decline + low albumin.")
    elif (pr7 is not None and pr7 < 0.8) or alb_grade >= 2:
        r.bump("MalnutritionRisk", RiskLevel.YELLOW,
               "Sustained low protein intake or hypoalbuminemia.")

    # === KASHEKSI (Fearon 2011) ===========================================
    bmi = features.get("BMI")
    wl_total = max(wl7, features.get("WeightLossPct30") or 0)
    if cc.cachexia_flag(wl_total, bmi):
        crp_alb = features.get("CRPAlbuminRatio") or 0
        lvl = RiskLevel.ORANGE if crp_alb and crp_alb > 10 else RiskLevel.YELLOW
        r.bump("CachexiaRisk", lvl,
               f"Fearon cachexia criterion met (weight loss {wl_total:.1f}%).")

    # === DEHIDRATASYON =====================================================
    wr3 = features.get("WaterRatioMean3")
    fluid_loss = features.get("FluidLossScore3", 0) or 0
    dizzy = features.get("DizzinessMean3", 0) or 0
    if (wr3 is not None and wr3 < 0.5) and fluid_loss >= 3:
        r.bump("DehydrationRisk", RiskLevel.ORANGE,
               "Water intake <50% target + high vomiting/diarrhea load.")
    elif (wr3 is not None and wr3 < 0.7 and (fluid_loss >= 1 or dizzy >= 1)):
        r.bump("DehydrationRisk", RiskLevel.YELLOW,
               "Low hydration with fluid loss/dizziness.")

    # === BOBREK TOKSISITESI (CTCAE kreatinin) =============================
    cr_grade = features.get("CreatinineGrade", 0)
    if cr_grade >= 3:
        r.bump("RenalToxicityRisk", RiskLevel.RED,
               f"Creatinine CTCAE Grade {cr_grade}. Clinical review recommended.")
    elif cr_grade == 2:
        r.bump("RenalToxicityRisk", RiskLevel.ORANGE, "Creatinine Grade 2.")
    elif cr_grade == 1:
        r.bump("RenalToxicityRisk", RiskLevel.YELLOW, "Creatinine Grade 1.")

    # === KARACIGER TOKSISITESI (CTCAE AST/ALT/bilirubin) ==================
    hep_grade = max(features.get("ASTGrade", 0), features.get("ALTGrade", 0),
                    features.get("BilirubinGrade", 0))
    r.bump("HepaticToxicityRisk", cc.grade_to_risk(hep_grade),
           f"Liver enzyme CTCAE Grade {hep_grade}.") if hep_grade else None

    # === IMMUNOTERAPI YAN ETKI (irAE) - immuno baglamindaki hastalarda ==
    if (patient.get("TreatmentType") or "") in ("Immunotherapy", "ChemoImmunotherapy"):
        diarrhea_trend = features.get("DiarrheaTrend7", 0) or 0
        rash_trend = features.get("SkinRashTrend7", 0) or 0
        dysp_trend = features.get("DyspneaTrend7", 0) or 0
        signals = sum(t > 0.2 for t in (diarrhea_trend, rash_trend, dysp_trend))
        if signals >= 2:
            r.bump("ImmunotherapyAdverseEventRisk", RiskLevel.ORANGE,
                   "Immunotherapy + >=2 rising irAE symptoms (diarrhea/rash/dyspnea).")
        elif signals == 1:
            r.bump("ImmunotherapyAdverseEventRisk", RiskLevel.YELLOW,
                   "Immunotherapy + 1 rising irAE symptom.")

    # === TEDAVI ERTELENME ==================================================
    delay_grade = max(anc_grade, features.get("PlateletGrade", 0),
                      features.get("HemoglobinGrade", 0))
    if delay_grade >= 3 or r.levels["InfectionRisk"] >= RiskLevel.ORANGE:
        r.bump("TreatmentDelayRisk", RiskLevel.ORANGE,
               "Severe cytopenia or high infection risk -> likely treatment delay.")
    elif delay_grade == 2 or features.get("PreviousTreatmentDelay"):
        r.bump("TreatmentDelayRisk", RiskLevel.YELLOW,
               "Moderate cytopenia or prior delay history.")

    # === OVERALL = aktif risklerin en yukseni (rule tarafinda) ============
    component_max = max(
        (r.levels[rt] for rt in RISK_TYPES if rt != "OverallClinicalDeteriorationRisk"),
        default=RiskLevel.GREEN)
    r.levels["OverallClinicalDeteriorationRisk"] = RiskLevel(
        max(int(r.levels["OverallClinicalDeteriorationRisk"]), int(component_max)))

    return r


if __name__ == "__main__":
    # Klasik test: ates + dusuk ANC -> FN RED, InfectionRisk RED, Overall RED
    feats = {"BaselineANC": 820, "ANCGrade": 3, "MaxTemp3": 38.6,
             "BaselineCRP": 60, "InfectionSymptomScore": 4, "PlateletGrade": 1,
             "CreatinineGrade": 0, "ASTGrade": 0, "ALTGrade": 0, "BilirubinGrade": 0,
             "HemoglobinGrade": 2, "BMI": 23}
    res = evaluate_rules(feats, latest_log={"Temperature": 38.6},
                         patient={"TreatmentType": "Chemotherapy"})
    print("Febril notropeni senaryosu:")
    for rt in RISK_TYPES:
        lvl = res.levels[rt]
        if lvl > 0:
            print(f"  {rt:35s} -> {lvl.label}")
    print("  Kritik alarmlar:", len(res.critical_alerts))
    assert res.levels["FebrileNeutropeniaRisk"] == RiskLevel.RED
    assert res.levels["OverallClinicalDeteriorationRisk"] == RiskLevel.RED
    print("rule_engine.py: guvenlik kurallari dogru tetiklendi.")
