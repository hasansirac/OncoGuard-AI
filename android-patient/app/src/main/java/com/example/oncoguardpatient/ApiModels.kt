package com.example.oncoguardpatient

// ---- LOGIN ----
data class LoginRequest(
    val email: String,
    val password: String
)

data class LoginResponse(
    val token: String,
    val role: String? = null,
    val userId: Int? = null,
    val doctorId: Int? = null,
    val patientId: Int? = null
)

// ---- REGISTER (hasta kaydi) ----
data class RegisterPatientRequest(
    val username: String,      // ad soyad
    val email: String,
    val password: String,
    val hospitalId: Int,
    val doctorId: Int,
    val age: Int,
    val gender: Int,           // 1=Male, 2=Female
    val height: Double,        // cm
    val weight: Double,        // kg
    val cancerType: Int,       // 1=Lung,2=Breast,3=Colon,4=Prostate,5=Other
    val treatmentType: Int     // 1=Chemo,2=Radio,3=Immuno,4=Targeted,5=Hormone
)

// ---- HASTANE & DOKTOR (kayit ekraninda secim icin) ----
data class HospitalDto(
    val id: Int,
    val name: String,
    val city: String
)

data class DoctorDto(
    val id: Int,
    val name: String,
    val email: String
)

// ---- GUNLUK VERI GIRISI ----
data class CreateDailyLogRequest(
    val patientId: Int,
    val logDate: String,
    val generalConditionScore: Int,
    val patientNote: String?,
    val bodyTemperature: Double?,
    val fatigue: Int,
    val pain: Int,
    val nausea: Int,
    val vomitingCount: Int,
    val diarrheaCount: Int,
    val constipation: Int,
    val cough: Int,
    val dyspnea: Int,
    val mouthSore: Int,
    val swallowingDifficulty: Int,
    val skinRash: Int,
    val hasBleedingOrBruising: Boolean,
    val hasDizziness: Boolean,
    val hasConfusion: Boolean,
    val otherSymptoms: String?,
    val waterIntakeMl: Double,
    val dryMouth: Int,
    val urineColor: String?,
    val urinationCount: Int,
    val tookMainMedication: Boolean,
    val missedDoseCount: Int,
    val usedAntibiotic: Boolean,
    val usedSteroid: Boolean,
    val usedAntiemetic: Boolean,
    val usedPainkiller: Boolean,
    val hadSideEffect: Boolean,
    val sideEffectDescription: String?,
    val systolicBloodPressure: Double?,
    val diastolicBloodPressure: Double?,
    val heartRate: Double?,
    val oxygenSaturation: Double?
)
data class CreateDailyLogResponse(
    val dailyLogId: Int,
    val message: String? = null
)
data class CreateFoodLogRequest(
    val dailyLogId: Int,
    val foodName: String,
    val amountGram: Double,
    val calories: Double,
    val protein: Double,
    val carbohydrate: Double = 0.0,
    val fat: Double = 0.0,
    val fiber: Double? = null,
    val iron: Double? = null,
    val vitaminB12: Double? = null,
    val folate: Double? = null,
    val vitaminD: Double? = null,
    val zinc: Double? = null,
    val magnesium: Double? = null,
    val selenium: Double? = null,
    val source: String? = "Android Patient App"
)


// ---- PATIENT DAILY ENTRY READINESS ----
data class PatientDailyEntryStatusResponse(
    val patientId: Int,
    val canEnterDailyData: Boolean,
    val missingItems: List<String> = emptyList(),
    val message: String? = null
)

// ---- DAILY ENTRY CALENDAR / DATE PREFILL ----
data class DailyEntryCalendarResponse(
    val patientId: Int,
    val activeCycleId: Int? = null,
    val startDate: String? = null,
    val endDate: String? = null,
    val cycleLengthDays: Int? = null,
    val currentCycleDay: Int? = null,
    val days: List<DailyEntryDayDto> = emptyList(),
    val message: String? = null
)

data class DailyEntryDayDto(
    val date: String,
    val displayLabel: String,
    val cycleDay: Int? = null,
    val cycleLengthDays: Int? = null,
    val isToday: Boolean,
    val isFuture: Boolean = false,
    val canEdit: Boolean = true,
    val hasLog: Boolean,
    val dailyLogId: Int? = null,
    val status: String? = null
)

data class DailyLogDetailResponse(
    val patientId: Int,
    val date: String,
    val hasLog: Boolean,
    val dailyLogId: Int? = null,
    val generalConditionScore: Int? = null,
    val patientNote: String? = null,
    val bodyTemperature: Double? = null,
    val fatigue: Int? = null,
    val pain: Int? = null,
    val nausea: Int? = null,
    val vomitingCount: Int? = null,
    val diarrheaCount: Int? = null,
    val cough: Int? = null,
    val dyspnea: Int? = null,
    val hasDizziness: Boolean? = null,
    val waterIntakeMl: Double? = null,
    val tookMainMedication: Boolean? = null,
    val missedDoseCount: Int? = null,
    val oxygenSaturation: Double? = null,
    val protein: Double? = null,
    val calories: Double? = null,
    val message: String? = null
)
