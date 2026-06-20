"""
preprocessing.py
================
Ham dataset (generator v2 ciktisi) -> modele hazir X/y matrisleri.

Ilkeler:
- LEAKAGE YOK: medyanlar SADECE train'den ogrenilir, test'e uygulanir.
- 'scenario' MODELE VERILMEZ (label'i ele verir). Sadece metadata/stratify referansi.
- CancerType / TreatmentType -> sabit sozlukle one-hot (train/test/serving ayni kolonlar).
- Eksik feature -> {kolon}_missing bayragi + train medyani ile impute
  (gunluk seviyede LOCF/rolling feature_engineering'de yapildi; burada
   pencere hesaplanamayan NaN'ler ele alinir).
- Stratified split: Overall label uzerinden (seyrek Red'leri korur).
- Preprocessor JSON'a kaydedilir -> FastAPI ayni donusumu serving'de kullanir.
"""

from __future__ import annotations
import json
import numpy as np
import pandas as pd
from sklearn.model_selection import train_test_split

from oncoguard_ai.features.feature_engineering import FEATURE_ORDER
from oncoguard_ai.data.synthetic_data_v2 import LABEL_COLS, CANCER_TYPES

TREATMENT_TYPES = ["Chemotherapy", "Immunotherapy", "ChemoImmunotherapy",
                   "Targeted", "Radiation", "Hormone"]
LABEL_PREFIX = "label_"
STRATIFY_LABEL = "label_OverallClinicalDeteriorationRisk"


class Preprocessor:
    """fit() train'den ogrenir; transform() herhangi bir df'i X matrisine cevirir."""

    def __init__(self):
        self.medians: dict[str, float] = {}
        self.missing_flag_cols: list[str] = []
        self.feature_columns: list[str] = []   # nihai kolon sirasi (model girdisi)

    # --- ogrenme (sadece train) ---
    def fit(self, df: pd.DataFrame):
        num = df[FEATURE_ORDER].apply(pd.to_numeric, errors="coerce")
        self.medians = {c: float(num[c].median()) for c in FEATURE_ORDER}
        # train'de NaN gorulen kolonlara missing bayragi
        self.missing_flag_cols = [c for c in FEATURE_ORDER if num[c].isna().any()]
        # nihai kolonlar: sayisal + missing flag + one-hot kategoriler
        self.feature_columns = (
            list(FEATURE_ORDER)
            + [f"{c}_missing" for c in self.missing_flag_cols]
            + [f"CancerType_{c}" for c in CANCER_TYPES]
            + [f"TreatmentType_{t}" for t in TREATMENT_TYPES]
        )
        return self

    # --- uygulama ---
    def transform(self, df: pd.DataFrame) -> pd.DataFrame:
        num = df[FEATURE_ORDER].apply(pd.to_numeric, errors="coerce")
        # missing bayraklari (impute ETMEDEN once)
        flags = {f"{c}_missing": num[c].isna().astype(int)
                 for c in self.missing_flag_cols}
        # train medyani ile impute
        num = num.fillna(self.medians)
        out = pd.concat([num, pd.DataFrame(flags, index=df.index)], axis=1)
        # sabit sozlukle one-hot (eksik kategori -> 0 kolonu)
        for c in CANCER_TYPES:
            out[f"CancerType_{c}"] = (df.get("CancerType") == c).astype(int)
        for t in TREATMENT_TYPES:
            out[f"TreatmentType_{t}"] = (df.get("TreatmentType") == t).astype(int)
        return out[self.feature_columns]

    # --- kayit / yukleme (serving icin) ---
    def save(self, path: str):
        with open(path, "w") as f:
            json.dump({"medians": self.medians,
                       "missing_flag_cols": self.missing_flag_cols,
                       "feature_columns": self.feature_columns}, f, indent=2)

    @classmethod
    def load(cls, path: str):
        p = cls()
        with open(path) as f:
            d = json.load(f)
        p.medians = d["medians"]; p.missing_flag_cols = d["missing_flag_cols"]
        p.feature_columns = d["feature_columns"]
        return p


def prepare(csv_path: str, test_size=0.2, seed=42):
    """CSV -> (X_train, X_test, y_train, y_test, preprocessor)."""
    df = pd.read_csv(csv_path)
    y = df[[LABEL_PREFIX + c for c in
            [l for l in LABEL_COLS]]] if False else df[[c for c in df.columns
                                                        if c.startswith(LABEL_PREFIX)]]
    strat = df[STRATIFY_LABEL]
    idx_tr, idx_te = train_test_split(
        df.index, test_size=test_size, random_state=seed, stratify=strat)
    df_tr, df_te = df.loc[idx_tr], df.loc[idx_te]

    pre = Preprocessor().fit(df_tr)        # SADECE train'den ogren
    X_train = pre.transform(df_tr)
    X_test = pre.transform(df_te)
    y_train = df_tr[[c for c in df.columns if c.startswith(LABEL_PREFIX)]]
    y_test = df_te[[c for c in df.columns if c.startswith(LABEL_PREFIX)]]
    return X_train, X_test, y_train, y_test, pre


if __name__ == "__main__":
    import os
    os.makedirs("oncoguard_ai/outputs/processed", exist_ok=True)
    Xtr, Xte, ytr, yte, pre = prepare("oncoguard_ai/outputs/oncoguard_dataset_v2_main.csv")
    pre.save("oncoguard_ai/outputs/processed/preprocessor.json")
    Xtr.to_csv("oncoguard_ai/outputs/processed/X_train.csv", index=False)
    Xte.to_csv("oncoguard_ai/outputs/processed/X_test.csv", index=False)
    ytr.to_csv("oncoguard_ai/outputs/processed/y_train.csv", index=False)
    yte.to_csv("oncoguard_ai/outputs/processed/y_test.csv", index=False)

    print("PREPROCESSING TAMAM")
    print(f"  X_train: {Xtr.shape}   X_test: {Xte.shape}")
    print(f"  Model feature sayisi: {len(pre.feature_columns)}")
    print(f"  Missing-flag kolonu: {len(pre.missing_flag_cols)}")
    print(f"  Kalan NaN (train): {int(Xtr.isna().sum().sum())} (beklenen 0)")
    print(f"  'scenario' X icinde mi? {'scenario' in Xtr.columns} (False olmali)")
    one_hot = [c for c in Xtr.columns if c.startswith(('CancerType_', 'TreatmentType_'))]
    print(f"  One-hot kolonlar ({len(one_hot)}): {one_hot}")
    # stratify dogru mu? train/test Overall dagilimi benzer olmali
    print("\n  Overall dagilim oranlari (train vs test):")
    for split, ys in [("train", ytr), ("test", yte)]:
        d = ys[STRATIFY_LABEL].value_counts(normalize=True).reindex([0,1,2,3]).round(3)
        print(f"    {split}: {dict(d)}")
