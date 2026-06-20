"""
ONCOGUARD-AI — Risk Prediction Service (FastAPI)
=================================================
Loads the ten per-risk XGBoost models and preprocessing metadata once at
start-up, and exposes two prediction paths:

  POST /predict-risk       -> caller already has the 87 feature values
  POST /predict-from-raw   -> caller sends raw patient data

The raw path builds features using the same feature_engineering code used at
training time. Risk evaluation uses calendar-aware monitoring windows:
  - 3-day risks use the last 3 real calendar days
  - 7-day risks use the last 7 real calendar days
  - 30-day cachexia risk uses the last 30 real calendar days

Important:
  LoggedDays3, LoggedDays7 and LoggedDays30 are NOT model features.
  They are used only for monitoring-window decisions.
"""

import json
from pathlib import Path
from typing import Any, Dict, List, Optional

import joblib
import pandas as pd
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field

from oncoguard_ai.integration.adapter import (
    backend_payload_to_features,
    features_to_model_row,
)

# --------------------------------------------------------------------------- #
# Paths
# --------------------------------------------------------------------------- #
BASE_DIR = Path(__file__).resolve().parent
MODELS_DIR = BASE_DIR / "models"

MANIFEST_PATH = MODELS_DIR / "model_manifest.json"
FEATURE_COLS_PATH = MODELS_DIR / "feature_columns.json"
METADATA_PATH = MODELS_DIR / "preprocessor_metadata.json"

# --------------------------------------------------------------------------- #
# Global state
# --------------------------------------------------------------------------- #
MANIFEST: dict = {}
FEATURE_COLUMNS: list = []
MEDIAN_VALUES: Dict[str, float] = {}
RISK_LEVEL_MAPPING: dict = {}
LABELS = {0: "Green", 1: "Yellow", 2: "Orange", 3: "Red"}
MODELS: Dict[str, object] = {}
RISK_ORDER: list = []

# --------------------------------------------------------------------------- #
# Missing-data policy
# --------------------------------------------------------------------------- #
# WeightLossPct30 is intentionally NOT critical here.
# Cachexia can remain monitoring for 30 days, but this should not block
# the other 3-day and 7-day risks.
CRITICAL_FEATURES = [
    "BaselineANC",
    "BaselineWBC",
    "BaselineCreatinine",
    "BaselineAlbumin",
    "FeverCount7",
    "MaxTemp3",
    "ProteinRatioMean7",
]

MIN_PRESENT_FEATURES = 40

# --------------------------------------------------------------------------- #
# Risk-specific minimum observation days
# --------------------------------------------------------------------------- #
RISK_MIN_DAYS = {
    # Acute 3-day risks
    "InfectionRisk": 3,
    "FebrileNeutropeniaRisk": 3,
    "DehydrationRisk": 3,
    "OverallClinicalDeteriorationRisk": 3,

    # Subacute 7-day risks
    "MalnutritionRisk": 7,
    "RenalToxicityRisk": 7,
    "HepaticToxicityRisk": 7,
    "TreatmentDelayRisk": 7,
    "ImmunotherapyAdverseEventRisk": 7,

    # Chronic 30-day risk
    "CachexiaRisk": 30,
}

GLOBAL_MIN_DAYS = 3


def _load_json(path: Path) -> dict:
    with open(path, "r", encoding="utf-8") as fh:
        return json.load(fh)


def load_package() -> None:
    global MANIFEST, FEATURE_COLUMNS, MEDIAN_VALUES, RISK_LEVEL_MAPPING
    global MODELS, RISK_ORDER

    if not MODELS_DIR.exists():
        raise RuntimeError(f"models/ folder not found at {MODELS_DIR}")

    MANIFEST = _load_json(MANIFEST_PATH)

    fc = _load_json(FEATURE_COLS_PATH)
    FEATURE_COLUMNS = fc["feature_columns"] if isinstance(fc, dict) else fc

    meta = _load_json(METADATA_PATH)
    MEDIAN_VALUES = meta.get("median_values", {})
    RISK_LEVEL_MAPPING = meta.get("risk_level_mapping", {})

    MODELS = {}
    RISK_ORDER = []

    for risk_name, info in MANIFEST.get("risk_models", {}).items():
        model_file = MODELS_DIR / info["model_file"]

        if not model_file.exists():
            raise RuntimeError(f"Model file missing: {model_file}")

        MODELS[risk_name] = joblib.load(model_file)
        RISK_ORDER.append(risk_name)

    print(
        f"[OncoGuard] Loaded {len(MODELS)} models, "
        f"{len(FEATURE_COLUMNS)} features, version "
        f"{MANIFEST.get('version', 'unknown')}"
    )


def ai_to_backend_level(ai_level: int) -> int:
    return int(ai_level) + 1


# --------------------------------------------------------------------------- #
# FastAPI app
# --------------------------------------------------------------------------- #
app = FastAPI(
    title="OncoGuard-AI Risk Prediction Service",
    description=(
        "Predicts ten oncology deterioration risks. Accepts either a "
        "precomputed feature vector or raw patient data."
    ),
    version="1.2.0",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.on_event("startup")
def _startup() -> None:
    load_package()


# --------------------------------------------------------------------------- #
# Request / response models
# --------------------------------------------------------------------------- #
class PredictRequest(BaseModel):
    features: Dict[str, float] = Field(
        ...,
        examples=[
            {
                "Age": 62,
                "BaselineANC": 800,
                "FeverCount7": 4,
                "WeightLossPct30": 8.4,
            }
        ],
    )
    strict_schema: bool = Field(default=True)


class RawPredictRequest(BaseModel):
    patient: Dict[str, Any] = Field(
        ...,
        description="Profile + cancer/treatment type.",
    )
    BaselineLab: Dict[str, Any] = Field(default_factory=dict)
    DailyLogs: List[Dict[str, Any]] = Field(default_factory=list)
    wbc_anc_unit: str = Field(default="thousand")


class RiskPrediction(BaseModel):
    risk: str
    status: str = "ok"                     # "ok" | "monitoring"
    level_ai: Optional[int] = None
    level_backend: Optional[int] = None
    label: Optional[str] = None
    probabilities: Optional[Dict[str, float]] = None
    days_logged: Optional[int] = None
    days_required: Optional[int] = None
    message: Optional[str] = None


class PredictResponse(BaseModel):
    version: str
    status: str = "ok"
    predictions: Dict[str, RiskPrediction]


# --------------------------------------------------------------------------- #
# Core helpers
# --------------------------------------------------------------------------- #
def validate_schema(raw: Dict[str, float]) -> List[str]:
    return [k for k in raw.keys() if k not in FEATURE_COLUMNS]


def assess_completeness(row: Dict[str, Any]) -> Dict[str, Any]:
    present = [c for c in FEATURE_COLUMNS if row.get(c) is not None]
    missing_critical = [c for c in CRITICAL_FEATURES if row.get(c) is None]

    enough = (len(present) >= MIN_PRESENT_FEATURES) and not missing_critical

    return {
        "enough": enough,
        "present_count": len(present),
        "min_required": MIN_PRESENT_FEATURES,
        "missing_critical": missing_critical,
    }


def build_feature_frame(row: Dict[str, Any]) -> pd.DataFrame:
    out = {}

    for col in FEATURE_COLUMNS:
        v = row.get(col)
        out[col] = v if v is not None else MEDIAN_VALUES.get(col, 0.0)

    return pd.DataFrame(
        [[out[c] for c in FEATURE_COLUMNS]],
        columns=FEATURE_COLUMNS,
    )


def predict_all(
    row: Dict[str, Any],
    days_logged: Optional[int] = None,
    days_by_risk: Optional[Dict[str, int]] = None,
) -> Dict[str, RiskPrediction]:
    X = build_feature_frame(row)
    results: Dict[str, RiskPrediction] = {}

    for risk_name in RISK_ORDER:
        required = RISK_MIN_DAYS.get(risk_name, GLOBAL_MIN_DAYS)

        available_days = days_logged
        if days_by_risk is not None:
            available_days = days_by_risk.get(risk_name, days_logged)

        if available_days is not None and available_days < required:
            results[risk_name] = RiskPrediction(
                risk=risk_name,
                status="monitoring",
                days_logged=available_days,
                days_required=required,
                message=(
                    f"Monitoring: needs {required} days of data "
                    f"({available_days} so far)."
                ),
            )
            continue

        model = MODELS[risk_name]
        pred = int(model.predict(X)[0])

        probs = None
        if hasattr(model, "predict_proba"):
            p = model.predict_proba(X)[0]
            classes = getattr(model, "classes_", list(range(len(p))))

            probs = {
                LABELS.get(int(c), str(c)): round(float(v), 4)
                for c, v in zip(classes, p)
            }

        results[risk_name] = RiskPrediction(
            risk=risk_name,
            status="ok",
            level_ai=pred,
            level_backend=ai_to_backend_level(pred),
            label=LABELS.get(pred, str(pred)),
            probabilities=probs,
            days_logged=available_days,
            days_required=required,
        )

    return results


# --------------------------------------------------------------------------- #
# Endpoints
# --------------------------------------------------------------------------- #
@app.get("/", tags=["meta"])
def root():
    return {
        "service": "OncoGuard-AI Risk Prediction Service",
        "version": MANIFEST.get("version", "unknown"),
        "models_loaded": len(MODELS),
        "feature_count": len(FEATURE_COLUMNS),
        "endpoints": ["/predict-risk", "/predict-from-raw", "/health"],
        "docs": "/docs",
    }


@app.get("/health", tags=["meta"])
def health():
    ok = len(MODELS) == 10 and len(FEATURE_COLUMNS) == 87

    return {
        "status": "ok" if ok else "degraded",
        "models_loaded": len(MODELS),
        "feature_count": len(FEATURE_COLUMNS),
    }


@app.post("/predict-risk", response_model=PredictResponse, tags=["prediction"])
def predict_risk(req: PredictRequest):
    if not MODELS:
        raise HTTPException(status_code=503, detail="Models not loaded.")

    unknown = validate_schema(req.features)

    if unknown and req.strict_schema:
        raise HTTPException(
            status_code=422,
            detail={
                "error": "Unknown feature keys (schema violation).",
                "unknown_keys": unknown,
            },
        )

    try:
        preds = predict_all(dict(req.features))
    except Exception as exc:
        raise HTTPException(status_code=400, detail=f"Prediction failed: {exc}")

    return PredictResponse(
        version=MANIFEST.get("version", "unknown"),
        predictions=preds,
    )


@app.post("/predict-from-raw", response_model=PredictResponse, tags=["prediction"])
def predict_from_raw(req: RawPredictRequest):
    if not MODELS:
        raise HTTPException(status_code=503, detail="Models not loaded.")

    payload = dict(req.patient)
    payload["BaselineLab"] = req.BaselineLab
    payload["DailyLogs"] = req.DailyLogs

    try:
        features, patient, baseline_lab, daily_logs = backend_payload_to_features(
            payload,
            wbc_anc_unit=req.wbc_anc_unit,
        )

        cancer_ai = patient.get("CancerType")
        treatment_ai = patient.get("TreatmentType")

        row = features_to_model_row(features, cancer_ai, treatment_ai)

    except Exception as exc:
        raise HTTPException(
            status_code=400,
            detail=f"Feature construction failed: {exc}",
        )

    completeness = assess_completeness(row)

    if not completeness["enough"]:
        raise HTTPException(
            status_code=422,
            detail={
                "status": "insufficient_data",
                "message": "Not enough data to produce a safe prediction.",
                "present_count": completeness["present_count"],
                "min_required": completeness["min_required"],
                "missing_critical": completeness["missing_critical"],
            },
        )

    # Calendar-aware day counts generated by feature_engineering.py.
    # NDaysLogged  = total unique logged calendar days
    # LoggedDays3  = unique logged days in the last 3 calendar days
    # LoggedDays7  = unique logged days in the last 7 calendar days
    # LoggedDays30 = unique logged days in the last 30 calendar days
    days_logged = int(features.get("NDaysLogged") or len(daily_logs) or 0)
    logged_days_3 = int(features.get("LoggedDays3") or 0)
    logged_days_7 = int(features.get("LoggedDays7") or 0)
    logged_days_30 = int(features.get("LoggedDays30") or 0)

    days_by_risk = {
        "InfectionRisk": logged_days_3,
        "FebrileNeutropeniaRisk": logged_days_3,
        "DehydrationRisk": logged_days_3,
        "OverallClinicalDeteriorationRisk": logged_days_3,

        "MalnutritionRisk": logged_days_7,
        "RenalToxicityRisk": logged_days_7,
        "HepaticToxicityRisk": logged_days_7,
        "TreatmentDelayRisk": logged_days_7,
        "ImmunotherapyAdverseEventRisk": logged_days_7,

        "CachexiaRisk": logged_days_30,
    }

    try:
        preds = predict_all(
            row,
            days_logged=days_logged,
            days_by_risk=days_by_risk,
        )
    except Exception as exc:
        raise HTTPException(status_code=400, detail=f"Prediction failed: {exc}")

    return PredictResponse(
        version=MANIFEST.get("version", "unknown"),
        predictions=preds,
    )