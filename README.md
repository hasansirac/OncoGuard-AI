<div align="center">

# 🛡️ OncoGuard-AI

### Adaptive Oncology Early-Warning & Clinical Decision-Support System

*An AI-assisted platform that monitors cancer patients between clinic visits and predicts ten treatment-related deterioration risks before they become emergencies.*

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
![Status](https://img.shields.io/badge/status-research%20prototype-orange)
![Models](https://img.shields.io/badge/models-10%20XGBoost-green)
![Dataset](https://img.shields.io/badge/dataset-synthetic%20v2.1-lightgrey)
[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.20774319.svg)](https://doi.org/10.5281/zenodo.20774319)

</div>

> ⚠️ **Research & educational prototype.** OncoGuard-AI is an undergraduate senior capstone project. It is **not** a certified medical device and must **not** be used for real clinical decisions. All data is **fully synthetic** — no real patient information is involved.
>
> 🔒 **Not production-hardened.** Intended for local demonstration and academic evaluation only. Auth coverage, transport security and input sanitisation are incomplete by design (see *Known limitations*); do not expose to the public internet.

---

## 📌 What it does

Patients undergoing chemotherapy, radiotherapy or immunotherapy face risks (febrile neutropenia, infection, dehydration, malnutrition, organ toxicity…) that often develop *between* hospital visits. OncoGuard-AI lets patients log daily symptoms, nutrition and hydration from a mobile app, combines that with baseline lab values entered by their doctor, and runs ten machine-learning models to flag rising risk early — giving clinicians a colour-coded (🟢 Green / 🟡 Yellow / 🟠 Orange / 🔴 Red) early-warning dashboard.

### Ten monitored risks

| # | Risk | Window |
|---|------|--------|
| 1 | Infection | 3-day |
| 2 | Febrile Neutropenia | 3-day |
| 3 | Dehydration | 3-day |
| 4 | Overall Clinical Deterioration | 3-day |
| 5 | Malnutrition | 7-day |
| 6 | Renal Toxicity | 7-day |
| 7 | Hepatic Toxicity | 7-day |
| 8 | Treatment Delay | 7-day |
| 9 | Immunotherapy Adverse Event | 7-day |
| 10 | Cachexia | 30-day |

Each risk has its own minimum observation window. Until enough calendar days of data exist, that risk is reported as **`monitoring`** rather than guessed.

---

## 🏗️ Architecture

```
┌────────────────────┐      ┌──────────────────────────┐      ┌─────────────────────┐
│  Android Patient   │      │   .NET Backend (Web API)  │      │  AI Service          │
│  (Kotlin Compose)  │─────▶│   Clean Architecture       │─────▶│  (FastAPI, Python)   │
│  daily symptom/    │ REST │   EF Core + MSSQL          │ HTTP │  10 × XGBoost models │
│  nutrition logging │◀─────│   JWT auth, Lab-Cycle engine│◀─────│  /predict-from-raw   │
└────────────────────┘      └──────────────┬─────────────┘      └─────────────────────┘
                                           │ REST
                                  ┌────────▼─────────┐
                                  │ Doctor Dashboard  │
                                  │ (HTML/JS)         │
                                  │ labs, profiles,   │
                                  │ risk cards        │
                                  └───────────────────┘
```

| Folder | Component | Stack |
|--------|-----------|-------|
| [`backend/`](backend) | REST API, business logic, persistence | .NET 8, EF Core, MSSQL, JWT |
| [`ai-service/`](ai-service) | Risk-prediction microservice | Python, FastAPI, XGBoost, scikit-learn |
| [`android-patient/`](android-patient) | Patient mobile app | Kotlin, Jetpack Compose, Retrofit |
| [`doctor-dashboard/`](doctor-dashboard) | Clinician web dashboard | HTML, vanilla JS |
| [`dataset/`](dataset) | Synthetic dataset + deterministic generator | Python |
| [`training/`](training) | Model training notebook | Jupyter, XGBoost |

The backend bridges to the AI service through an **integration adapter** (`ai-service/oncoguard_ai/integration/adapter.py`) that handles the key cross-layer mappings: field-name differences, ANC/WBC unit conversion (×10³/µL → /µL), cancer/treatment enum mapping, risk-level encoding (backend 1–4 ↔ AI 0–3) and same-day log de-duplication. (Known gap: cancer types outside the model's seven trained categories — e.g. `Prostate`, `Other` — map to an all-zero one-hot.)

---

## 🤖 The AI pipeline

- **10 separate XGBoost classifiers**, one per risk, each predicting a 4-level severity (Green/Yellow/Orange/Red).
- **87 engineered features**: baseline labs, CTCAE toxicity grades, rolling symptom/nutrition/hydration trends, comorbidity flags, treatment history, and one-hot cancer/treatment encodings.
- **Clinically grounded** in CTCAE v5 (toxicity grading), IDSA (febrile neutropenia), ESPEN (nutrition targets: protein 1.3 g/kg, energy 27.5 kcal/kg, fluid 30 ml/kg) and Fearon 2011 (cachexia).
- **Calendar-aware monitoring**: 3-, 7- and 30-day windows are computed from real `LogDate` values, not row counts.

### Model performance (synthetic test set, n = 3000)

| Risk | Accuracy | Macro-F1 | Red Precision | Red Recall | Red F1 |
|------|:--------:|:--------:|:-------------:|:----------:|:------:|
| Febrile Neutropenia | 0.935 | 0.745 | 0.811 | **0.889** | 0.848 |
| Immunotherapy AE | 0.963 | 0.736 | 0.786 | 0.872 | 0.827 |
| Dehydration | 0.885 | 0.752 | 0.765 | 0.818 | 0.791 |
| Treatment Delay | 0.795 | 0.697 | 0.722 | 0.848 | 0.780 |
| Malnutrition | 0.874 | 0.747 | 0.741 | 0.769 | 0.755 |
| Overall Deterioration | 0.657 | 0.643 | 0.777 | 0.752 | 0.764 |
| Hepatic Toxicity | 0.924 | 0.673 | 0.640 | 0.848 | 0.729 |
| Renal Toxicity | 0.890 | 0.704 | 0.701 | 0.736 | 0.718 |
| Infection | 0.803 | 0.697 | 0.662 | 0.653 | 0.658 |
| Cachexia | 0.946 | 0.705 | 0.580 | 0.607 | 0.594 |

> These are results on a **synthetic** test set and do not represent validated real-world clinical performance. See [`docs/MODEL_CARD.md`](docs/MODEL_CARD.md) for full details and limitations.

---

## 🧬 Dataset

A fully synthetic, deterministically generated dataset (**v2.1**, 14,998 rows × 87 total columns: 74 engineered features, 3 categorical/metadata columns, and 10 labels). The exact CSV is byte-for-byte reproducible with `seed=42`. See [`dataset/`](dataset) and [`dataset/DATASET_CARD.md`](dataset/DATASET_CARD.md).

📦 **Archived on Zenodo (CC BY 4.0):** [`10.5281/zenodo.20774319`](https://doi.org/10.5281/zenodo.20774319) · CSV SHA-256 `c1cfe9acbaf0e377a2a9dfe034f210857f8bda1964e5d6749c1df7b38de78300`

```bash
cd dataset
python3 -c "from oncoguard_ai.data.synthetic_data_v2 import generate_balanced_dataset; \
print(generate_balanced_dataset(n_total=15000, seed=42).shape)"
# -> (14998, 87)
```

---

## 🚀 Quick start

### 1. AI service (FastAPI)
```bash
cd ai-service
python3 -m venv .venv && source .venv/bin/activate    # Windows: .venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --host 127.0.0.1 --port 8000
# docs at http://127.0.0.1:8000/docs
```

### 2. Backend (.NET)
```bash
cd backend
# copy the example config and fill in your own JWT key + connection string
cp OncoGuard.WebAPI/appsettings.example.json OncoGuard.WebAPI/appsettings.Development.json
dotnet ef database update --project OncoGuard.Infrastructure --startup-project OncoGuard.WebAPI
dotnet run --project OncoGuard.WebAPI
# API on http://localhost:5080 ; Swagger in Development
```
> **Config note:** set a real `Jwt:Key` (≥ 32 chars). `appsettings.json` ships with a placeholder only. The AI service URL is set via `AiService:BaseUrl` (default `http://127.0.0.1:8000`).

### 3. Doctor dashboard
Open `doctor-dashboard/index.html` in a browser. Set the `API_BASE` constant at the top of the file to match your running backend (e.g. `http://localhost:5080`).

### 4. Android patient app
Open `android-patient/` in Android Studio. Base URL is in `app/.../ApiConfig.kt` — emulator uses `http://10.0.2.2:5080/`, a physical phone uses your machine's LAN IP.

---

## 🩺 Demo flow

1. **Doctor** registers/logs in, then enters the patient's **baseline labs** and **clinical/treatment history**. The patient cannot submit data until this exists.
2. **Patient** logs in and records **daily** symptoms, nutrition, hydration and medication. Past dates can be back-filled and updated.
3. Once enough calendar days accumulate, the **3 / 7 / 30-day** windows fill. The doctor presses **Run Risk Assessment** on the dashboard, which calls the AI service and produces risk levels; risks whose window is not yet full are shown as **`monitoring`**.
4. The **doctor dashboard** shows colour-coded risk cards per patient.

---

## ⚠️ Known limitations

This is a research prototype; several limitations are intentional and documented for transparency:

- **Cachexia rarely completes in a standard cycle.** Lab cycles open as 21- (or 28-) day monitoring windows, but the Cachexia model requires 30 logged calendar days. In a default 21-day cycle the 30-day window never fills, so Cachexia typically stays in `monitoring`.
- **Risk assessment is manually triggered.** The doctor runs it via the **Run Risk Assessment** button; it is not yet auto-triggered after each daily log.
- **Rule engine and ML models are not yet fused.** They run as separate paths; the rule-over-model override exists in code but is not wired into the AI evaluation path.
- **`predict_proba` is reported as "confidence" without calibration** — treat as relative, not absolute.
- **Cancer types are limited to the 7 trained categories**; others map to an all-zero one-hot ("other").
- **Synthetic data only** — reported metrics do not represent validated real-world clinical performance.

---

## 👤 Author

**Hasan Siraç Özbeyler** — Computer Engineering, İstanbul Arel University
Senior capstone thesis · Supervisor: Prof. Dr. Halûk Gümüşkaya

## 📄 License

Code: [MIT](LICENSE). Dataset: **CC BY 4.0** (see [`dataset/LICENSE-DATA`](dataset/LICENSE-DATA)). Research/educational use — not a medical device.
