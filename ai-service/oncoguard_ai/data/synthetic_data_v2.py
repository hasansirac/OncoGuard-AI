"""
synthetic_data_v2.py
====================
Generator v2 - review sonrasi eklenenler:
  + Fonksiyonel komorbiditeler (latent siddeti gercekten degistirir)
  + Treatment tolerance: CycleNumber, PreviousSevereToxicity,
    DoseReductionFlag, GCSFUseFlag
  + TreatmentType <-> Scenario klinik eslemesi (Hormone+irAE gibi sacmaliklari onler)
  + CancerType cesitliligi + Leukemia/Lymphoma icin AYRI baseline lab dagilimi
  + Age x ECOG x Albumin prognostik modifier
  + Senaryo-ici "hafif vaka" varyasyonu (dagilim dengesi)
  + Yumusatilmis Overall (korroborasyon kurali, saf max degil)
v1'deki latent_severity -> noise -> feature/label metodolojisi korunur.
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

CANCER_TYPES = ["Lung", "Breast", "Colorectal", "Lymphoma",
                "Gastric", "Pancreatic", "Leukemia"]
HEMATOLOGIC = {"Leukemia", "Lymphoma"}

# Tedavi turleri (kisaltma)
CHEMO, IMMUNO, CHEMOIMMUNO = "Chemotherapy", "Immunotherapy", "ChemoImmunotherapy"
TARGETED, RADIATION, HORMONE = "Targeted", "Radiation", "Hormone"
IMMUNO_CONTEXT = {IMMUNO, CHEMOIMMUNO}

# Her senaryo icin KLINIK OLARAK MAKUL tedavi turleri (+ secim agirliklari)
SCENARIO_TREATMENTS = {
    "StablePatient":            ([CHEMO, IMMUNO, TARGETED, RADIATION, HORMONE], None),
    "InfectionDeveloping":      ([CHEMO, CHEMOIMMUNO], None),
    "FebrileNeutropeniaWarning":([CHEMO], None),                  # FN kemo-kaynakli
    "NutritionDecline":         ([CHEMO, RADIATION, TARGETED, HORMONE], None),
    "CachexiaProgression":      ([CHEMO, TARGETED, HORMONE], None),
    "DehydrationEpisode":       ([CHEMO, TARGETED, RADIATION], None),
    "RenalToxicitySignal":      ([CHEMO, TARGETED], [0.7, 0.3]),  # cisplatin agirlikli
    "HepaticToxicitySignal":    ([CHEMO, TARGETED, IMMUNO], None),
    "ImmunotherapyAdverseEvent":([IMMUNO, CHEMOIMMUNO], None),    # irAE sadece immuno
    "TreatmentDelayRisk":       ([CHEMO, CHEMOIMMUNO], None),
    "MixedDeterioration":       ([CHEMO, CHEMOIMMUNO, TARGETED], None),
    "MissingDataHeavyPatient":  ([CHEMO, IMMUNO, TARGETED, HORMONE], None),
}

# ---------------------------------------------------------------------------
# DENGELEME (v2.1) — toplam satiri sismeden nadir-kritik sinifi guclendir.
# (a) SCENARIO_WEIGHTS: senaryonun goreli orneklem payi (nadir riskleri besler)
# (b) SCENARIO_TIER_PROBS: o senaryoda mild/normal/severe olasiligi
#     (nadir risklerde severe payi yuksek -> gercekten Red'e ulasir)
# Hedef: her risk icin Red >= ~400 (yaklasik 15k satirda).
# ---------------------------------------------------------------------------
SCENARIO_WEIGHTS = {
    "StablePatient": 1.0,            # makul Green payi (Overall icin)
    "InfectionDeveloping": 1.0,
    "FebrileNeutropeniaWarning": 1.1,
    "NutritionDecline": 0.9,
    "CachexiaProgression": 1.8,      # en seyrek Red -> en cok boost
    "DehydrationEpisode": 1.4,
    "RenalToxicitySignal": 1.5,
    "HepaticToxicitySignal": 1.2,
    "ImmunotherapyAdverseEvent": 1.2,
    "TreatmentDelayRisk": 0.7,       # zaten bol Red -> dusur
    "MixedDeterioration": 1.0,
    "MissingDataHeavyPatient": 0.6,  # belirsizlik; payi dusur
}

_DEFAULT_TIER = [0.30, 0.50, 0.20]
SCENARIO_TIER_PROBS = {   # [mild, normal, severe]
    "CachexiaProgression": [0.10, 0.40, 0.50],
    "RenalToxicitySignal": [0.15, 0.40, 0.45],
    "DehydrationEpisode": [0.15, 0.45, 0.40],
    "HepaticToxicitySignal": [0.15, 0.45, 0.40],
    "ImmunotherapyAdverseEvent": [0.15, 0.45, 0.40],
    "FebrileNeutropeniaWarning": [0.15, 0.45, 0.40],
    "InfectionDeveloping": [0.20, 0.45, 0.35],
}


def _sev_to_level(sev, rng, noise=0.25) -> int:
    s = sev + rng.normal(0, noise)
    if s >= 2.5:
        return 3
    if s >= 1.5:
        return 2
    if s >= 0.6:
        return 1
    return 0


def _measure(true_val, rng, rel_noise=0.08, lo=None):
    v = true_val * (1 + rng.normal(0, rel_noise))
    return max(lo, v) if lo is not None else v


def _sample_comorbidities(age, rng) -> dict:
    """Yasa gore hafif ayarlanmis prevalansla komorbidite ornekle."""
    out = {}
    age_factor = 1.0 + max(0, (age - 60)) * 0.012  # yasli -> biraz daha olasi
    for c, p in cc.COMORBIDITY_PREVALENCE.items():
        out[c] = bool(rng.random() < min(0.9, p * age_factor))
    return out


def generate_patient(scenario: str, rng: np.random.Generator):
    age = int(rng.integers(35, 82))
    sex = rng.choice(["M", "F"])
    weight = max(45.0, float(rng.normal(72, 12)))
    height = float(rng.normal(170, 9))
    ecog = int(np.clip(rng.integers(0, 4) - (1 if age < 55 else 0), 0, 4))
    cancer = str(rng.choice(CANCER_TYPES))
    treatments, weights = SCENARIO_TREATMENTS[scenario]
    treatment = str(rng.choice(treatments, p=weights))
    comorb = _sample_comorbidities(age, rng)

    # senaryo-ici 3 kademeli siddet -> dagilim dengesi + Red sinifini besler.
    # mild: subklinik, severe: agir vaka (latent + trajektori birlikte yukselir).
    tier_probs = SCENARIO_TIER_PROBS.get(scenario, _DEFAULT_TIER)
    tier = str(rng.choice(["mild", "normal", "severe"], p=tier_probs))
    sev_scale = {"mild": 0.45, "normal": 1.0, "severe": 1.55}[tier]
    traj_scale = {"mild": 0.5, "normal": 1.0, "severe": 1.7}[tier]

    # --- baseline lab: hematolojik kanserlerde sitopeniler daha derin ---
    if cancer in HEMATOLOGIC:
        lab = {
            "BaselineANC": float(rng.normal(1800, 800)),
            "BaselineWBC": float(rng.normal(4200, 1800)),
            "BaselinePlatelet": float(rng.normal(150, 60)),
            "BaselineHemoglobin": float(rng.normal(10.5, 1.6)),
        }
    else:
        lab = {
            "BaselineANC": float(rng.normal(3500, 900)),
            "BaselineWBC": float(rng.normal(6500, 1500)),
            "BaselinePlatelet": float(rng.normal(230, 60)),
            "BaselineHemoglobin": float(rng.normal(13, 1.3)),
        }
    lab.update({
        "BaselineCRP": float(abs(rng.normal(4, 3))),
        "BaselineAlbumin": float(rng.normal(4.0, 0.3)),
        "BaselineCreatinine": float(rng.normal(0.9, 0.15)),
        "BaselineAST": float(rng.normal(25, 8)),
        "BaselineALT": float(rng.normal(25, 8)),
        "BaselineBilirubin": float(rng.normal(0.7, 0.2)),
        "BaselineTSH": float(rng.normal(2.0, 0.7)),
        "BaselineFreeT4": float(rng.normal(1.2, 0.2)),
    })

    sev = {k: 0.0 for k in LABEL_COLS}
    n_days = int(rng.integers(7, 15))
    prev_neutropenia = bool(rng.random() < 0.2)
    prev_delay = False
    prev_sev_tox = bool(rng.random() < 0.15)
    dose_reduction = bool(prev_sev_tox and rng.random() < 0.6) or bool(rng.random() < 0.08)
    gcsf = bool(rng.random() < 0.25)
    cycle_number = int(rng.integers(1, 9))
    missing_rate = 0.08
    traj = dict(protein=0.0, water=0.0, fatigue=0.0, weight=0.0, fever_days=0,
                vomit=0, diarrhea=0, rash=0.0, dyspnea=0.0, activity=0.0)

    # --- senaryo latent + trajektori (v1 ile ayni iskelet) ---
    if scenario == "StablePatient":
        pass
    elif scenario == "InfectionDeveloping":
        lab["BaselineANC"] = float(rng.normal(1300, 300))
        lab["BaselineCRP"] = float(rng.normal(35, 15))
        traj["fever_days"] = int(rng.integers(1, 3)); traj["fatigue"] = 0.35
        sev["InfectionRisk"] = rng.uniform(1.6, 2.6)
        sev["FebrileNeutropeniaRisk"] = rng.uniform(0.8, 1.8)
    elif scenario == "FebrileNeutropeniaWarning":
        lab["BaselineANC"] = float(rng.normal(700, 200))
        lab["BaselineCRP"] = float(rng.normal(55, 20))
        traj["fever_days"] = int(rng.integers(2, 5)); traj["fatigue"] = 0.4
        prev_neutropenia = True
        sev["FebrileNeutropeniaRisk"] = rng.uniform(2.4, 3.0)
        sev["InfectionRisk"] = rng.uniform(2.2, 3.0)
        sev["TreatmentDelayRisk"] = rng.uniform(1.8, 2.8)
    elif scenario == "NutritionDecline":
        lab["BaselineAlbumin"] = float(rng.normal(3.2, 0.25))
        traj["protein"] = rng.uniform(0.05, 0.09); traj["fatigue"] = 0.2
        sev["MalnutritionRisk"] = rng.uniform(1.8, 2.8)
    elif scenario == "CachexiaProgression":
        lab["BaselineAlbumin"] = float(rng.normal(3.0, 0.25))
        lab["BaselineCRP"] = float(rng.normal(30, 12))
        traj.update(protein=0.06, weight=rng.uniform(0.15, 0.35), activity=0.25, fatigue=0.3)
        sev["CachexiaRisk"] = rng.uniform(1.8, 2.8)
        sev["MalnutritionRisk"] = rng.uniform(1.2, 2.2)
    elif scenario == "DehydrationEpisode":
        traj.update(water=rng.uniform(0.06, 0.10),
                    vomit=int(rng.integers(1, 4)), diarrhea=int(rng.integers(1, 5)))
        sev["DehydrationRisk"] = rng.uniform(1.8, 2.8)
    elif scenario == "RenalToxicitySignal":
        lab["BaselineCreatinine"] = float(rng.normal(1.9, 0.4)); traj["water"] = 0.05
        sev["RenalToxicityRisk"] = rng.uniform(1.8, 2.8)
    elif scenario == "HepaticToxicitySignal":
        fold = rng.uniform(3.5, 8.0)
        lab["BaselineAST"] = cc.REFERENCE["AST"]["uln"] * fold
        lab["BaselineALT"] = cc.REFERENCE["ALT"]["uln"] * fold * rng.uniform(0.8, 1.2)
        lab["BaselineBilirubin"] = float(rng.normal(1.6, 0.5))
        sev["HepaticToxicityRisk"] = rng.uniform(1.8, 2.9)
    elif scenario == "ImmunotherapyAdverseEvent":
        lab["BaselineTSH"] = float(rng.normal(7.0, 2.5))
        traj.update(diarrhea=int(rng.integers(1, 3)),
                    rash=rng.uniform(0.15, 0.35), dyspnea=rng.uniform(0.10, 0.30))
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
        missing_rate = rng.uniform(0.55, 0.8); traj["fatigue"] = 0.2
        sev["OverallClinicalDeteriorationRisk"] = rng.uniform(0.8, 1.6)

    # --- siddet kademesini trajektoriye uygula (feature'lar da Red'i desteklesin) ---
    for tk in ("protein", "water", "fatigue", "weight", "rash", "dyspnea", "activity"):
        traj[tk] *= traj_scale
    for tk in ("fever_days", "vomit", "diarrhea"):
        traj[tk] = int(round(traj[tk] * traj_scale))

    # --- KOMORBIDITE lab kaymalari ---
    for c, on in comorb.items():
        if on:
            for lab_key, mult in cc.COMORBIDITY_LAB_SHIFTS.get(c, {}).items():
                lab[lab_key] = lab.get(lab_key, 0) * mult

    # --- LAB CLAMP: negatif/imkansiz degerleri klinik tabanda kes ---
    cc.clamp_labs(lab)

    # --- KOMORBIDITE risk modifierlari (treatment-gated) ---
    for c, on in comorb.items():
        if not on:
            continue
        for risk, bump in cc.COMORBIDITY_RISK_MODIFIERS[c].items():
            # COPD->irAE sadece immuno baglaminda anlamli (pnomonit)
            if c == "HasCOPD" and risk == "ImmunotherapyAdverseEventRisk" \
                    and treatment not in IMMUNO_CONTEXT:
                continue
            sev[risk] = sev.get(risk, 0.0) + bump

    # --- PROGNOSTIK modifier (Age x ECOG x Albumin) ---
    pb = cc.prognostic_burden(age, ecog, lab["BaselineAlbumin"])
    sev["OverallClinicalDeteriorationRisk"] += 0.5 * pb
    sev["TreatmentDelayRisk"] += 0.4 * pb
    sev["MalnutritionRisk"] += 0.3 * pb

    # --- TOLERANCE etkileri ---
    if gcsf:  # G-CSF FN/enfeksiyon riskini DUSURUR
        sev["FebrileNeutropeniaRisk"] = max(0.0, sev["FebrileNeutropeniaRisk"] - 0.8)
        sev["InfectionRisk"] = max(0.0, sev["InfectionRisk"] - 0.4)
    if prev_sev_tox:
        sev["TreatmentDelayRisk"] += 0.5
    if dose_reduction:
        sev["TreatmentDelayRisk"] += 0.3

    # --- siddet kademesi olceklemesi + ust sinir ---
    for k in sev:
        sev[k] = min(3.2, sev[k] * sev_scale)

    # --- gunluk loglar ---
    p_t = cc.protein_target_g(weight); c_t = cc.calorie_target_kcal(weight)
    w_t = cc.water_target_ml(weight)
    base_fatigue = rng.uniform(0.5, 2.0); base_appetite = rng.uniform(3.0, 4.0)
    base_activity = rng.uniform(2.5, 4.0)
    spo2_base = 95 if comorb["HasCOPD"] else 98   # COPD -> SpO2 baslangici dusuk
    fever_idx = set(rng.choice(range(n_days),
                    size=min(traj["fever_days"], n_days), replace=False)) \
        if traj["fever_days"] else set()

    logs = []
    cur_w = weight
    for d in range(n_days):
        if rng.random() < missing_rate:
            logs.append({}); continue
        pr = max(0.1, 1.0 - traj["protein"] * d + rng.normal(0, 0.07))
        wr = max(0.1, 1.0 - traj["water"] * d + rng.normal(0, 0.07))
        cur_w -= traj["weight"]
        fatigue = base_fatigue + traj["fatigue"] * d + rng.normal(0, 0.3)
        temp = rng.uniform(38.3, 39.2) if d in fever_idx else 36.8 + rng.normal(0, 0.2)
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
            "Confusion": 0.0, "BleedingBruising": 0.0,
            "ProteinIntake": round(_measure(pr * p_t, rng, lo=0), 1),
            "CalorieIntake": round(_measure(pr * c_t, rng, lo=0), 1),
            "WaterIntakeMl": round(_measure(wr * w_t, rng, lo=0), 1),
            "AppetiteScore": float(np.clip(base_appetite - traj["protein"] * d * 8
                                           + rng.normal(0, 0.4), 0, 4)),
            "MealCompletionRatio": float(np.clip(pr + rng.normal(0, 0.08), 0, 1)),
            "WeightKg": round(max(30.0, cur_w + rng.normal(0, 0.2)), 1),
            "MedicationTaken": int(rng.random() > 0.1),
            "MissedDoseCount": int(rng.random() < 0.12),
            "OxygenSaturation": int(np.clip(spo2_base - traj["dyspnea"] * d * 2
                                            + rng.normal(0, 1), 84, 100)),
            "ActivityLevel": float(np.clip(base_activity - traj["activity"] * d
                                           + rng.normal(0, 0.3), 0, 5)),
            "SleepHours": float(np.clip(rng.normal(7, 1), 3, 10)),
        })

    patient = {
        "Age": age, "Gender": sex, "WeightKg": round(weight, 1),
        "HeightCm": round(height, 1), "CancerType": cancer, "Stage": int(rng.integers(1, 5)),
        "TreatmentType": treatment, "ECOG": ecog, "CycleDay": int(rng.integers(5, 14)),
        "CycleNumber": cycle_number, "PreviousNeutropenia": prev_neutropenia,
        "PreviousTreatmentDelay": prev_delay, "PreviousSevereToxicity": prev_sev_tox,
        "DoseReductionFlag": dose_reduction, "GCSFUseFlag": gcsf,
    }
    patient.update(comorb)
    return patient, lab, logs, sev, pb


def make_labels(features, latent_sev, prognostic, treatment, rng) -> dict[str, int]:
    # irAE cue'su: gozlenebilir severe sinyaller (pnomonit/kolit/hepatit/dokuntu)
    spo2 = features.get("MinSpO2_3")
    irae_cue = 0.0
    if spo2 is not None:
        irae_cue += 3.0 if spo2 < 90 else (2.0 if spo2 < 92 else (1.0 if spo2 < 94 else 0))
    irae_cue += min(2.0, max(0.0, features.get("DyspneaTrend7", 0) or 0) * 6)
    irae_cue += min(1.5, max(0.0, features.get("DiarrheaTrend7", 0) or 0) * 4)
    irae_cue += min(1.5, max(0.0, features.get("SkinRashTrend7", 0) or 0) * 4)

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
        "ImmunotherapyAdverseEventRisk": min(3.0, irae_cue),
        "TreatmentDelayRisk": max(features.get("ANCGrade", 0), features.get("PlateletGrade", 0)),
    }
    labels = {}
    for k in LABEL_COLS:
        if k == "OverallClinicalDeteriorationRisk":
            continue
        sev = 0.7 * latent_sev.get(k, 0.0) + 0.3 * min(cues.get(k, 0.0), 3.0)
        labels[k] = _sev_to_level(sev, rng)

    # irAE klinik kapi: immunoterapi baglamı yoksa irAE olamaz
    if treatment not in IMMUNO_CONTEXT:
        labels["ImmunotherapyAdverseEventRisk"] = 0

    # === YUMUSATILMIS OVERALL (korroborasyon kurali) ===
    levels = list(labels.values())
    red = sum(l == 3 for l in levels)
    orange = sum(l == 2 for l in levels)
    yellow = sum(l == 1 for l in levels)
    strong_support = (yellow >= 2) or (prognostic >= 1.2)
    if red >= 1:
        ov = 3
    elif orange >= 2:
        ov = 2
    elif orange == 1:
        ov = 2 if strong_support else 1
    elif yellow >= 2:
        ov = 1
    else:
        ov = 0
    # latent overall (ornn missing-data senaryosu) ile harmanla + kucuk gurultu
    ov = max(ov, _sev_to_level(latent_sev.get("OverallClinicalDeteriorationRisk", 0),
                               rng, noise=0.15))
    labels["OverallClinicalDeteriorationRisk"] = ov
    return labels


def generate_dataset(n_per_scenario: int = 200, seed: int = 42):
    import pandas as pd
    rng = np.random.default_rng(seed)
    rows = []
    for scenario in SCENARIOS:
        for _ in range(n_per_scenario):
            patient, lab, logs, latent, pb = generate_patient(scenario, rng)
            feats = build_features(patient, lab, logs)
            labels = make_labels(feats, latent, pb, patient["TreatmentType"], rng)
            row = {k: feats.get(k) for k in FEATURE_ORDER}
            row.update({f"label_{k}": v for k, v in labels.items()})
            row["scenario"] = scenario
            row["CancerType"] = patient["CancerType"]
            row["TreatmentType"] = patient["TreatmentType"]
            rows.append(row)
    return pd.DataFrame(rows)


def _scenario_counts(n_total: int) -> dict:
    """SCENARIO_WEIGHTS'e gore n_total'i senaryolara dagitir."""
    total_w = sum(SCENARIO_WEIGHTS[s] for s in SCENARIOS)
    counts = {s: int(round(n_total * SCENARIO_WEIGHTS[s] / total_w)) for s in SCENARIOS}
    return counts


def generate_balanced_dataset(n_total: int = 15000, seed: int = 42):
    """v2.1 dengelenmis uretim: senaryo paylari (SCENARIO_WEIGHTS) + senaryo-bazli
    severe orani (SCENARIO_TIER_PROBS) ile nadir Red sinifini guclendirir."""
    import pandas as pd
    rng = np.random.default_rng(seed)
    counts = _scenario_counts(n_total)
    rows = []
    for scenario in SCENARIOS:
        for _ in range(counts[scenario]):
            patient, lab, logs, latent, pb = generate_patient(scenario, rng)
            feats = build_features(patient, lab, logs)
            labels = make_labels(feats, latent, pb, patient["TreatmentType"], rng)
            row = {k: feats.get(k) for k in FEATURE_ORDER}
            row.update({f"label_{k}": v for k, v in labels.items()})
            row["scenario"] = scenario
            row["CancerType"] = patient["CancerType"]
            row["TreatmentType"] = patient["TreatmentType"]
            rows.append(row)
    return pd.DataFrame(rows)


if __name__ == "__main__":
    df = generate_dataset(n_per_scenario=50, seed=1)
    print("v2 hizli kontrol:", df.shape)
    print(df["label_OverallClinicalDeteriorationRisk"].value_counts().sort_index())
