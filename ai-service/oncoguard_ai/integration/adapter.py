"""
adapter.py  —  ONCOGUARD-AI Entegrasyon Uyum Katmanı (Backend <-> AI)
=====================================================================

Bu dosya .NET backend ile AI servisi arasındaki veri uyumsuzluklarını çözer.

Görevleri:
1. Backend alan adlarını AI feature_engineering formatına çevirir.
2. ANC/WBC birim dönüşümü yapar.
3. CancerType ve TreatmentType enum/string değerlerini AI kategorilerine map eder.
4. DailyLog kayıtlarını normalize eder.
5. Aynı takvim gününe ait tekrar kayıtları tek güne düşürür.
6. WeightKg / HeightCm / ECOG gibi farklı isimlerle gelen alanları kabul eder.
"""

from __future__ import annotations

from datetime import datetime

from oncoguard_ai.core import clinical_constants as cc
from oncoguard_ai.features.feature_engineering import build_features, FEATURE_ORDER


# ---------------------------------------------------------------------------
# 1) RISK LEVEL  (AI 0-3  <->  backend 1-4)
# ---------------------------------------------------------------------------
AI_TO_BACKEND_RISKLEVEL = {
    0: 1,  # Green
    1: 2,  # Yellow
    2: 3,  # Orange
    3: 4,  # Red
}

BACKEND_TO_AI_RISKLEVEL = {
    1: 0,
    2: 1,
    3: 2,
    4: 3,
}


# ---------------------------------------------------------------------------
# 2) RISK TYPE  (AI label adı -> backend RiskType enum int)
# ---------------------------------------------------------------------------
AI_LABEL_TO_BACKEND_RISKTYPE = {
    "InfectionRisk": 1,
    "FebrileNeutropeniaRisk": 2,
    "MalnutritionRisk": 3,
    "CachexiaRisk": 4,
    "DehydrationRisk": 5,
    "RenalToxicityRisk": 6,
    "HepaticToxicityRisk": 7,
    "ImmunotherapyAdverseEventRisk": 8,
    "TreatmentDelayRisk": 9,
    "OverallClinicalDeteriorationRisk": 10,
}


# ---------------------------------------------------------------------------
# 3) CancerType / TreatmentType mapping
# ---------------------------------------------------------------------------
BACKEND_TO_AI_CANCER = {
    "Lung": "Lung",
    "Breast": "Breast",
    "Colon": "Colorectal",
    "Colorectal": "Colorectal",
    "Prostate": None,
    "Other": None,

    1: "Lung",
    2: "Breast",
    3: "Colorectal",
    4: None,
    5: None,

    "1": "Lung",
    "2": "Breast",
    "3": "Colorectal",
    "4": None,
    "5": None,
}

BACKEND_TO_AI_TREATMENT = {
    "Chemotherapy": "Chemotherapy",
    "Radiotherapy": "Radiation",
    "Radiation": "Radiation",
    "Immunotherapy": "Immunotherapy",
    "TargetedTherapy": "Targeted",
    "Targeted": "Targeted",
    "HormoneTherapy": "Hormone",
    "Hormone": "Hormone",

    1: "Chemotherapy",
    2: "Radiation",
    3: "Immunotherapy",
    4: "Targeted",
    5: "Hormone",

    "1": "Chemotherapy",
    "2": "Radiation",
    "3": "Immunotherapy",
    "4": "Targeted",
    "5": "Hormone",
}


# ---------------------------------------------------------------------------
# 4) Genel yardımcılar
# ---------------------------------------------------------------------------
def _first_present(d: dict, *keys, default=None):
    """
    Verilen key listesinden ilk bulunan ve None olmayan değeri döndürür.

    Önemli:
    payload.get("ECOG") or payload.get("EcogPerformanceScore") kullanırsak
    ECOG=0 gibi geçerli değerler kaybolabilir. Bu fonksiyon bunu engeller.
    """
    for key in keys:
        if key in d and d[key] is not None:
            return d[key]
    return default


def _bool_to_grade(value, true_value=2):
    """
    Backend bool değerlerini AI tarafındaki ordinal semptom formatına çevirir.
    False -> 0
    True  -> 2
    """
    return true_value if value else 0


def _parse_log_datetime(value):
    """
    LogDate değerini datetime'a çevirir.

    Kabul edilen örnekler:
    - 2026-06-17
    - 2026-06-17T10:00:00
    - 2026-06-17T10:00:00Z
    """
    if value is None:
        return None

    if isinstance(value, datetime):
        return value

    text = str(value).strip()
    if not text:
        return None

    try:
        return datetime.fromisoformat(text.replace("Z", "+00:00"))
    except Exception:
        try:
            return datetime.strptime(text[:10], "%Y-%m-%d")
        except Exception:
            return None


def _log_date_key(value):
    """
    Aynı takvim gününü aynı key'e indirir.
    Örnek:
    2026-06-17T10:00:00 -> 2026-06-17
    2026-06-17T21:00:00 -> 2026-06-17
    """
    dt = _parse_log_datetime(value)
    if dt is None:
        return None
    return dt.date().isoformat()


def _sort_and_dedupe_daily_logs(logs: list[dict]) -> list[dict]:
    """
    Aynı takvim gününe ait tekrar DailyLog gelirse tek kayıt bırakır.
    Duplicate varsa son gelen kayıt kazanır.

    Böylece:
    Aynı gün 3 kayıt = 1 monitoring günü
    Farklı gün 3 kayıt = 3 monitoring günü
    """
    dated_logs = {}
    undated_logs = []

    for log in logs:
        key = log.get("LogDate")
        if key:
            dated_logs[key] = log
        else:
            undated_logs.append(log)

    sorted_logs = [dated_logs[k] for k in sorted(dated_logs.keys())]

    return sorted_logs + undated_logs


# ---------------------------------------------------------------------------
# 5) Komorbidite parse
# ---------------------------------------------------------------------------
_COMORBIDITY_KEYWORDS = {
    "HasDiabetes": [
        "diyabet",
        "diabetes",
        "dm",
        "şeker",
        "seker",
    ],
    "HasHypertension": [
        "hipertansiyon",
        "hypertension",
        "htn",
        "tansiyon",
    ],
    "HasChronicKidneyDisease": [
        "böbrek",
        "bobrek",
        "kidney",
        "ckd",
        "renal",
        "kbh",
    ],
    "HasHeartFailure": [
        "kalp yetmez",
        "heart failure",
        "chf",
        "kalp",
    ],
    "HasCOPD": [
        "koah",
        "copd",
        "astım",
        "astim",
        "asthma",
    ],
    "HasLiverDisease": [
        "karaciğer",
        "karaciger",
        "liver",
        "hepatik",
        "siroz",
        "cirrhosis",
    ],
}


def parse_comorbidities(payload: dict) -> dict:
    """
    Komorbiditeleri iki şekilde okur:

    1. Backend ayrı boolean gönderirse:
       HasDiabetes = true

    2. Backend serbest metin gönderirse:
       Comorbidities = "diabetes, hypertension"
    """
    out = {}
    text = (payload.get("Comorbidities") or "").lower()

    for key, keywords in _COMORBIDITY_KEYWORDS.items():
        if key in payload:
            out[key] = bool(payload[key])
        else:
            out[key] = any(keyword in text for keyword in keywords)

    return out


# ---------------------------------------------------------------------------
# 6) Baseline lab normalize
# ---------------------------------------------------------------------------
def normalize_baseline_lab(lab: dict, wbc_anc_unit: str = "thousand") -> dict:
    """
    Backend LabResult -> AI baseline_lab formatı.

    wbc_anc_unit:
    - thousand: backend ANC/WBC değerleri x10^3/uL gelir, AI için x1000 yapılır.
    - absolute: değer zaten mutlak /uL gelir.
    """

    def anc_wbc(value):
        if value is None:
            return None
        return cc.wbc_anc_to_absolute(value, assume_unit=wbc_anc_unit)

    return {
        "BaselineANC": anc_wbc(
            _first_present(lab, "Anc", "ANC", "anc")
        ),
        "BaselineWBC": anc_wbc(
            _first_present(lab, "Wbc", "WBC", "wbc")
        ),
        "BaselineCRP": _first_present(
            lab, "Crp", "CRP", "crp"
        ),
        "BaselineAlbumin": _first_present(
            lab, "Albumin", "albumin"
        ),
        "BaselineCreatinine": _first_present(
            lab, "Creatinine", "creatinine"
        ),
        "BaselineAST": _first_present(
            lab, "Ast", "AST", "ast"
        ),
        "BaselineALT": _first_present(
            lab, "Alt", "ALT", "alt"
        ),
        "BaselinePlatelet": _first_present(
            lab, "Platelet", "platelet"
        ),
        "BaselineHemoglobin": _first_present(
            lab, "Hemoglobin", "hemoglobin"
        ),
        "BaselineBilirubin": _first_present(
            lab, "TotalBilirubin", "Bilirubin", "bilirubin"
        ),
        "BaselineTSH": _first_present(
            lab, "Tsh", "TSH", "tsh"
        ),
        "BaselineFreeT4": _first_present(
            lab, "FreeT4", "freeT4", "FreeT4Value"
        ),
    }


# ---------------------------------------------------------------------------
# 7) DailyLog normalize
# ---------------------------------------------------------------------------
def normalize_daily_log(d: dict) -> dict:
    """
    Backend günlük veri kaydını AI feature_engineering formatına çevirir.

    Burada gelen değerler ham tutulur.
    Protein/kalori/su oranları feature_engineering.py içinde hesaplanır.
    """
    return {
        "LogDate": _log_date_key(
            _first_present(d, "LogDate", "logDate", "Date", "date")
        ),

        "Temperature": _first_present(
            d, "BodyTemperature", "bodyTemperature", "Temperature"
        ),
        "Fatigue": _first_present(
            d, "Fatigue", "fatigue", default=0
        ),
        "Pain": _first_present(
            d, "Pain", "pain", default=0
        ),
        "Nausea": _first_present(
            d, "Nausea", "nausea", default=0
        ),
        "VomitingCount": _first_present(
            d, "VomitingCount", "vomitingCount", default=0
        ),
        "DiarrheaCount": _first_present(
            d, "DiarrheaCount", "diarrheaCount", default=0
        ),
        "Cough": _first_present(
            d, "Cough", "cough", default=0
        ),
        "Dyspnea": _first_present(
            d, "Dyspnea", "dyspnea", default=0
        ),
        "SkinRash": _first_present(
            d, "SkinRash", "skinRash", default=0
        ),

        "Dizziness": _bool_to_grade(
            _first_present(d, "HasDizziness", "hasDizziness", default=False)
        ),
        "Confusion": _bool_to_grade(
            _first_present(d, "HasConfusion", "hasConfusion", default=False)
        ),
        "BleedingBruising": _bool_to_grade(
            _first_present(
                d,
                "HasBleedingOrBruising",
                "hasBleedingOrBruising",
                default=False,
            )
        ),

        "ProteinIntake": _first_present(
            d, "TotalProtein", "Protein", "protein"
        ),
        "CalorieIntake": _first_present(
            d, "TotalCalories", "Calories", "calories"
        ),
        "WaterIntakeMl": _first_present(
            d, "WaterIntakeMl", "waterIntakeMl", "TotalWaterMl"
        ),

        "AppetiteScore": _first_present(
            d, "AppetiteScore", "appetiteScore"
        ),
        "MealCompletionRatio": _first_present(
            d, "MealCompletionRatio", "mealCompletionRatio"
        ),
        "WeightKg": _first_present(
            d, "WeightKg", "weightKg", "Weight", "weight"
        ),

        "MedicationTaken": 1
        if _first_present(
            d, "TookMainMedication", "tookMainMedication", default=False
        )
        else 0,

        "MissedDoseCount": _first_present(
            d, "MissedDoseCount", "missedDoseCount", default=0
        ),

        "OxygenSaturation": _first_present(
            d, "OxygenSaturation", "oxygenSaturation"
        ),
        "ActivityLevel": _first_present(
            d, "ActivityLevel", "activityLevel"
        ),
        "SleepHours": _first_present(
            d, "SleepHours", "sleepHours"
        ),
    }


# ---------------------------------------------------------------------------
# 8) Patient normalize
# ---------------------------------------------------------------------------
def normalize_patient(payload: dict) -> dict:
    """
    Backend Patient + CancerProfile + TreatmentPlan alanlarını AI patient formatına çevirir.
    """
    comorbidities = parse_comorbidities(payload)

    cancer_raw = _first_present(payload, "CancerType", "cancerType")
    treatment_raw = _first_present(payload, "TreatmentType", "treatmentType")

    patient = {
        "Age": _first_present(payload, "Age", "age"),
        "Gender": _first_present(payload, "Gender", "gender"),

        "WeightKg": _first_present(
            payload, "WeightKg", "Weight", "weightKg", "weight"
        ),
        "HeightCm": _first_present(
            payload, "HeightCm", "Height", "heightCm", "height"
        ),

        "ECOG": _first_present(
            payload, "ECOG", "EcogPerformanceScore", "ecogPerformanceScore"
        ),

        "CycleDay": _first_present(
            payload, "CycleDay", "cycleDay"
        ),
        "CycleNumber": _first_present(
            payload, "CycleNumber", "cycleNumber"
        ),

        "CancerType": BACKEND_TO_AI_CANCER.get(cancer_raw, cancer_raw),
        "TreatmentType": BACKEND_TO_AI_TREATMENT.get(treatment_raw, treatment_raw),

        "PreviousNeutropenia": _first_present(
            payload,
            "HasPreviousNeutropenia",
            "PreviousNeutropenia",
            default=False,
        ),
        "PreviousTreatmentDelay": _first_present(
            payload,
            "HadPreviousTreatmentDelay",
            "PreviousTreatmentDelay",
            default=False,
        ),
        "PreviousSevereToxicity": _first_present(
            payload,
            "HasPreviousSevereToxicity",
            "PreviousSevereToxicity",
            default=False,
        ),
        "DoseReductionFlag": _first_present(
            payload,
            "HasDoseReduction",
            "DoseReductionFlag",
            default=False,
        ),
        "GCSFUseFlag": _first_present(
            payload,
            "UsesGcsfSupport",
            "GCSFUseFlag",
            default=False,
        ),
    }

    patient.update(comorbidities)
    return patient


# ---------------------------------------------------------------------------
# 9) Backend payload -> AI feature vector
# ---------------------------------------------------------------------------
def backend_payload_to_features(payload: dict, wbc_anc_unit: str = "thousand"):
    """
    Backend raw payload'u AI feature sözlüğüne çevirir.

    Beklenen yapı:
    {
        ...patient/profile/treatment alanları,
        "BaselineLab": {...},
        "DailyLogs": [...]
    }

    Dönüş:
    features, patient, baseline_lab, daily_logs
    """
    patient = normalize_patient(payload)

    baseline_lab = normalize_baseline_lab(
        payload.get("BaselineLab", {}),
        wbc_anc_unit,
    )

    daily_logs = [
        normalize_daily_log(d)
        for d in payload.get("DailyLogs", [])
    ]

    daily_logs = _sort_and_dedupe_daily_logs(daily_logs)

    features = build_features(patient, baseline_lab, daily_logs)

    return features, patient, baseline_lab, daily_logs


def features_to_model_row(features: dict, cancer_ai: str, treatment_ai: str):
    """
    build_features çıktısını modelin beklediği 87 kolonlu düz satıra çevirir.
    One-hot CancerType_* ve TreatmentType_* burada eklenir.
    """
    from oncoguard_ai.data.synthetic_data_v2 import CANCER_TYPES
    from oncoguard_ai.models.preprocessing import TREATMENT_TYPES

    row = {key: features.get(key) for key in FEATURE_ORDER}

    for cancer_type in CANCER_TYPES:
        row[f"CancerType_{cancer_type}"] = 1 if cancer_ai == cancer_type else 0

    for treatment_type in TREATMENT_TYPES:
        row[f"TreatmentType_{treatment_type}"] = 1 if treatment_ai == treatment_type else 0

    return row


# ---------------------------------------------------------------------------
# 10) AI risk output -> backend RiskScore rows
# ---------------------------------------------------------------------------
def ai_risks_to_backend(
    ai_output: dict,
    patient_id: int,
    lab_cycle_id: int | None = None,
) -> list[dict]:
    """
    AI risk çıktılarını backend RiskScore formatına yakın sözlüklere çevirir.
    """
    rows = []

    for ai_label, info in ai_output.items():
        level_ai = int(info.get("level", 0))

        rows.append({
            "PatientId": patient_id,
            "LabCycleId": lab_cycle_id,
            "RiskType": AI_LABEL_TO_BACKEND_RISKTYPE.get(ai_label),
            "RiskLevel": AI_TO_BACKEND_RISKLEVEL[level_ai],
            "Score": info.get("score", level_ai / 3.0),
            "Confidence": info.get("confidence", 0.0),
            "IsCritical": level_ai == 3,
            "RequiresDoctorReview": level_ai >= 2,
            "Summary": info.get("summary"),
            "TriggeredByModel": True,
        })

    return rows


def fuse_rule_over_model(model_level_ai: int, rule_level_ai: int) -> int:
    """
    Rule engine ve ML model sonucu birleşimi.

    AI seviye ölçeği:
    0 Green
    1 Yellow
    2 Orange
    3 Red

    Final karar:
    Daha yüksek risk seviyesi kazanır.
    """
    return max(int(model_level_ai), int(rule_level_ai))