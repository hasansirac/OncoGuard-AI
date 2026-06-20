# Dataset Card — OncoGuard-AI Synthetic Oncology Dataset (v2.1)

## Summary

A **fully synthetic** dataset simulating oncology patients on active treatment, designed to train and evaluate early-warning models for ten treatment-related deterioration risks. **No real patient data is used.** The data is generated deterministically, so the exact CSV can be reproduced byte-for-byte.

| | |
|---|---|
| **Name** | `oncoguard_dataset_v21_main.csv` |
| **Version** | v2.1 (balanced, 15k) |
| **Rows** | 14,998 |
| **Columns** | 87 → 74 features + 3 categorical/metadata + 10 labels |
| **Seed** | 42 |
| **License** | Data: **CC BY 4.0** · Code/generator: MIT |
| **CSV SHA-256** | `c1cfe9acbaf0e377a2a9dfe034f210857f8bda1964e5d6749c1df7b38de78300` |
| **Real patient data** | None — 100% synthetic |

## Provenance & reproducibility

The dataset is produced by a deterministic generator. From the `dataset/` folder:

```bash
python3 -c "from oncoguard_ai.data.synthetic_data_v2 import generate_balanced_dataset; \
df = generate_balanced_dataset(n_total=15000, seed=42); print(df.shape)"
# -> (14998, 87)
```

Generation chain:
`synthetic_data_v2.generate_balanced_dataset()` → `feature_engineering.build_features()` → `clinical_constants` (CTCAE grades, ESPEN targets, comorbidity tables, lab clamping).

Each simulated patient contributes **7–14 days** of daily logs; trajectories are drawn from clinical scenarios and balanced via per-scenario severity weighting.

## Column groups

- **Patient / treatment context (numeric & flags):** `Age`, `ECOG`, `CycleDay`, `CycleNumber`, `BMI`, treatment-history flags (`PreviousNeutropenia`, `DoseReductionFlag`, `GCSFUseFlag`, …), comorbidity flags (`HasDiabetes`, `HasHypertension`, …, `ComorbidityCount`).
- **Baseline labs:** `BaselineANC`, `BaselineWBC`, `BaselineCRP`, `BaselineAlbumin`, `BaselineCreatinine`, `BaselineAST/ALT`, `BaselinePlatelet`, `BaselineHemoglobin`, `BaselineBilirubin`, `BaselineTSH`, `BaselineFreeT4`.
- **CTCAE toxicity grades:** `ANCGrade`, `PlateletGrade`, `HemoglobinGrade`, `AST/ALT/BilirubinGrade`, `CreatinineGrade`, `AlbuminGrade`.
- **Rolling symptom / nutrition / hydration trends:** protein & calorie ratios, fever counts, fatigue/appetite deltas, fluid-loss scores, SpO₂ minima, medication adherence, missing-data scores, trend directions, etc.
- **Categorical / metadata (3):** `scenario` (generation provenance — **excluded from model input to prevent leakage**), `CancerType`, `TreatmentType`.
- **Labels (10):** `label_InfectionRisk`, `label_FebrileNeutropeniaRisk`, `label_MalnutritionRisk`, `label_CachexiaRisk`, `label_DehydrationRisk`, `label_RenalToxicityRisk`, `label_HepaticToxicityRisk`, `label_ImmunotherapyAdverseEventRisk`, `label_TreatmentDelayRisk`, `label_OverallClinicalDeteriorationRisk`. Each label is ordinal **0 = Green, 1 = Yellow, 2 = Orange, 3 = Red**.

> The 74 raw features become **87 model features** after one-hot encoding `CancerType` (7 types) and `TreatmentType` (6 types) during preprocessing.

## Label distribution (selected)

| Label | Green (0) | Yellow (1) | Orange (2) | Red (3) |
|-------|:---------:|:----------:|:----------:|:-------:|
| InfectionRisk | 9,765 | 2,777 | 1,690 | 766 |
| FebrileNeutropeniaRisk | 12,754 | 862 | 664 | 718 |
| OverallClinicalDeteriorationRisk | 1,875 | 4,212 | 2,895 | 6,016 |

Several risks are intentionally imbalanced (rare severe events), reflecting clinical reality; the generator applies balancing weights to keep severe-class recall trainable.

## Intended use

Training and benchmarking multi-class oncology risk classifiers; reproducibility studies; educational ML-in-healthcare demonstrations.

## Limitations & ethical notes

- **Synthetic only.** Distributions are clinically informed but not validated against real cohorts. Performance here does **not** transfer to real patients.
- Generated trajectories span 7–14 days, so 30-day features (e.g. `WeightLossPct30`) are populated from short windows — keep this in mind for cachexia modeling.
- Not for clinical, diagnostic or treatment use.

## Citation

```
Özbeyler, H. S. (2026). OncoGuard-AI Synthetic Oncology Dataset (v2.1) [Data set].
Senior capstone project, İstanbul Arel University, Department of Computer Engineering.
```

**Licensing:** the dataset is released under **CC BY 4.0**; the generator code under **MIT**. The CSV integrity can be verified with the SHA-256 above.
