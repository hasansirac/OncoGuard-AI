"""
clinical_constants.py
======================
ONCOGUARD-AI'in TEK klinik gercek kaynagi (single source of truth).

Buradaki butun esikler yayinlanmis kilavuzlardan alinmistir. Hicbir deger
"kafadan" konmamistir. Hem Rule Engine hem de sentetik veri label uretimi
AYNI sabitleri kullanir -> tutarlilik garanti.

KAYNAKLAR
---------
[1] CTCAE v5.0 (NCI, 2017) - Common Terminology Criteria for Adverse Events.
    Notropeni, AST/ALT, bilirubin, kreatinin, albumin grade esikleri.
[2] Freifeld AG et al. IDSA Clinical Practice Guideline for the Use of
    Antimicrobial Agents in Neutropenic Patients with Cancer (2011) &
    CTCAE v5 febril notropeni tanimi: ANC<1000 + atesin tek seferlik >38.3 C
    veya >=38.0 C 1 saatten uzun surmesi.
[3] Fearon K et al. "Definition and classification of cancer cachexia:
    an international consensus." Lancet Oncol 2011;12(5):489-95.
    Kasheksi: kilo kaybi >%5 (6 ay), veya >%2 + BMI<20, veya >%2 + sarkopeni.
[4] Arends J et al. ESPEN guidelines on nutrition in cancer patients.
    Clin Nutr 2017. Protein hedefi 1.0-1.5 g/kg/gun, enerji 25-30 kcal/kg/gun.

NOT: CTCAE v6 (2026) notropeni gradelerini bir kademe kaydirmistir. Bu proje
yaygin standart olan v5'i temel alir; v6 farki dokumantasyonda belirtilir.
"""

from __future__ import annotations
from enum import IntEnum


# ---------------------------------------------------------------------------
# Risk seviyeleri (4 kademe) - proje dokumaniyla birebir
# ---------------------------------------------------------------------------
class RiskLevel(IntEnum):
    GREEN = 0    # Safe
    YELLOW = 1   # Caution
    ORANGE = 2   # High attention
    RED = 3      # Critical

    @property
    def label(self) -> str:
        return {0: "Green", 1: "Yellow", 2: "Orange", 3: "Red"}[int(self)]


# ---------------------------------------------------------------------------
# Referans araliklari (LLN = alt sinir, ULN = ust sinir) - yetiskin
# Bu degerler grade hesabinin paydasidir. Lab cihazina gore degisebilir;
# bu yuzden tek yerde tutuyoruz ki ileride hastane referansiyla degistirilebilsin.
# ---------------------------------------------------------------------------
REFERENCE = {
    "ANC":        {"lln": 1500, "uln": 8000, "unit": "/uL"},
    "WBC":        {"lln": 4000, "uln": 11000, "unit": "/uL"},
    "Hemoglobin": {"lln": 12.0, "uln": 17.0, "unit": "g/dL"},
    "Platelet":   {"lln": 150,  "uln": 400,  "unit": "x10^3/uL"},
    "CRP":        {"lln": 0.0,  "uln": 5.0,  "unit": "mg/L"},
    "Albumin":    {"lln": 3.5,  "uln": 5.0,  "unit": "g/dL"},
    "Creatinine": {"lln": 0.6,  "uln": 1.2,  "unit": "mg/dL"},
    "AST":        {"lln": 0,    "uln": 40,   "unit": "U/L"},
    "ALT":        {"lln": 0,    "uln": 41,   "unit": "U/L"},
    "Bilirubin":  {"lln": 0.1,  "uln": 1.2,  "unit": "mg/dL"},
    "TSH":        {"lln": 0.4,  "uln": 4.0,  "unit": "mIU/L"},
    "FreeT4":     {"lln": 0.8,  "uln": 1.8,  "unit": "ng/dL"},
}

# ---------------------------------------------------------------------------
# BIRIM SOZLESMESI (UNITS CONTRACT) -- BACKEND + AI ARASINDA ZORUNLU
# ---------------------------------------------------------------------------
# ANC ve WBC MUTLAK hucre/uL cinsindendir (orn. ANC=1800, WBC=6500).
#   -> CTCAE notropeni esikleri (1500/1000/500) bu birimdedir.
#   -> Backend WBC'yi 10^3/uL (orn. 6.5) tutuyorsa MUTLAKA 1000 ile carpip
#      gondermeli; aksi halde grade hesabi tamamen yanlis olur.
# Trombosit (Platelet) 10^3/uL cinsindendir (orn. 230) -- bu da standarttir.
# Bu farki tek yerde sabitliyoruz; entegrasyonda referans budur.
UNITS_CONTRACT = {
    "ANC": "cells/uL (absolute, e.g. 1800)",
    "WBC": "cells/uL (absolute, e.g. 6500)",
    "Platelet": "x10^3/uL (e.g. 230)",
    "Hemoglobin": "g/dL", "Albumin": "g/dL", "Creatinine": "mg/dL",
    "AST": "U/L", "ALT": "U/L", "Bilirubin": "mg/dL",
    "CRP": "mg/L", "TSH": "mIU/L", "FreeT4": "ng/dL",
}


def wbc_anc_to_absolute(value, assume_unit="auto"):
    """Backend WBC/ANC'yi 10^3/uL gonderiyorsa mutlak /uL'ye cevirir.
    assume_unit='auto': <100 ise 10^3/uL kabul edip 1000 ile carpar
    (klinik olarak WBC asla <100/uL olmaz, ANC nadiren)."""
    if value is None:
        return None
    if assume_unit == "absolute":
        return value
    if assume_unit == "thousand":
        return value * 1000
    return value * 1000 if value < 100 else value  # auto


# ---------------------------------------------------------------------------
# LAB TABAN DEGERLERI (clamp) -- negatif/imkansiz deger uretimini engeller.
# normal() kuyrugu sifirin altina inebilir; her lab klinik tabaninda kesilir.
# Degerler "hayatla bagdasabilir mutlak alt sinir" mantigiyla secildi.
# ---------------------------------------------------------------------------
LAB_FLOORS = {
    "BaselineANC": 0.0, "BaselineWBC": 100.0, "BaselineCRP": 0.0,
    "BaselinePlatelet": 2.0, "BaselineAST": 5.0, "BaselineALT": 5.0,
    "BaselineBilirubin": 0.1, "BaselineTSH": 0.01, "BaselineFreeT4": 0.1,
    "BaselineAlbumin": 1.0, "BaselineCreatinine": 0.2, "BaselineHemoglobin": 3.0,
}


def clamp_labs(lab: dict) -> dict:
    """Tum lab degerlerini klinik tabanlarina kelepceler (negatif/imkansiz onlenir)."""
    for k, floor in LAB_FLOORS.items():
        if k in lab and lab[k] is not None:
            lab[k] = max(floor, float(lab[k]))
    return lab


# ---------------------------------------------------------------------------
# [1] CTCAE v5 - NOTROPENI (ANC, /uL).  Grade hesaplayan saf fonksiyon.
# ---------------------------------------------------------------------------
def ctcae_anc_grade(anc: float) -> int:
    """CTCAE v5 notrofil grade. Kaynak [1]."""
    if anc is None:
        return 0
    if anc < 500:
        return 4
    if anc < 1000:
        return 3
    if anc < 1500:
        return 2
    if anc < REFERENCE["ANC"]["lln"]:   # LLN-1500 arasi pratikte grade 1 ust sinir
        return 1
    return 0


def ctcae_platelet_grade(plt: float) -> int:
    """CTCAE v5 trombositopeni grade (x10^3/uL). Kaynak [1]."""
    if plt is None:
        return 0
    if plt < 25:
        return 4
    if plt < 50:
        return 3
    if plt < 75:
        return 2
    if plt < REFERENCE["Platelet"]["lln"]:
        return 1
    return 0


def ctcae_hemoglobin_grade(hgb: float) -> int:
    """CTCAE v5 anemi grade (g/dL). Kaynak [1]."""
    if hgb is None:
        return 0
    if hgb < 8.0:
        return 3
    if hgb < 10.0:
        return 2
    if hgb < REFERENCE["Hemoglobin"]["lln"]:
        return 1
    return 0


# ---------------------------------------------------------------------------
# [1] CTCAE v5 - ULN katina dayali enzimler (baseline normal varsayimi).
# AST/ALT ayni esik tablosunu paylasir.
# ---------------------------------------------------------------------------
def ctcae_fold_uln_grade(value: float, analyte: str,
                         g1: float, g2: float, g3: float) -> int:
    """ULN katina gore grade. g1/g2/g3 = grade 2/3/4 esiklerinin baslangici.
    Ornek AST/ALT: g1=3, g2=5, g3=20 (>ULN-3x = G1, 3-5x = G2, 5-20x = G3, >20x = G4).
    """
    if value is None:
        return 0
    uln = REFERENCE[analyte]["uln"]
    fold = value / uln if uln else 0
    if fold > g3:
        return 4
    if fold > g2:
        return 3
    if fold > g1:
        return 2
    if fold > 1.0:
        return 1
    return 0


def ctcae_ast_grade(ast: float) -> int:
    """AST: G1 >ULN-3x, G2 3-5x, G3 5-20x, G4 >20x ULN. Kaynak [1]."""
    return ctcae_fold_uln_grade(ast, "AST", g1=3.0, g2=5.0, g3=20.0)


def ctcae_alt_grade(alt: float) -> int:
    """ALT: AST ile ayni esikler. Kaynak [1]."""
    return ctcae_fold_uln_grade(alt, "ALT", g1=3.0, g2=5.0, g3=20.0)


def ctcae_bilirubin_grade(bili: float) -> int:
    """Bilirubin: G1 >ULN-1.5x, G2 1.5-3x, G3 3-10x, G4 >10x ULN. Kaynak [1]."""
    return ctcae_fold_uln_grade(bili, "Bilirubin", g1=1.5, g2=3.0, g3=10.0)


def ctcae_creatinine_grade(cr: float) -> int:
    """Kreatinin: G1 >ULN-1.5x, G2 1.5-3x, G3 3-6x, G4 >6x ULN. Kaynak [1]."""
    return ctcae_fold_uln_grade(cr, "Creatinine", g1=1.5, g2=3.0, g3=6.0)


def ctcae_albumin_grade(alb: float) -> int:
    """Hipoalbuminemi: G1 <LLN-3, G2 <3-2, G3 <2 g/dL. Kaynak [1]."""
    if alb is None:
        return 0
    if alb < 2.0:
        return 3
    if alb < 3.0:
        return 2
    if alb < REFERENCE["Albumin"]["lln"]:
        return 1
    return 0


# ---------------------------------------------------------------------------
# [2] Febril notropeni tanimi
# ---------------------------------------------------------------------------
FEVER_SINGLE_C = 38.3      # tek seferlik atesin esigi (C)
FEVER_SUSTAINED_C = 38.0   # 1 saatten uzun suren ates esigi (C)
NEUTROPENIA_FN_ANC = 1000  # FN icin ANC esigi (/uL)


def is_febrile_neutropenia(temp_c: float, anc: float) -> bool:
    """CTCAE v5 / IDSA febril notropeni. Kaynak [2].
    Basitlestirme: tek olcum esigi kullaniliyor (sureklilik gunluk veride yok)."""
    if temp_c is None or anc is None:
        return False
    febrile = temp_c >= FEVER_SINGLE_C
    neutropenic = anc < NEUTROPENIA_FN_ANC
    return febrile and neutropenic


# ---------------------------------------------------------------------------
# [3] Fearon 2011 - Kasheksi
# ---------------------------------------------------------------------------
CACHEXIA_WL_PRIMARY = 5.0       # %5 / 6 ay
CACHEXIA_WL_SECONDARY = 2.0     # %2 + (BMI<20 veya sarkopeni)
CACHEXIA_BMI_THRESHOLD = 20.0


def cachexia_flag(weight_loss_pct: float, bmi: float,
                  sarcopenia: bool = False) -> bool:
    """Fearon 2011 kasheksi tani kriteri. Kaynak [3].
    weight_loss_pct: son ~6 aydaki yuzde kilo kaybi (pozitif sayi)."""
    if weight_loss_pct is None:
        return False
    if weight_loss_pct > CACHEXIA_WL_PRIMARY:
        return True
    if weight_loss_pct > CACHEXIA_WL_SECONDARY and (
        (bmi is not None and bmi < CACHEXIA_BMI_THRESHOLD) or sarcopenia
    ):
        return True
    return False


# ---------------------------------------------------------------------------
# [4] ESPEN - Beslenme hedefleri (kisi bazli, kg uzerinden)
# ---------------------------------------------------------------------------
PROTEIN_G_PER_KG = 1.3          # ESPEN 1.0-1.5 araligi -> orta nokta
CALORIE_KCAL_PER_KG = 27.5      # ESPEN 25-30 araligi -> orta nokta
WATER_ML_PER_KG = 30.0          # genel hidrasyon hedefi


def protein_target_g(weight_kg: float) -> float:
    return weight_kg * PROTEIN_G_PER_KG


def calorie_target_kcal(weight_kg: float) -> float:
    return weight_kg * CALORIE_KCAL_PER_KG


def water_target_ml(weight_kg: float) -> float:
    return weight_kg * WATER_ML_PER_KG


# ---------------------------------------------------------------------------
# Genel risk birlestirme yardimcilari
# ---------------------------------------------------------------------------
def grade_to_risk(grade: int) -> RiskLevel:
    """CTCAE grade (0-4) -> 4 kademeli RiskLevel haritasi.
    Grade 0->Green, 1->Yellow, 2->Orange, 3+ ->Red."""
    if grade >= 3:
        return RiskLevel.RED
    return RiskLevel(grade)  # 0,1,2 dogrudan eslesir


# ---------------------------------------------------------------------------
# KOMORBIDITE MODIFIER'LARI  (generator v2)
# Mantik: ayni lab/semptom her hastada ayni anlami tasimaz (proje dokumani).
# Her komorbidite hangi riskin LATENT siddetini ne kadar artirir + bazi
# baseline lab degerlerini kaydirir. Prevalanslar onkoloji popolasyonu
# icin makul tahminlerdir (egitim amacli sentetik veri).
# ---------------------------------------------------------------------------
COMORBIDITIES = [
    "HasDiabetes", "HasHypertension", "HasChronicKidneyDisease",
    "HasHeartFailure", "HasCOPD", "HasLiverDisease",
]

COMORBIDITY_PREVALENCE = {
    "HasDiabetes": 0.22,
    "HasHypertension": 0.34,
    "HasChronicKidneyDisease": 0.10,
    "HasHeartFailure": 0.07,
    "HasCOPD": 0.12,
    "HasLiverDisease": 0.08,
}

# Komorbidite -> {risk_label: latent siddet artisi}
COMORBIDITY_RISK_MODIFIERS = {
    "HasDiabetes":            {"InfectionRisk": 0.5, "RenalToxicityRisk": 0.2},
    "HasHypertension":        {"RenalToxicityRisk": 0.2},
    "HasChronicKidneyDisease":{"RenalToxicityRisk": 1.0, "DehydrationRisk": 0.3},
    "HasHeartFailure":        {"DehydrationRisk": 0.6,
                              "OverallClinicalDeteriorationRisk": 0.3},
    "HasCOPD":                {"ImmunotherapyAdverseEventRisk": 0.5,
                              "InfectionRisk": 0.3},
    "HasLiverDisease":        {"HepaticToxicityRisk": 1.0},
}

# Komorbidite -> baseline lab carpani (gercekci kayma)
COMORBIDITY_LAB_SHIFTS = {
    "HasChronicKidneyDisease": {"BaselineCreatinine": 1.6},
    "HasLiverDisease":         {"BaselineAST": 1.8, "BaselineALT": 1.8,
                               "BaselineBilirubin": 1.4},
    "HasCOPD":                 {},   # SpO2 gunluk logda dusurulur
}


def prognostic_burden(age, ecog, albumin) -> float:
    """Age x ECOG x Albumin prognostik yuk skoru (0 .. ~2.6).
    Onkolojide en guclu prognostik uclu: ileri yas, kotu performans
    durumu (ECOG) ve hipoalbuminemi tedavi toleransini ve sagkalimi
    olumsuz etkiler. Latent Overall/TreatmentDelay/Malnutrition'a eklenir."""
    s = 0.0
    if age is not None:
        if age > 70:
            s += 0.5
        if age > 78:
            s += 0.3
    if ecog is not None:
        if ecog >= 2:
            s += 0.6
        if ecog >= 3:
            s += 0.4
    if albumin is not None:
        if albumin < 3.5:
            s += 0.4
        if albumin < 3.0:
            s += 0.4
    return s


if __name__ == "__main__":
    # Hizli akil testi: literatur ornekleri dogru grade veriyor mu?
    assert ctcae_anc_grade(820) == 3, "ANC 820 -> Grade 3 (CTCAE v5)"
    assert ctcae_anc_grade(450) == 4, "ANC 450 -> Grade 4"
    assert ctcae_anc_grade(1800) == 0, "ANC normal -> Grade 0"
    assert is_febrile_neutropenia(38.5, 820) is True, "Ates+dusuk ANC = FN"
    assert is_febrile_neutropenia(38.5, 1800) is False, "ANC normal -> FN yok"
    assert cachexia_flag(6.0, 22, False) is True, "%6 kayip -> kasheksi"
    assert cachexia_flag(3.0, 19, False) is True, "%3 + BMI<20 -> kasheksi"
    assert cachexia_flag(3.0, 25, False) is False, "%3 + BMI normal -> degil"
    assert ctcae_ast_grade(45) == 1 and ctcae_ast_grade(250) == 3
    print("clinical_constants.py: tum klinik akil testleri GECTI.")
