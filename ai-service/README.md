# ONCOGUARD-AI Risk Prediction Service

## Overview
This service provides machine-learning–based risk prediction for oncology patients
who are monitored between two laboratory controls. It loads ten trained XGBoost
models and predicts the following risks, each on a four-level severity scale
(Green, Yellow, Orange, Red):

- Infection Risk
- Febrile Neutropenia Risk
- Malnutrition Risk
- Cachexia Risk
- Dehydration Risk
- Renal Toxicity Risk
- Hepatic Toxicity Risk
- Immunotherapy Adverse Event Risk
- Treatment Delay Risk
- Overall Clinical Deterioration Risk

The service is a standalone Python (FastAPI) application. The .NET backend
communicates with it over HTTP.

## Model Package
The service uses a self-contained model package located in the `models/` folder:

- 10 trained XGBoost models (`*_xgb.joblib`)
- `model_manifest.json` — provenance, version and per-risk metadata
- `feature_columns.json` — canonical 87-feature order
- `preprocessor_metadata.json` — training medians, one-hot sources, risk-level mapping
- `final_xgb_model_results.csv` — recorded evaluation metrics

**Dataset / model version:** `v2.1-balanced-15k`
(14,998 records; 11,998 train / 3,000 test; 87 features)

## Project Structure
```
ONCOGUARD_AI_SERVICE/
├── main.py
├── requirements.txt
├── README.md
└── models/
    ├── infectionrisk_xgb.joblib
    ├── febrileneutropeniarisk_xgb.joblib
    ├── malnutritionrisk_xgb.joblib
    ├── cachexiarisk_xgb.joblib
    ├── dehydrationrisk_xgb.joblib
    ├── renaltoxicityrisk_xgb.joblib
    ├── hepatictoxicityrisk_xgb.joblib
    ├── immunotherapyadverseeventrisk_xgb.joblib
    ├── treatmentdelayrisk_xgb.joblib
    ├── overallclinicaldeteriorationrisk_xgb.joblib
    ├── model_manifest.json
    ├── feature_columns.json
    ├── preprocessor_metadata.json
    └── final_xgb_model_results.csv
```

## Installation
```bash
pip install -r requirements.txt
```

## Run
```bash
uvicorn main:app --reload --port 8000
```

- Swagger UI: http://127.0.0.1:8000/docs
- Health check: http://127.0.0.1:8000/health

## API Endpoints

### `GET /health`
Returns service status and confirms that all ten models and 87 features are loaded.

### `POST /predict-risk`
Accepts a raw feature payload and returns a severity level for each of the ten
risks. Any subset of the 87 features may be supplied; missing features are
imputed with the training-set medians, and the columns are ordered to the
canonical schema before prediction.

**Example request**
```json
{
  "features": {
    "Age": 62,
    "BaselineANC": 800,
    "FeverCount7": 4,
    "WeightLossPct30": 8.4
  }
}
```

**Example response (abridged)**
```json
{
  "version": "v2.1-balanced-15k",
  "predictions": {
    "InfectionRisk": {
      "risk": "InfectionRisk",
      "level_ai": 3,
      "level_backend": 4,
      "label": "Red",
      "probabilities": { "Green": 0.05, "Yellow": 0.10, "Orange": 0.20, "Red": 0.65 }
    }
  }
}
```

`level_ai` uses the model encoding (0–3); `level_backend` uses the backend
encoding (1–4); `label` is the human-readable severity.

## Notes
- The `xgboost` version in `requirements.txt` must match the version used for
  training (`3.2.0`) to load the `.joblib` models correctly.
- The service loads the model package once at start-up and reuses it for every
  request, keeping per-request latency low.
- This is a decision-support tool and is not a substitute for clinical judgment.
