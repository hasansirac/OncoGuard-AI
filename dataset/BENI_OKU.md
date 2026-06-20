# OncoGuard-AI — KULLANILAN Dataset Paketi (v2.1)

Bu klasör, projende GERÇEKTEN kullandığın ve modellerin eğitildiği v2.1 dataset'ini
üreten doğrulanmış dosya setidir. Beş farklı zip'e (datasetler, datasetler2, dateset3,
dataset4, dataset5) dağılmış sürümler arasından, v2.1'i BİREBİR (hash eşleşmesiyle)
yeniden üreten doğru kombinasyon seçilip bir araya getirilmiştir.

DOĞRULAMA: Aşağıdaki komut çalıştırıldığında oncoguard_dataset_v21_main.csv
(14998 satır × 87 kolon) byte-for-byte yeniden üretilir.

    python3 -c "import sys; sys.path.insert(0,'.'); \
    from oncoguard_ai.data.synthetic_data_v2 import generate_balanced_dataset; \
    df = generate_balanced_dataset(n_total=15000, seed=42); print(df.shape)"

## İçindekiler ve her dosyanın kaynağı

oncoguard_ai/
  core/clinical_constants.py    <- dataset4 (iç paket). clamp_labs İÇEREN tek doğru sürüm.
                                   CTCAE grade fonksiyonları + ESPEN hedefleri
                                   (PROTEIN 1.3 g/kg, KALORİ 27.5 kcal/kg, SU 30 ml/kg).
  features/feature_engineering.py <- dataset4 (iç paket). FEATURE_ORDER = 74 feature (v2.1 ile eşleşir).
  rules/rule_engine.py          <- dataset4 (iç paket). Per-risk, CTCAE grade tabanlı, Rule>AI füzyonu.
  models/preprocessing.py       <- dataset4 (iç paket). Train-only medyan, scenario'yu atar, one-hot.
  data/synthetic_data_v2.py     <- dataset5. v2.1 GENERATOR (generate_balanced_dataset burada).
  data/synthetic_data.py        <- v1 generator (arşiv/referans, v2.1'de kullanılmaz).

oncoguard_dataset_v21_main.csv  <- dataset5. Modellerin eğitildiği nihai v2.1 verisi
                                   (14998 satır, 74 feature + 10 label + scenario/CancerType/TreatmentType).

## Üretim zinciri (kim kimi çağırır)
synthetic_data_v2.generate_balanced_dataset()
   -> feature_engineering.build_features() / FEATURE_ORDER
   -> clinical_constants (grade'ler, hedefler, komorbidite tabloları, clamp_labs)
Çıkan ham veri preprocessing.Preprocessor ile X/y matrisine + preprocessor_metadata'ya dönüşür;
sonra eğitim notebook'u (OncoGuardAI_Training.ipynb, Faz 2) 10 XGBoost modelini üretir.

## NOT (eksik olan tek şey — veri değil)
Bu paket veri/feature/kural/preprocessing tarafını TAM içerir. Eğitim script'i (10 XGBoost'u
eğitip *_xgb.joblib + manifest üreten kod) bu klasörde DEĞİL; o, OncoGuardAI_Training.ipynb
not defterindedir (Faz 2 hücreleri). İkisini birlikte saklarsan tüm AI tarafı eksiksiz olur.
