# Model Card — OncoGuard-AI Risk Models (v2.1)

## Overview

Ten independent **XGBoost** multi-class classifiers, one per oncology deterioration risk. Each predicts a 4-level ordinal severity: **0 Green, 1 Yellow, 2 Orange, 3 Red**.

| | |
|---|---|
| **Model family** | XGBoost (gradient-boosted trees) |
| **Version** | v2.1-balanced-15k |
| **Trained** | 2026-06-04 |
| **Input features** | 87 (after one-hot encoding) |
| **Dataset** | Synthetic v2.1 — 14,998 rows (11,998 train / 3,000 test) |
| **Class balancing** | scenario weights + per-scenario severe ratio; `sample_weight=balanced` |

## Inputs

87 engineered features (see `dataset/DATASET_CARD.md`): patient/treatment context, baseline labs, CTCAE toxicity grades, rolling symptom/nutrition/hydration trends, comorbidity flags, plus one-hot `CancerType` and `TreatmentType`. Missing values are imputed with **train-only medians** (stored in `models/preprocessor_metadata.json`).

> `LoggedDays3/7/30` and `NDaysLogged` are **not** model features — they only drive the monitoring-window gating in the serving layer.

## Performance (synthetic test set, n = 3000)

| Risk | Accuracy | Macro-F1 | Red Precision | Red Recall | Red F1 |
|------|:--------:|:--------:|:-------------:|:----------:|:------:|
| InfectionRisk | 0.803 | 0.697 | 0.662 | 0.653 | 0.658 |
| FebrileNeutropeniaRisk | 0.935 | 0.745 | 0.811 | 0.889 | 0.848 |
| MalnutritionRisk | 0.874 | 0.747 | 0.741 | 0.769 | 0.755 |
| CachexiaRisk | 0.946 | 0.705 | 0.580 | 0.607 | 0.594 |
| DehydrationRisk | 0.885 | 0.752 | 0.765 | 0.818 | 0.791 |
| RenalToxicityRisk | 0.890 | 0.704 | 0.701 | 0.736 | 0.718 |
| HepaticToxicityRisk | 0.924 | 0.673 | 0.640 | 0.848 | 0.729 |
| ImmunotherapyAdverseEventRisk | 0.963 | 0.736 | 0.786 | 0.872 | 0.827 |
| TreatmentDelayRisk | 0.795 | 0.697 | 0.722 | 0.848 | 0.780 |
| OverallClinicalDeteriorationRisk | 0.657 | 0.643 | 0.777 | 0.752 | 0.764 |

"Red recall" (catching the most severe cases) is prioritised, since missing a true-Red event is the most clinically costly error.

## Serving

Exposed via FastAPI (`ai-service/main.py`):
- `POST /predict-risk` — caller supplies the 87-feature vector.
- `POST /predict-from-raw` — caller supplies raw patient/lab/daily-log data; features are built server-side with the same `feature_engineering` code used at training time.

Risks below their minimum observation window (3/7/30 calendar days) return `status: "monitoring"` instead of a prediction.

## Intended use

Decision **support** / early warning to surface rising risk for clinician review. Educational and research demonstration of ML-in-oncology.

## Limitations & responsible-use notes

- **Trained and evaluated on synthetic data only.** Reported metrics do **not** represent validated real-world clinical accuracy and should not be interpreted as such.
- Model `predict_proba` outputs are reported as "confidence" **without probability calibration** — treat as relative, not absolute, certainty.
- `CancerType` covers 7 categories at training time; cancer types outside this set map to an all-zero one-hot (treated as "other").
- 30-day features derive from short (7–14 day) synthetic trajectories.
- **Not a medical device. Not for diagnosis or treatment.** A licensed clinician must make all decisions.

## Citation

```
Özbeyler, H. S. (2026). OncoGuard-AI Risk Models (v2.1).
Senior capstone project, İstanbul Arel University, Department of Computer Engineering.
```
