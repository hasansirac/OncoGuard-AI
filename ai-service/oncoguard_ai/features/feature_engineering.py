"""
feature_engineering.py
======================
Ham veriyi (baseline lab + gunluk loglar) modelin anlayacagi AKILLI ozelliklere
cevirir.

Tasarim ilkeleri:
- Saf fonksiyonlar, test edilebilir.
- "Veri yok != risk yok": eksik gun sayilir, MissingDataScore uretilir.
- Kisisel baseline: ilk birkac gunun ortalamasi -> delta hesabi.
- Pencere = son N gercek takvim gunu.
- Trend = basit lineer egim.

Girdi sozlesmesi
----------------
patient: dict        -> Age, Gender, WeightKg, HeightCm, CancerType, Stage,
                        TreatmentType, ECOG, PreviousNeutropenia, CycleDay ...
baseline_lab: dict   -> BaselineANC, BaselineWBC, BaselineCRP, BaselineAlbumin,
                        BaselineCreatinine, BaselineAST, BaselineALT,
                        BaselinePlatelet, BaselineHemoglobin, BaselineBilirubin,
                        BaselineTSH, BaselineFreeT4 ...
daily_logs: list[dict] -> her gun:
        LogDate, Temperature, Fatigue, Pain, Nausea, VomitingCount, DiarrheaCount,
        Cough, Dyspnea, SkinRash, Dizziness, Confusion, BleedingBruising,
        ProteinIntake, CalorieIntake, WaterIntakeMl, AppetiteScore,
        MealCompletionRatio, WeightKg, MedicationTaken(0/1), MissedDoseCount,
        OxygenSaturation, ActivityLevel, SleepHours ...
"""

from __future__ import annotations

from datetime import datetime, timedelta
from statistics import mean
from typing import Any

from oncoguard_ai.core import clinical_constants as cc


# ---------------------------------------------------------------------------
# Kucuk yardimcilar
# ---------------------------------------------------------------------------
def _vals(logs: list[dict], key: str) -> list[float]:
    """Bir kolonun None olmayan degerlerini sirayla dondurur."""
    out = []
    for r in logs:
        v = r.get(key)
        if v is not None:
            out.append(float(v))
    return out


def _window(logs: list[dict], n: int) -> list[dict]:
    """
    Tarih bilgisi yoksa fallback olarak son n kaydi dondurur.
    Asil sistem LogDate geldiginde _calendar_window kullanir.
    """
    return logs[-n:] if n <= len(logs) else logs[:]


def _parse_log_date(value):
    """
    LogDate degerini date objesine cevirir.

    Kabul edilen ornekler:
    - 2026-06-17
    - 2026-06-17T10:00:00
    - 2026-06-17T10:00:00Z
    """
    if value is None:
        return None

    if isinstance(value, datetime):
        return value.date()

    text = str(value).strip()
    if not text:
        return None

    try:
        return datetime.fromisoformat(text.replace("Z", "+00:00")).date()
    except Exception:
        try:
            return datetime.strptime(text[:10], "%Y-%m-%d").date()
        except Exception:
            return None


def _calendar_window(logs: list[dict], n: int) -> list[dict]:
    """
    Son n gercek takvim gunundeki loglari dondurur.

    Ornek:
    latest = 2026-06-17, n=3
    pencere = 2026-06-15, 2026-06-16, 2026-06-17

    Eger 15 yok, 16 var, 17 var ise collected day = 2 olur.
    """
    if not logs:
        return []

    dated = []
    for log in logs:
        d = _parse_log_date(log.get("LogDate"))
        if d is not None:
            dated.append((d, log))

    if not dated:
        return _window(logs, n)

    latest_date = max(d for d, _ in dated)
    start_date = latest_date - timedelta(days=n - 1)

    return [
        log for d, log in dated
        if start_date <= d <= latest_date
    ]


def _sort_unique_by_log_date(logs: list[dict]) -> list[dict]:
    """
    LogDate'e gore siralar ve ayni takvim gunu icin tek kayit tutar.
    Duplicate varsa son gelen kayit kazanir.
    """
    dated = {}
    undated = []

    for log in logs:
        d = _parse_log_date(log.get("LogDate"))
        if d is None:
            undated.append(log)
        else:
            dated[d.isoformat()] = log

    return [dated[k] for k in sorted(dated.keys())] + undated


def _mean_or(logs: list[dict], key: str, default=None):
    v = _vals(logs, key)
    return mean(v) if v else default


def _slope(values: list[float]) -> float:
    """
    Basit lineer regresyon egimi.
    Pozitif = artiyor, negatif = azaliyor.
    """
    n = len(values)
    if n < 2:
        return 0.0

    xs = list(range(n))
    mx = mean(xs)
    my = mean(values)

    num = sum((x - mx) * (y - my) for x, y in zip(xs, values))
    den = sum((x - mx) ** 2 for x in xs)

    return num / den if den else 0.0


def _count_ge(logs: list[dict], key: str, thr: float) -> int:
    return sum(1 for v in _vals(logs, key) if v >= thr)


def _safe_div(a, b):
    return a / b if (b not in (None, 0)) else 0.0


# ---------------------------------------------------------------------------
# Kisisel baseline
# ---------------------------------------------------------------------------
def personal_baselines(logs: list[dict], warmup_days: int = 3) -> dict:
    warm = logs[:warmup_days] if len(logs) >= warmup_days else logs

    return {
        "FatigueBaseline": _mean_or(warm, "Fatigue", 0.0),
        "AppetiteBaseline": _mean_or(warm, "AppetiteScore", 4.0),
        "PainBaseline": _mean_or(warm, "Pain", 0.0),
    }


# ---------------------------------------------------------------------------
# Ana ozellik uretici
# ---------------------------------------------------------------------------
def build_features(
    patient: dict,
    baseline_lab: dict,
    daily_logs: list[dict],
) -> dict[str, Any]:
    f: dict[str, Any] = {}

    # Tarihe gore sirala, ayni gun tekrarlarini teke dusur.
    daily_logs = _sort_unique_by_log_date(daily_logs)

    # Toplam benzersiz kayitli gun sayisi.
    n_days = len(daily_logs)

    # Gercek tarih pencereleri.
    # Bunlar son N kayit degil, son N takvim gunudur.
    w3 = _calendar_window(daily_logs, 3)
    w7 = _calendar_window(daily_logs, 7)
    w30 = _calendar_window(daily_logs, 30)

    weight_kg = patient.get("WeightKg") or 70.0
    height_cm = patient.get("HeightCm") or 170.0

    bmi = _safe_div(weight_kg, (height_cm / 100.0) ** 2)
    f["BMI"] = round(bmi, 1)

    # --- Profil pass-through ---
    f["Age"] = patient.get("Age")
    f["ECOG"] = patient.get("ECOG")
    f["CycleDay"] = patient.get("CycleDay")
    f["CycleNumber"] = patient.get("CycleNumber")

    f["PreviousNeutropenia"] = int(bool(patient.get("PreviousNeutropenia")))
    f["PreviousTreatmentDelay"] = int(bool(patient.get("PreviousTreatmentDelay")))
    f["PreviousSevereToxicity"] = int(bool(patient.get("PreviousSevereToxicity")))
    f["DoseReductionFlag"] = int(bool(patient.get("DoseReductionFlag")))
    f["GCSFUseFlag"] = int(bool(patient.get("GCSFUseFlag")))

    # --- Komorbiditeler ---
    for c in cc.COMORBIDITIES:
        f[c] = int(bool(patient.get(c)))

    f["ComorbidityCount"] = sum(f[c] for c in cc.COMORBIDITIES)

    # --- Baseline lab pass-through + CTCAE grade ---
    anc = baseline_lab.get("BaselineANC")
    crp = baseline_lab.get("BaselineCRP")
    alb = baseline_lab.get("BaselineAlbumin")

    f["BaselineANC"] = anc
    f["BaselineWBC"] = baseline_lab.get("BaselineWBC")
    f["BaselineCRP"] = crp
    f["BaselineAlbumin"] = alb
    f["BaselineCreatinine"] = baseline_lab.get("BaselineCreatinine")
    f["BaselineAST"] = baseline_lab.get("BaselineAST")
    f["BaselineALT"] = baseline_lab.get("BaselineALT")
    f["BaselinePlatelet"] = baseline_lab.get("BaselinePlatelet")
    f["BaselineHemoglobin"] = baseline_lab.get("BaselineHemoglobin")
    f["BaselineBilirubin"] = baseline_lab.get("BaselineBilirubin")
    f["BaselineTSH"] = baseline_lab.get("BaselineTSH")
    f["BaselineFreeT4"] = baseline_lab.get("BaselineFreeT4")

    f["ANCGrade"] = cc.ctcae_anc_grade(anc)
    f["PlateletGrade"] = cc.ctcae_platelet_grade(
        baseline_lab.get("BaselinePlatelet")
    )
    f["HemoglobinGrade"] = cc.ctcae_hemoglobin_grade(
        baseline_lab.get("BaselineHemoglobin")
    )
    f["ASTGrade"] = cc.ctcae_ast_grade(
        baseline_lab.get("BaselineAST")
    )
    f["ALTGrade"] = cc.ctcae_alt_grade(
        baseline_lab.get("BaselineALT")
    )
    f["BilirubinGrade"] = cc.ctcae_bilirubin_grade(
        baseline_lab.get("BaselineBilirubin")
    )
    f["CreatinineGrade"] = cc.ctcae_creatinine_grade(
        baseline_lab.get("BaselineCreatinine")
    )
    f["AlbuminGrade"] = cc.ctcae_albumin_grade(alb)

    # --- Beslenme ozellikleri ---
    p_target = cc.protein_target_g(weight_kg)
    c_target = cc.calorie_target_kcal(weight_kg)
    wat_target = cc.water_target_ml(weight_kg)

    def ratio_series(logs, intake_key, target):
        return [_safe_div(v, target) for v in _vals(logs, intake_key)]

    pr3 = ratio_series(w3, "ProteinIntake", p_target)
    pr7 = ratio_series(w7, "ProteinIntake", p_target)
    cr7 = ratio_series(w7, "CalorieIntake", c_target)

    f["ProteinRatioMean3"] = round(mean(pr3), 3) if pr3 else None
    f["ProteinRatioMean7"] = round(mean(pr7), 3) if pr7 else None
    f["CalorieRatioMean7"] = round(mean(cr7), 3) if cr7 else None
    f["ProteinDeficitDays7"] = sum(1 for r in pr7 if r < 0.8)
    f["AppetiteMean7"] = _mean_or(w7, "AppetiteScore")

    f["MealSkippingCount7"] = sum(
        1 for v in _vals(w7, "MealCompletionRatio")
        if v <= 0.25
    )

    # --- Kilo / cachexia ---
    f["WeightLossPct7"] = _weight_loss_pct(daily_logs, 7)
    f["WeightLossPct30"] = _weight_loss_pct(daily_logs, 30)

    f["CRPAlbuminRatio"] = (
        round(_safe_div(crp, alb), 3)
        if (crp and alb)
        else None
    )

    f["ActivityDecline7"] = -_slope(_vals(w7, "ActivityLevel"))

    # --- Enfeksiyon / bagisiklik ---
    f["FeverCount3"] = _count_ge(w3, "Temperature", cc.FEVER_SINGLE_C)
    f["FeverCount7"] = _count_ge(w7, "Temperature", cc.FEVER_SINGLE_C)
    f["MaxTemp3"] = max(_vals(w3, "Temperature"), default=None)

    f["FeverAndLowANCFlag"] = int(
        f["FeverCount3"] > 0 and cc.ctcae_anc_grade(anc) >= 2
    )

    f["FatigueMean3"] = _mean_or(w3, "Fatigue")

    bl = personal_baselines(daily_logs)

    f["FatigueDelta"] = round(
        (_mean_or(w3, "Fatigue", 0.0) or 0.0)
        - bl["FatigueBaseline"],
        2,
    )

    f["AppetiteDelta"] = round(
        (_mean_or(w3, "AppetiteScore", 4.0) or 4.0)
        - bl["AppetiteBaseline"],
        2,
    )

    f["InfectionSymptomScore"] = round(
        f["FeverCount3"]
        + (_mean_or(w3, "Cough", 0.0) or 0.0)
        + (_mean_or(w3, "Dyspnea", 0.0) or 0.0)
        + 0.5 * (_mean_or(w3, "Fatigue", 0.0) or 0.0),
        2,
    )

    # --- Hidrasyon ---
    wr3 = ratio_series(w3, "WaterIntakeMl", wat_target)

    f["WaterRatioMean3"] = round(mean(wr3), 3) if wr3 else None
    f["VomitingCount3"] = sum(int(v) for v in _vals(w3, "VomitingCount"))
    f["DiarrheaCount3"] = sum(int(v) for v in _vals(w3, "DiarrheaCount"))
    f["FluidLossScore3"] = f["VomitingCount3"] + f["DiarrheaCount3"]
    f["DizzinessMean3"] = _mean_or(w3, "Dizziness", 0.0)

    # --- Organ toksisitesi / semptom trendleri ---
    f["DiarrheaTrend7"] = round(_slope(_vals(w7, "DiarrheaCount")), 3)
    f["SkinRashTrend7"] = round(_slope(_vals(w7, "SkinRash")), 3)
    f["DyspneaTrend7"] = round(_slope(_vals(w7, "Dyspnea")), 3)
    f["MinSpO2_3"] = min(_vals(w3, "OxygenSaturation"), default=None)

    # --- Ilac uyumu ---
    taken = _vals(w7, "MedicationTaken")
    f["MedicationAdherence7"] = round(mean(taken), 3) if taken else None
    f["MissedDoseCount7"] = sum(int(v) for v in _vals(w7, "MissedDoseCount"))

    # --- Eksik veri ---
    f["MissingLogCount7"] = max(0, 7 - len(w7))
    f["MissingNutritionFlag"] = int(len(_vals(w7, "ProteinIntake")) == 0)
    f["MissingMedicationFlag"] = int(len(_vals(w7, "MedicationTaken")) == 0)

    expected = 7 * 3

    present = (
        len(_vals(w7, "Fatigue"))
        + len(_vals(w7, "ProteinIntake"))
        + len(_vals(w7, "MedicationTaken"))
    )

    f["MissingDataScore7"] = round(
        1.0 - _safe_div(present, expected),
        3,
    )

    # Modelde direkt kullanilmayan ama monitoring kararinda kullanilan sayaclar.
    # Bunlari FEATURE_ORDER listesine eklemiyoruz; model 87 feature bekliyor.
    f["NDaysLogged"] = n_days
    f["LoggedDays3"] = len(w3)
    f["LoggedDays7"] = len(w7)
    f["LoggedDays30"] = len(w30)

    # --- TrendDirection ---
    def _trend_dir(values, worse_when_rising: bool, thr: float):
        if len(values) < 3:
            return 0

        sl = _slope(values)

        if worse_when_rising:
            return 1 if sl > thr else (-1 if sl < -thr else 0)

        return 1 if sl < -thr else (-1 if sl > thr else 0)

    f["FatigueTrendDir"] = _trend_dir(
        _vals(w7, "Fatigue"),
        True,
        0.10,
    )

    f["AppetiteTrendDir"] = _trend_dir(
        _vals(w7, "AppetiteScore"),
        False,
        0.08,
    )

    f["WaterTrendDir"] = _trend_dir(
        [_safe_div(v, wat_target) for v in _vals(w7, "WaterIntakeMl")],
        False,
        0.04,
    )

    f["WeightTrendDir"] = _trend_dir(
        _vals(w7, "WeightKg"),
        False,
        0.10,
    )

    return f


def _weight_loss_pct(logs: list[dict], window_days: int) -> float | None:
    """Pencere basindaki agirliga gore yuzde kilo kaybi. Pozitif = kayip."""
    w = _calendar_window(logs, window_days + 1)

    weights = [
        (i, r.get("WeightKg"))
        for i, r in enumerate(w)
        if r.get("WeightKg") is not None
    ]

    if len(weights) < 2:
        return None

    first = weights[0][1]
    last = weights[-1][1]

    if not first:
        return None

    return round((first - last) / first * 100.0, 2)


# Modeller icin sabit ozellik sirasi.
# DIKKAT:
# LoggedDays3 / LoggedDays7 / LoggedDays30 buraya eklenmez.
# Onlar model feature'i degil, monitoring threshold icin yardimci alandir.
FEATURE_ORDER = [
    "Age",
    "ECOG",
    "CycleDay",
    "CycleNumber",
    "BMI",
    "PreviousNeutropenia",
    "PreviousTreatmentDelay",
    "PreviousSevereToxicity",
    "DoseReductionFlag",
    "GCSFUseFlag",
    "HasDiabetes",
    "HasHypertension",
    "HasChronicKidneyDisease",
    "HasHeartFailure",
    "HasCOPD",
    "HasLiverDisease",
    "ComorbidityCount",
    "BaselineANC",
    "BaselineWBC",
    "BaselineCRP",
    "BaselineAlbumin",
    "BaselineCreatinine",
    "BaselineAST",
    "BaselineALT",
    "BaselinePlatelet",
    "BaselineHemoglobin",
    "BaselineBilirubin",
    "BaselineTSH",
    "BaselineFreeT4",
    "ANCGrade",
    "PlateletGrade",
    "HemoglobinGrade",
    "ASTGrade",
    "ALTGrade",
    "BilirubinGrade",
    "CreatinineGrade",
    "AlbuminGrade",
    "ProteinRatioMean3",
    "ProteinRatioMean7",
    "CalorieRatioMean7",
    "ProteinDeficitDays7",
    "AppetiteMean7",
    "MealSkippingCount7",
    "WeightLossPct7",
    "WeightLossPct30",
    "CRPAlbuminRatio",
    "ActivityDecline7",
    "FeverCount3",
    "FeverCount7",
    "MaxTemp3",
    "FeverAndLowANCFlag",
    "FatigueMean3",
    "FatigueDelta",
    "AppetiteDelta",
    "InfectionSymptomScore",
    "WaterRatioMean3",
    "VomitingCount3",
    "DiarrheaCount3",
    "FluidLossScore3",
    "DizzinessMean3",
    "DiarrheaTrend7",
    "SkinRashTrend7",
    "DyspneaTrend7",
    "MinSpO2_3",
    "MedicationAdherence7",
    "MissedDoseCount7",
    "FatigueTrendDir",
    "AppetiteTrendDir",
    "WaterTrendDir",
    "WeightTrendDir",
    "MissingLogCount7",
    "MissingNutritionFlag",
    "MissingMedicationFlag",
    "MissingDataScore7",
]


if __name__ == "__main__":
    patient = {
        "Age": 62,
        "WeightKg": 70,
        "HeightCm": 170,
        "ECOG": 1,
        "CycleDay": 8,
        "PreviousNeutropenia": True,
        "TreatmentType": "Chemotherapy",
    }

    baseline = {
        "BaselineANC": 900,
        "BaselineCRP": 40,
        "BaselineAlbumin": 3.2,
        "BaselineCreatinine": 1.0,
        "BaselineAST": 30,
        "BaselineALT": 28,
        "BaselinePlatelet": 140,
        "BaselineHemoglobin": 10.5,
    }

    logs = []

    for d in range(7):
        logs.append({
            "LogDate": f"2026-06-{10 + d:02d}",
            "Temperature": 36.8 + (1.8 if d >= 5 else 0.0),
            "Fatigue": 1 + d * 0.4,
            "Pain": 1,
            "Nausea": 1,
            "VomitingCount": 1 if d >= 4 else 0,
            "DiarrheaCount": 0,
            "Cough": 1 if d >= 5 else 0,
            "Dyspnea": 0,
            "SkinRash": 0,
            "Dizziness": 0,
            "ProteinIntake": 70 - d * 6,
            "CalorieIntake": 1900 - d * 120,
            "WaterIntakeMl": 2100 - d * 100,
            "AppetiteScore": 4 - d * 0.4,
            "MealCompletionRatio": max(0.25, 1.0 - d * 0.12),
            "WeightKg": 70 - d * 0.25,
            "MedicationTaken": 1,
            "MissedDoseCount": 0,
            "OxygenSaturation": 97,
            "ActivityLevel": 3 - d * 0.3,
            "SleepHours": 7,
        })

    feats = build_features(patient, baseline, logs)

    import json

    print(json.dumps({
        k: feats[k] for k in [
            "NDaysLogged",
            "LoggedDays3",
            "LoggedDays7",
            "LoggedDays30",
            "ANCGrade",
            "AlbuminGrade",
            "FeverCount3",
            "FeverAndLowANCFlag",
            "ProteinRatioMean7",
            "WeightLossPct7",
            "FatigueDelta",
            "InfectionSymptomScore",
            "MissingDataScore7",
        ]
    }, indent=2))

    print(
        "feature_engineering.py: ozellik vektoru uretildi, alan sayisi =",
        len(FEATURE_ORDER),
    )
