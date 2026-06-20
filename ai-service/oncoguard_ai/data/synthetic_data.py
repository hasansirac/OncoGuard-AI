"""
synthetic_data.py
=================
Senaryo-tabanli sentetik onkoloji cycle dataseti uretir (FAZ 11).

METODOLOJI (neden boyle?)
-------------------------
Naif yaklasim: label = feature'larin deterministik fonksiyonu. SORUN: model
bunu %100 ezberler, metrikler anlamsiz (hep 1.0) cikar ve hoca "bu data leakage"
der. DOGRU yaklasim (sentetik klinik veride standart):

  latent_severity  --(olcum gurultusu + eksik veri)-->  gozlenen feature'lar
        |
        +--(esik + KUCUK label gurultusu)--> label (Green/Yellow/Orange/Red)

Yani her hasta icin gizli bir "klinik siddet" trajektorisi uretilir; hem
feature'lar hem label'lar ondan turetilir ama ARALARINA gurultu girer. Boylece:
- Model genelleme yapmak zorunda kalir (metrikler gercekci, ~0.8-0.95).
- Ablation calismasi gunluk feature'larin katkisini GERCEKTEN gosterir.
- Esikler clinical_constants ile tutarli -> literature uygun.

Senaryolar dokuman "SENTETIK DATASET SENARYOLARI" bolumuyle birebir.
"""

from __future__ import annotations
import numpy as np

from oncoguard_ai.core import clinical_constants as cc
from oncoguard_ai.core.clinical_constants import RiskLevel
from oncoguard_ai.features.feature_engineering import build_features, FEATURE_ORDER

SCENARIOS = [
    "StablePatient", "InfectionDeveloping", "FebrileNeutropeniaWarning",
    "NutritionDecline", "CachexiaProgression", "DehydrationEpisode",
    "RenalToxicitySignal", "HepaticToxicitySignal", "ImmunotherapyAdverseEvent",
    "TreatmentDelayRisk", "MixedDeterioration", "MissingDataHeavyPatient",
]

LABEL_COLS = [
    "InfectionRisk", "FebrileNeutropeniaRisk", "MalnutritionRisk",
    "CachexiaRisk", "DehydrationRisk", "RenalToxicityRisk",
    "HepaticToxicityRisk", "ImmunotherapyAdverseEventRisk",
    "TreatmentDelayRisk", "OverallClinicalDeteriorationRisk",
]

CANCER_TYPES = ["Lung", "Breast", "Colorectal", "Lymphoma", "Gastric"]


def _sev_to_level(sev: float, rng, noise=0.25) -> int:
    """Latent siddet [0..3] -> 0-3 label, kucuk gurultuyle.
    Esikler 0.5/1.5/2.5 etrafinda; gurultu sinir vakalarini biraz kaydirir."""
    s = sev + rng.normal(0, noise)
    if s >= 2.5:
        return 3
    if s >= 1.5:
        return 2
    if s >= 0.6:
        return 1
    return 0


def _measure(true_val, rng, rel_noise=0.08, lo=None):
    """Gercek degerin gurultulu olcumu (lab/semptom)."""
    v = true_val * (1 + rng.normal(0, rel_noise))
    if lo is not None:
        v = max(lo, v)
    return v


def generate_patient(scenario: str, rng: np.random.Generator):
    """Tek bir hasta-cycle uretir: (patient, baseline_lab, daily_logs, latent)."""
    age = int(rng.integers(35, 80))
    sex = rng.choice(["M", "F"])
    weight = float(rng.normal(72, 12)); weight = max(45, weight)
    height = float(rng.normal(170, 9))
    treatment = "Chemotherapy"
    cancer = rng.choice(CANCER_TYPES)

    # --- Senaryoya gore latent siddet (0=stabil .. 3=kritik) ve baseline ---
    sev = {k: 0.0 for k in LABEL_COLS}
    # makul "normal" baseline lab degerleri
    lab = {
        "BaselineANC": float(rng.normal(3500, 900)),
        "BaselineWBC": float(rng.normal(6500, 1500)),
        "BaselineCRP": float(abs(rng.normal(4, 3))),
        "BaselineAlbumin": float(rng.normal(4.0, 0.3)),
        "BaselineCreatinine": float(rng.normal(0.9, 0.15)),
        "BaselineAST": float(rng.normal(25, 8)),
        "BaselineALT": float(rng.normal(25, 8)),
        "BaselinePlatelet": float(rng.normal(230, 60)),
        "BaselineHemoglobin": float(rng.normal(13, 1.3)),
        "BaselineBilirubin": float(rng.normal(0.7, 0.2)),
        "BaselineTSH": float(rng.normal(2.0, 0.7)),
        "BaselineFreeT4": float(rng.normal(1.2, 0.2)),
    }
    n_days = int(rng.integers(7, 15))
    prev_neutropenia = bool(rng.random() < 0.2)
    prev_delay = False
    missing_rate = 0.08
    # trajektori "egim" parametreleri (gun basina kotuleme)
    traj = dict(protein=0.0, water=0.0, fatigue=0.0, weight=0.0,
                fever_days=0, vomit=0, diarrhea=0, rash=0.0, dyspnea=0.0,
                activity=0.0)

    if scenario == "StablePatient":
        pass  # her sey normal

    elif scenario == "InfectionDeveloping":
        lab["BaselineANC"] = float(rng.normal(1300, 300))
        lab["BaselineCRP"] = float(rng.normal(35, 15))
        traj["fever_days"] = int(rng.integers(1, 3))
        traj["fatigue"] = 0.35
        sev["InfectionRisk"] = rng.uniform(1.6, 2.6)
        sev["FebrileNeutropeniaRisk"] = rng.uniform(0.8, 1.8)

    elif scenario == "FebrileNeutropeniaWarning":
        lab["BaselineANC"] = float(rng.normal(700, 200))  # <1000
        lab["BaselineCRP"] = float(rng.normal(55, 20))
        traj["fever_days"] = int(rng.integers(2, 5))
        traj["fatigue"] = 0.4
        prev_neutropenia = True
        sev["FebrileNeutropeniaRisk"] = rng.uniform(2.4, 3.0)
        sev["InfectionRisk"] = rng.uniform(2.2, 3.0)
        sev["TreatmentDelayRisk"] = rng.uniform(1.8, 2.8)

    elif scenario == "NutritionDecline":
        lab["BaselineAlbumin"] = float(rng.normal(3.2, 0.25))
        traj["protein"] = rng.uniform(0.05, 0.09)  # gun basina oran dususu
        traj["fatigue"] = 0.2
        sev["MalnutritionRisk"] = rng.uniform(1.8, 2.8)

    elif scenario == "CachexiaProgression":
        lab["BaselineAlbumin"] = float(rng.normal(3.0, 0.25))
        lab["BaselineCRP"] = float(rng.normal(30, 12))
        traj["protein"] = 0.06
        traj["weight"] = rng.uniform(0.15, 0.35)  # kg/gun kayip
        traj["activity"] = 0.25
        traj["fatigue"] = 0.3
        sev["CachexiaRisk"] = rng.uniform(1.8, 2.8)
        sev["MalnutritionRisk"] = rng.uniform(1.2, 2.2)

    elif scenario == "DehydrationEpisode":
        traj["water"] = rng.uniform(0.06, 0.10)
        traj["vomit"] = int(rng.integers(1, 4))
        traj["diarrhea"] = int(rng.integers(1, 5))
        sev["DehydrationRisk"] = rng.uniform(1.8, 2.8)

    elif scenario == "RenalToxicitySignal":
        lab["BaselineCreatinine"] = float(rng.normal(1.9, 0.4))  # ~G2
        traj["water"] = 0.05
        sev["RenalToxicityRisk"] = rng.uniform(1.8, 2.8)

    elif scenario == "HepaticToxicitySignal":
        fold = rng.uniform(3.5, 8.0)
        lab["BaselineAST"] = cc.REFERENCE["AST"]["uln"] * fold
        lab["BaselineALT"] = cc.REFERENCE["ALT"]["uln"] * fold * rng.uniform(0.8, 1.2)
        lab["BaselineBilirubin"] = float(rng.normal(1.6, 0.5))
        sev["HepaticToxicityRisk"] = rng.uniform(1.8, 2.9)

    elif scenario == "ImmunotherapyAdverseEvent":
        treatment = "Immunotherapy"
        lab["BaselineTSH"] = float(rng.normal(7.0, 2.5))
        traj["diarrhea"] = int(rng.integers(1, 3))
        traj["rash"] = rng.uniform(0.15, 0.35)
        traj["dyspnea"] = rng.uniform(0.10, 0.30)
        sev["ImmunotherapyAdverseEventRisk"] = rng.uniform(1.8, 2.8)

    elif scenario == "TreatmentDelayRisk":
        lab["BaselineANC"] = float(rng.normal(900, 250))
        lab["BaselinePlatelet"] = float(rng.normal(60, 20))
        lab["BaselineHemoglobin"] = float(rng.normal(8.5, 0.8))
        prev_delay = True
        sev["TreatmentDelayRisk"] = rng.uniform(2.0, 2.9)
        sev["InfectionRisk"] = rng.uniform(0.8, 1.8)

    elif scenario == "MixedDeterioration":
        lab["BaselineANC"] = float(rng.normal(1100, 300))
        lab["BaselineAlbumin"] = float(rng.normal(3.1, 0.2))
        traj.update(protein=0.05, water=0.05, fatigue=0.35, weight=0.15,
                    fever_days=int(rng.integers(0, 2)), vomit=1, diarrhea=2)
        sev["InfectionRisk"] = rng.uniform(1.2, 2.2)
        sev["MalnutritionRisk"] = rng.uniform(1.2, 2.2)
        sev["DehydrationRisk"] = rng.uniform(1.0, 2.0)

    elif scenario == "MissingDataHeavyPatient":
        missing_rate = rng.uniform(0.55, 0.8)  # cogu gun bos
        # altta yatan hafif kotuleme var ama veri yetersiz -> belirsizlik
        traj["fatigue"] = 0.2
        sev["OverallClinicalDeteriorationRisk"] = rng.uniform(0.8, 1.6)

    # --- Gunluk loglari uret ---
    p_target = cc.protein_target_g(weight)
    c_target = cc.calorie_target_kcal(weight)
    w_target = cc.water_target_ml(weight)
    base_fatigue = rng.uniform(0.5, 2.0)
    base_appetite = rng.uniform(3.0, 4.0)
    base_activity = rng.uniform(2.5, 4.0)
    fever_day_idx = set(rng.choice(range(n_days),
                        size=min(traj["fever_days"], n_days), replace=False)
                        ) if traj["fever_days"] else set()

    logs = []
    cur_weight = weight
    for d in range(n_days):
        if rng.random() < missing_rate:
            logs.append({})  # eksik gun
            continue
        protein_ratio = max(0.1, 1.0 - traj["protein"] * d + rng.normal(0, 0.07))
        water_ratio = max(0.1, 1.0 - traj["water"] * d + rng.normal(0, 0.07))
        cur_weight -= traj["weight"]
        fatigue = base_fatigue + traj["fatigue"] * d + rng.normal(0, 0.3)
        temp = 36.8 + rng.normal(0, 0.2)
        if d in fever_day_idx:
            temp = rng.uniform(38.3, 39.2)
        logs.append({
            "Temperature": round(temp, 1),
            "Fatigue": float(np.clip(fatigue, 0, 4)),
            "Pain": float(np.clip(base_fatigue * 0.6 + rng.normal(0, 0.3), 0, 4)),
            "Nausea": float(np.clip(rng.normal(0.5 + traj["vomit"] * 0.3, 0.4), 0, 4)),
            "VomitingCount": int(max(0, rng.poisson(traj["vomit"]))),
            "DiarrheaCount": int(max(0, rng.poisson(traj["diarrhea"]))),
            "Cough": float(np.clip(rng.normal(0.3, 0.3), 0, 4)),
            "Dyspnea": float(np.clip(traj["dyspnea"] * d + rng.normal(0, 0.2), 0, 4)),
            "SkinRash": float(np.clip(traj["rash"] * d + rng.normal(0, 0.2), 0, 4)),
            "Dizziness": float(np.clip(traj["water"] * d * 10 + rng.normal(0, 0.3), 0, 4)),
            "Confusion": 0.0,
            "BleedingBruising": 0.0,
            "ProteinIntake": round(_measure(protein_ratio * p_target, rng, lo=0), 1),
            "CalorieIntake": round(_measure(protein_ratio * c_target, rng, lo=0), 1),
            "WaterIntakeMl": round(_measure(water_ratio * w_target, rng, lo=0), 1),
            "AppetiteScore": float(np.clip(
                base_appetite - traj["protein"] * d * 8 + rng.normal(0, 0.4), 0, 4)),
            "MealCompletionRatio": float(np.clip(protein_ratio + rng.normal(0, 0.08), 0, 1)),
            "WeightKg": round(cur_weight + rng.normal(0, 0.2), 1),
            "MedicationTaken": int(rng.random() > 0.1),
            "MissedDoseCount": int(rng.random() < 0.12),
            "OxygenSaturation": int(np.clip(98 - traj["dyspnea"] * d * 2
                                            + rng.normal(0, 1), 85, 100)),
            "ActivityLevel": float(np.clip(
                base_activity - traj["activity"] * d + rng.normal(0, 0.3), 0, 5)),
            "SleepHours": float(np.clip(rng.normal(7, 1), 3, 10)),
        })

    patient = {"Age": age, "Gender": sex, "WeightKg": round(weight, 1),
               "HeightCm": round(height, 1), "CancerType": cancer, "Stage": int(rng.integers(1, 5)),
               "TreatmentType": treatment, "ECOG": int(rng.integers(0, 3)),
               "CycleDay": int(rng.integers(5, 14)),
               "PreviousNeutropenia": prev_neutropenia,
               "PreviousTreatmentDelay": prev_delay}
    return patient, lab, logs, sev


def make_labels(features: dict, latent_sev: dict, rng) -> dict[str, int]:
    """Latent siddet + gozlenen feature ipuclari -> 0-3 label.
    Latent agirlikli; feature ipuclari kucuk duzeltme yapar (gercekci kuplaj)."""
    labels = {}
    # feature-temelli ek siddet ipuclari (latent ile ayni yone, klinik tutarli)
    cues = {
        "InfectionRisk": 0.6 * features.get("FeverAndLowANCFlag", 0)
                         + 0.4 * min(features.get("InfectionSymptomScore", 0) or 0, 4) / 4 * 3,
        "FebrileNeutropeniaRisk": 3.0 * features.get("FeverAndLowANCFlag", 0),
        "MalnutritionRisk": max(0, (0.8 - (features.get("ProteinRatioMean7") or 1))) * 3
                            + 0.5 * features.get("AlbuminGrade", 0),
        "CachexiaRisk": min((features.get("WeightLossPct30") or 0) / 5.0, 3),
        "DehydrationRisk": max(0, (0.7 - (features.get("WaterRatioMean3") or 1))) * 4,
        "RenalToxicityRisk": features.get("CreatinineGrade", 0),
        "HepaticToxicityRisk": max(features.get("ASTGrade", 0), features.get("ALTGrade", 0)),
        "ImmunotherapyAdverseEventRisk": 0.0,
        "TreatmentDelayRisk": max(features.get("ANCGrade", 0), features.get("PlateletGrade", 0)),
        "OverallClinicalDeteriorationRisk": 0.0,
    }
    for k in LABEL_COLS:
        if k in ("OverallClinicalDeteriorationRisk",):
            continue
        sev = 0.7 * latent_sev.get(k, 0.0) + 0.3 * min(cues.get(k, 0.0), 3.0)
        labels[k] = _sev_to_level(sev, rng)
    # Overall = bilesenlerin en yukseni (kucuk gurultuyle)
    comp = max(labels.values()) if labels else 0
    labels["OverallClinicalDeteriorationRisk"] = _sev_to_level(
        max(comp, latent_sev.get("OverallClinicalDeteriorationRisk", 0.0)),
        rng, noise=0.15)
    return labels


def generate_dataset(n_per_scenario: int = 200, seed: int = 42):
    """Tum senaryolardan denge ile veri uretir -> (rows, columns)."""
    import pandas as pd
    rng = np.random.default_rng(seed)
    rows = []
    for scenario in SCENARIOS:
        for _ in range(n_per_scenario):
            patient, lab, logs, latent = generate_patient(scenario, rng)
            feats = build_features(patient, lab, logs)
            labels = make_labels(feats, latent, rng)
            row = {k: feats.get(k) for k in FEATURE_ORDER}
            row.update({f"label_{k}": v for k, v in labels.items()})
            row["scenario"] = scenario
            row["CancerType"] = patient["CancerType"]
            row["TreatmentType"] = patient["TreatmentType"]
            rows.append(row)
    df = pd.DataFrame(rows)
    return df


if __name__ == "__main__":
    df = generate_dataset(n_per_scenario=150, seed=7)
    print("Dataset boyutu:", df.shape)
    print("\nOverall risk label dagilimi (0=Green..3=Red):")
    print(df["label_OverallClinicalDeteriorationRisk"].value_counts().sort_index())
    print("\nFebril notropeni label dagilimi:")
    print(df["label_FebrileNeutropeniaRisk"].value_counts().sort_index())
    print("\nMissingData senaryosunda ort. MissingDataScore7:",
          round(df[df.scenario == "MissingDataHeavyPatient"]["MissingDataScore7"].mean(), 3))
