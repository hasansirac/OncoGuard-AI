package com.example.oncoguardpatient

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.launch
import java.text.SimpleDateFormat
import java.util.*

private val TEAL = Color(0xFF1FB6A6)
private val CARD = Color(0xFF16212E)
private val INKDIM = Color(0xFF93A4B8)
private val GREEN = Color(0xFF22C55E)
private val YELLOW = Color(0xFFFFC857)
private val RED = Color(0xFFEF4444)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun homeFieldColors() = OutlinedTextFieldDefaults.colors(
    focusedBorderColor = TEAL,
    unfocusedBorderColor = Color(0xFF26384C),
    focusedTextColor = Color.White,
    unfocusedTextColor = Color.White,
    cursorColor = TEAL
)

@Composable
fun HomeScreen(onSubmitted: () -> Unit, onLogout: () -> Unit) {
    val scope = rememberCoroutineScope()

    var generalCondition by remember { mutableStateOf(3) }
    var bodyTemp by remember { mutableStateOf("") }
    var fatigue by remember { mutableStateOf(0) }
    var nausea by remember { mutableStateOf(0) }
    var pain by remember { mutableStateOf(0) }
    var cough by remember { mutableStateOf(0) }
    var dyspnea by remember { mutableStateOf(0) }
    var vomiting by remember { mutableStateOf(0) }
    var diarrhea by remember { mutableStateOf(0) }
    var water by remember { mutableStateOf("") }
    var oxygenText by remember { mutableStateOf("") }
    var dizziness by remember { mutableStateOf(false) }
    var tookMeds by remember { mutableStateOf(true) }
    var note by remember { mutableStateOf("") }
    var proteinText by remember { mutableStateOf("") }
    var caloriesText by remember { mutableStateOf("") }

    var selectedDate by remember { mutableStateOf<String?>(null) }
    var selectedDateLabel by remember { mutableStateOf("Select a date") }
    var selectedHasLog by remember { mutableStateOf(false) }
    var calendarDays by remember { mutableStateOf<List<DailyEntryDayDto>>(emptyList()) }
    var calendarLoading by remember { mutableStateOf(false) }
    var calendarSummary by remember { mutableStateOf("21-day monitoring cycle") }
    var detailLoading by remember { mutableStateOf(false) }

    var loading by remember { mutableStateOf(false) }
    var message by remember { mutableStateOf<String?>(null) }
    var isError by remember { mutableStateOf(false) }

    var readinessLoading by remember { mutableStateOf(true) }
    var readiness by remember { mutableStateOf<PatientDailyEntryStatusResponse?>(null) }
    var readinessError by remember { mutableStateOf<String?>(null) }

    fun resetFormForEmptyDay() {
        generalCondition = 3
        bodyTemp = ""
        fatigue = 0
        nausea = 0
        pain = 0
        cough = 0
        dyspnea = 0
        vomiting = 0
        diarrhea = 0
        water = ""
        oxygenText = ""
        dizziness = false
        tookMeds = true
        note = ""
        proteinText = ""
        caloriesText = ""
        selectedHasLog = false
    }

    fun applySavedDetail(detail: DailyLogDetailResponse) {
        selectedHasLog = detail.hasLog

        if (!detail.hasLog) {
            resetFormForEmptyDay()
            return
        }

        generalCondition = detail.generalConditionScore ?: 3
        bodyTemp = formatNumber(detail.bodyTemperature)
        fatigue = detail.fatigue ?: 0
        nausea = detail.nausea ?: 0
        pain = detail.pain ?: 0
        cough = detail.cough ?: 0
        dyspnea = detail.dyspnea ?: 0
        vomiting = detail.vomitingCount ?: 0
        diarrhea = detail.diarrheaCount ?: 0
        water = formatNumber(detail.waterIntakeMl)
        oxygenText = formatNumber(detail.oxygenSaturation)
        dizziness = detail.hasDizziness ?: false
        tookMeds = detail.tookMainMedication ?: true
        note = detail.patientNote ?: ""
        proteinText = formatNumber(detail.protein)
        caloriesText = formatNumber(detail.calories)
    }

    fun loadDateDetail(date: String) {
        val pid = AppState.patientId ?: return
        detailLoading = true
        message = null
        isError = false

        scope.launch {
            try {
                val response = RetrofitClient.api.getDailyLogByDate(pid, date)
                if (response.isSuccessful) {
                    val body = response.body()
                    if (body != null) {
                        applySavedDetail(body)
                    } else {
                        resetFormForEmptyDay()
                    }
                } else {
                    isError = true
                    message = "Could not load the selected date."
                    resetFormForEmptyDay()
                }
            } catch (e: Exception) {
                isError = true
                message = "Cannot reach the server while loading the selected date."
                resetFormForEmptyDay()
            } finally {
                detailLoading = false
            }
        }
    }

    fun refreshCalendar(keepSelectedDate: String? = selectedDate) {
        val pid = AppState.patientId ?: return
        calendarLoading = true

        scope.launch {
            try {
                val response = RetrofitClient.api.getDailyEntryCalendar(pid)
                if (response.isSuccessful) {
                    val body = response.body()
                    val days = body?.days ?: emptyList()
                    calendarDays = days
                    calendarSummary = if (body?.startDate != null && body.endDate != null) {
                        "Cycle ${body.currentCycleDay ?: 0}/${body.cycleLengthDays ?: 21} · ${body.startDate} to ${body.endDate}"
                    } else {
                        "21-day monitoring cycle"
                    }

                    val target = days.firstOrNull { it.date == keepSelectedDate && it.canEdit }
                        ?: days.firstOrNull { it.isToday && it.canEdit }
                        ?: days.lastOrNull { it.canEdit }
                    if (target != null) {
                        selectedDate = target.date
                        selectedDateLabel = target.displayLabel
                        selectedHasLog = target.hasLog
                        loadDateDetail(target.date)
                    } else {
                        selectedDate = null
                        selectedDateLabel = "No editable date is open yet"
                        selectedHasLog = false
                        resetFormForEmptyDay()
                    }
                } else {
                    calendarDays = emptyList()
                    isError = true
                    message = "Could not load monitoring dates. Ask your doctor to check the active lab cycle."
                }
            } catch (e: Exception) {
                calendarDays = emptyList()
                isError = true
                message = "Cannot reach the server while loading monitoring dates."
            } finally {
                calendarLoading = false
            }
        }
    }

    fun refreshReadiness() {
        val pid = AppState.patientId
        if (pid == null) {
            readinessLoading = false
            readinessError = "Patient ID not found. Please sign in again."
            readiness = null
            return
        }

        readinessLoading = true
        readinessError = null

        scope.launch {
            try {
                val response = RetrofitClient.api.getPatientDailyEntryStatus(pid)
                if (response.isSuccessful) {
                    readiness = response.body()
                    if (response.body()?.canEnterDailyData == true) {
                        refreshCalendar(null)
                    }
                } else {
                    readiness = null
                    readinessError = "Could not check daily entry status. Please try again."
                }
            } catch (e: Exception) {
                readiness = null
                readinessError = "Cannot reach the server. Is the backend running?"
            } finally {
                readinessLoading = false
            }
        }
    }

    LaunchedEffect(AppState.patientId) {
        refreshReadiness()
    }

    Box(
        Modifier.fillMaxSize()
            .background(Brush.verticalGradient(listOf(Color(0xFF0F1720), Color(0xFF16212E))))
    ) {
        Column(
            Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(20.dp)
        ) {
            Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                Column(Modifier.weight(1f)) {
                    Text("Hello, ${AppState.patientName}", color = Color.White, fontSize = 20.sp, fontWeight = FontWeight.Bold)
                    Text("Daily monitoring calendar", color = INKDIM, fontSize = 13.sp)
                }
                TextButton(onClick = onLogout) { Text("Sign Out", color = INKDIM, fontSize = 13.sp) }
            }
            Spacer(Modifier.height(18.dp))

            when {
                readinessLoading -> {
                    SectionCard("Checking patient access") {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            CircularProgressIndicator(Modifier.size(22.dp), color = TEAL, strokeWidth = 2.dp)
                            Spacer(Modifier.width(12.dp))
                            Text("Checking whether your doctor has completed your lab result and clinical profile...", color = INKDIM, fontSize = 13.sp)
                        }
                    }
                    Spacer(Modifier.height(24.dp))
                    return@Column
                }

                readinessError != null -> {
                    PatientLockedCard(
                        title = "Daily entry status could not be checked",
                        message = readinessError ?: "Unknown error.",
                        missingItems = emptyList(),
                        onRefresh = { refreshReadiness() },
                        onLogout = onLogout
                    )
                    Spacer(Modifier.height(24.dp))
                    return@Column
                }

                readiness?.canEnterDailyData != true -> {
                    PatientLockedCard(
                        title = "Daily entry is not open yet",
                        message = readiness?.message ?: "Your doctor must complete your baseline lab result and clinical profile first.",
                        missingItems = readiness?.missingItems ?: emptyList(),
                        onRefresh = { refreshReadiness() },
                        onLogout = onLogout
                    )
                    Spacer(Modifier.height(24.dp))
                    return@Column
                }
            }

            SectionCard("21-day monitoring cycle") {
                Text(calendarSummary, color = TEAL, fontSize = 13.sp, fontWeight = FontWeight.Bold)
                Spacer(Modifier.height(6.dp))
                Text(
                    "The doctor opens this cycle with a baseline lab result. Today and past cycle days can be filled or updated. Future days are visible but locked until that date arrives.",
                    color = INKDIM,
                    fontSize = 12.sp
                )
                Spacer(Modifier.height(10.dp))

                if (calendarLoading) {
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        CircularProgressIndicator(Modifier.size(20.dp), color = TEAL, strokeWidth = 2.dp)
                        Spacer(Modifier.width(10.dp))
                        Text("Loading dates...", color = INKDIM, fontSize = 13.sp)
                    }
                } else if (calendarDays.isEmpty()) {
                    Text("No monitoring cycle dates are open. Ask your doctor to check the active lab cycle.", color = YELLOW, fontSize = 13.sp)
                } else {
                    calendarDays.forEach { day ->
                        DailyDateRow(
                            day = day,
                            selected = selectedDate == day.date,
                            onClick = {
                                if (day.canEdit) {
                                    selectedDate = day.date
                                    selectedDateLabel = day.displayLabel
                                    selectedHasLog = day.hasLog
                                    loadDateDetail(day.date)
                                } else {
                                    message = "This cycle day is upcoming and locked until its date arrives."
                                    isError = true
                                }
                            }
                        )
                        Spacer(Modifier.height(8.dp))
                    }
                }

                Spacer(Modifier.height(8.dp))
                OutlinedButton(onClick = { refreshCalendar(selectedDate) }, shape = RoundedCornerShape(10.dp)) {
                    Text("Refresh dates", color = TEAL)
                }
            }

            SectionCard("Selected date") {
                Text(selectedDateLabel, color = TEAL, fontSize = 15.sp, fontWeight = FontWeight.Bold)
                Spacer(Modifier.height(6.dp))
                Text(
                    if (selectedHasLog) "Status: Filled. Saving again will update this cycle day." else "Status: Missing. Saving will create this cycle day.",
                    color = if (selectedHasLog) GREEN else YELLOW,
                    fontSize = 13.sp,
                    fontWeight = FontWeight.SemiBold
                )
                if (detailLoading) {
                    Spacer(Modifier.height(10.dp))
                    Row(verticalAlignment = Alignment.CenterVertically) {
                        CircularProgressIndicator(Modifier.size(18.dp), color = TEAL, strokeWidth = 2.dp)
                        Spacer(Modifier.width(8.dp))
                        Text("Loading selected date data...", color = INKDIM, fontSize = 12.sp)
                    }
                }
            }

            SectionCard("General condition") {
                Text(when (generalCondition) {
                    1 -> "Very bad"; 2 -> "Bad"; 3 -> "Okay"; 4 -> "Good"; else -> "Very good"
                }, color = TEAL, fontSize = 14.sp, fontWeight = FontWeight.Bold)
                Slider(
                    value = generalCondition.toFloat(),
                    onValueChange = { generalCondition = it.toInt() },
                    valueRange = 1f..5f, steps = 3,
                    colors = SliderDefaults.colors(thumbColor = TEAL, activeTrackColor = TEAL)
                )
            }

            SectionCard("Body temperature (°C)") {
                OutlinedTextField(
                    value = bodyTemp, onValueChange = { bodyTemp = it },
                    placeholder = { Text("e.g. 37.5", color = INKDIM) },
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    modifier = Modifier.fillMaxWidth(), colors = homeFieldColors()
                )
            }

            SectionCard("Symptoms") {
                GradeRow("Fatigue", fatigue) { fatigue = it }
                GradeRow("Nausea", nausea) { nausea = it }
                GradeRow("Pain", pain) { pain = it }
                GradeRow("Cough", cough) { cough = it }
                GradeRow("Shortness of breath", dyspnea) { dyspnea = it }
            }

            SectionCard("Daily counts") {
                CounterRow("Vomiting", vomiting, { vomiting = (vomiting - 1).coerceAtLeast(0) }, { vomiting++ })
                CounterRow("Diarrhea", diarrhea, { diarrhea = (diarrhea - 1).coerceAtLeast(0) }, { diarrhea++ })
            }

            SectionCard("Water intake (ml)") {
                OutlinedTextField(
                    value = water, onValueChange = { water = it },
                    placeholder = { Text("e.g. 1500", color = INKDIM) },
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    modifier = Modifier.fillMaxWidth(), colors = homeFieldColors()
                )
            }

            SectionCard("Vital signs") {
                OutlinedTextField(
                    value = oxygenText,
                    onValueChange = { oxygenText = it },
                    placeholder = { Text("e.g. 97", color = INKDIM) },
                    label = { Text("Oxygen saturation (%)", color = INKDIM) },
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    modifier = Modifier.fillMaxWidth(),
                    colors = homeFieldColors()
                )
                Spacer(Modifier.height(8.dp))
                Text("Optional. Enter only if measured with a pulse oximeter.", color = INKDIM, fontSize = 12.sp)
            }

            SectionCard("Nutrition") {
                OutlinedTextField(
                    value = proteinText,
                    onValueChange = { proteinText = it },
                    placeholder = { Text("e.g. 75", color = INKDIM) },
                    label = { Text("Protein (g)", color = INKDIM) },
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                    modifier = Modifier.fillMaxWidth(),
                    colors = homeFieldColors()
                )

                Spacer(Modifier.height(10.dp))

                OutlinedTextField(
                    value = caloriesText,
                    onValueChange = { caloriesText = it },
                    placeholder = { Text("e.g. 1600", color = INKDIM) },
                    label = { Text("Calories (kcal)", color = INKDIM) },
                    singleLine = true,
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    modifier = Modifier.fillMaxWidth(),
                    colors = homeFieldColors()
                )
            }

            SectionCard("Other") {
                ToggleRow("Dizziness", dizziness) { dizziness = it }
                ToggleRow("Took medication for this date", tookMeds) { tookMeds = it }
            }

            SectionCard("Note (optional)") {
                OutlinedTextField(
                    value = note, onValueChange = { note = it },
                    placeholder = { Text("Anything you want to add...", color = INKDIM) },
                    modifier = Modifier.fillMaxWidth().height(90.dp), colors = homeFieldColors()
                )
            }

            if (message != null) {
                Spacer(Modifier.height(10.dp))
                Text(message!!, color = if (isError) RED else GREEN, fontSize = 13.sp)
            }

            Spacer(Modifier.height(18.dp))
            Button(
                onClick = {
                    message = null; isError = false
                    val pid = AppState.patientId
                    val date = selectedDate
                    if (pid == null) {
                        message = "Patient ID not found. Please sign in again."; isError = true
                        return@Button
                    }
                    if (date.isNullOrBlank()) {
                        message = "Please select a monitoring date."; isError = true
                        return@Button
                    }
                    loading = true
                    scope.launch {
                        try {
                            val temperature = bodyTemp.toDoubleOrNull()
                            val waterMl = water.toDoubleOrNull()
                            val oxygen = oxygenText.trim().ifBlank { null }?.toDoubleOrNull()
                            val protein = proteinText.toDoubleOrNull()
                            val calories = caloriesText.toDoubleOrNull()

                            if (temperature == null || temperature <= 0.0) {
                                message = "Please enter body temperature."
                                isError = true
                                return@launch
                            }
                            if (waterMl == null || waterMl <= 0.0) {
                                message = "Please enter water intake."
                                isError = true
                                return@launch
                            }
                            if (oxygenText.isNotBlank() && (oxygen == null || oxygen <= 0.0 || oxygen > 100.0)) {
                                message = "Please enter a valid oxygen saturation value, or leave it empty if you did not measure it."
                                isError = true
                                return@launch
                            }
                            if (protein == null || protein <= 0.0 || calories == null || calories <= 0.0) {
                                message = "Please enter protein and calories."
                                isError = true
                                return@launch
                            }

                            val req = CreateDailyLogRequest(
                                patientId = pid,
                                logDate = buildLogDateForDate(date),
                                generalConditionScore = generalCondition,
                                patientNote = note.ifBlank { null },
                                bodyTemperature = temperature,
                                fatigue = fatigue, pain = pain, nausea = nausea,
                                vomitingCount = vomiting, diarrheaCount = diarrhea,
                                constipation = 0, cough = cough, dyspnea = dyspnea,
                                mouthSore = 0, swallowingDifficulty = 0, skinRash = 0,
                                hasBleedingOrBruising = false, hasDizziness = dizziness, hasConfusion = false,
                                otherSymptoms = null,
                                waterIntakeMl = waterMl,
                                dryMouth = 0, urineColor = null, urinationCount = 0,
                                tookMainMedication = tookMeds, missedDoseCount = if (tookMeds) 0 else 1,
                                usedAntibiotic = false, usedSteroid = false,
                                usedAntiemetic = false, usedPainkiller = false,
                                hadSideEffect = false, sideEffectDescription = null,
                                systolicBloodPressure = null, diastolicBloodPressure = null,
                                heartRate = null, oxygenSaturation = oxygen
                            )
                            val resp = RetrofitClient.api.createDailyLog(req)

                            if (!resp.isSuccessful) {
                                message = "Could not save daily log. Doctor may need to complete your lab result and clinical profile first."
                                isError = true
                                refreshReadiness()
                                return@launch
                            }

                            val dailyLogId = resp.body()?.dailyLogId

                            if (dailyLogId == null) {
                                message = "Daily log saved, but dailyLogId was not returned."
                                isError = true
                                return@launch
                            }

                            val foodLogRequest = CreateFoodLogRequest(
                                dailyLogId = dailyLogId,
                                foodName = "Daily nutrition summary",
                                amountGram = 0.0,
                                calories = calories,
                                protein = protein,
                                carbohydrate = 0.0,
                                fat = 0.0,
                                source = "Android Patient App"
                            )

                            val foodResp = RetrofitClient.api.createFoodLog(foodLogRequest)

                            if (!foodResp.isSuccessful) {
                                message = "Daily log saved, but nutrition record could not be saved."
                                isError = true
                                return@launch
                            }

                            message = if (selectedHasLog) "Selected date updated successfully." else "Selected date saved successfully."
                            isError = false
                            selectedHasLog = true
                            refreshCalendar(date)
                        } catch (e: Exception) {
                            message = "Cannot reach the server. Is the backend running?"; isError = true
                        } finally {
                            loading = false
                        }
                    }
                },
                enabled = !loading && !detailLoading && selectedDate != null,
                modifier = Modifier.fillMaxWidth().height(52.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(containerColor = TEAL)
            ) {
                if (loading) CircularProgressIndicator(Modifier.size(22.dp), color = Color.White, strokeWidth = 2.dp)
                else Text(
                    if (selectedHasLog) "Update Selected Date Data" else "Save Selected Date Data",
                    fontSize = 15.sp,
                    fontWeight = FontWeight.SemiBold,
                    color = Color(0xFF04130F)
                )
            }
            Spacer(Modifier.height(24.dp))
        }
    }
}

@Composable
private fun DailyDateRow(
    day: DailyEntryDayDto,
    selected: Boolean,
    onClick: () -> Unit
) {
    val locked = !day.canEdit || day.isFuture
    val bg = when {
        selected -> TEAL
        locked -> Color(0xFF111827)
        day.hasLog -> Color(0xFF123626)
        else -> Color(0xFF0F1720)
    }
    val mainText = if (selected) Color(0xFF04130F) else if (locked) INKDIM else Color.White
    val subText = when {
        selected -> Color(0xFF04130F)
        locked -> INKDIM
        day.hasLog -> GREEN
        else -> YELLOW
    }

    Row(
        Modifier.fillMaxWidth()
            .background(bg, RoundedCornerShape(10.dp))
            .clickable(enabled = !locked) { onClick() }
            .padding(12.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Column(Modifier.weight(1f)) {
            Text(day.displayLabel, color = mainText, fontSize = 13.sp, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(3.dp))
            val description = when {
                locked -> "Upcoming - locked until this date"
                day.hasLog -> "Filled - tap to update"
                else -> "Missing - tap to create"
            }
            Text(description, color = subText, fontSize = 12.sp)
        }
        Text(
            when {
                locked -> "🔒"
                day.hasLog -> "✓"
                else -> "!"
            },
            color = subText,
            fontSize = 18.sp,
            fontWeight = FontWeight.Bold
        )
    }
}

@Composable
private fun PatientLockedCard(
    title: String,
    message: String,
    missingItems: List<String>,
    onRefresh: () -> Unit,
    onLogout: () -> Unit
) {
    SectionCard(title) {
        Text(message, color = INKDIM, fontSize = 13.sp)
        if (missingItems.isNotEmpty()) {
            Spacer(Modifier.height(12.dp))
            Text("Missing doctor-entered information:", color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(6.dp))
            missingItems.forEach { item ->
                Text("• $item", color = YELLOW, fontSize = 13.sp)
            }
        }
        Spacer(Modifier.height(14.dp))
        Text(
            "Your doctor must enter the baseline lab result and clinical profile before daily monitoring starts.",
            color = INKDIM,
            fontSize = 12.sp
        )
        Spacer(Modifier.height(16.dp))
        Row(horizontalArrangement = Arrangement.spacedBy(10.dp)) {
            Button(
                onClick = onRefresh,
                colors = ButtonDefaults.buttonColors(containerColor = TEAL),
                shape = RoundedCornerShape(10.dp)
            ) {
                Text("Refresh", color = Color(0xFF04130F), fontWeight = FontWeight.Bold)
            }
            OutlinedButton(onClick = onLogout, shape = RoundedCornerShape(10.dp)) {
                Text("Sign Out", color = INKDIM)
            }
        }
    }
}

private fun buildLogDateForDate(date: String): String {
    return "${date}T12:00:00"
}

private fun formatNumber(value: Double?): String {
    if (value == null) return ""
    val longValue = value.toLong()
    return if (value == longValue.toDouble()) longValue.toString() else value.toString()
}

@Composable
private fun SectionCard(title: String, content: @Composable ColumnScope.() -> Unit) {
    Column(
        Modifier.fillMaxWidth().padding(bottom = 12.dp)
            .background(CARD, RoundedCornerShape(14.dp)).padding(16.dp)
    ) {
        Text(title, color = Color.White, fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(10.dp))
        content()
    }
}

@Composable
private fun GradeRow(label: String, value: Int, onChange: (Int) -> Unit) {
    Column(Modifier.padding(vertical = 6.dp)) {
        Text(label, color = INKDIM, fontSize = 13.sp)
        Spacer(Modifier.height(6.dp))
        Row(horizontalArrangement = Arrangement.spacedBy(6.dp)) {
            val labels = listOf("None", "Mild", "Moderate", "Severe")
            labels.forEachIndexed { i, t ->
                val selected = value == i
                Box(
                    Modifier.weight(1f)
                        .background(if (selected) TEAL else Color(0xFF0F1720), RoundedCornerShape(8.dp))
                        .clickable { onChange(i) }
                        .padding(vertical = 9.dp),
                    contentAlignment = Alignment.Center
                ) {
                    Text(t, color = if (selected) Color(0xFF04130F) else INKDIM,
                        fontSize = 11.sp, fontWeight = if (selected) FontWeight.Bold else FontWeight.Normal)
                }
            }
        }
    }
}

@Composable
private fun CounterRow(label: String, value: Int, onMinus: () -> Unit, onPlus: () -> Unit) {
    Row(Modifier.fillMaxWidth().padding(vertical = 6.dp), verticalAlignment = Alignment.CenterVertically) {
        Text(label, color = INKDIM, fontSize = 13.sp, modifier = Modifier.weight(1f))
        OutlinedButton(onClick = onMinus, modifier = Modifier.size(40.dp), contentPadding = PaddingValues(0.dp)) { Text("−", fontSize = 18.sp, color = TEAL) }
        Text("$value", color = Color.White, fontSize = 16.sp, fontWeight = FontWeight.Bold,
            modifier = Modifier.padding(horizontal = 16.dp))
        OutlinedButton(onClick = onPlus, modifier = Modifier.size(40.dp), contentPadding = PaddingValues(0.dp)) { Text("+", fontSize = 18.sp, color = TEAL) }
    }
}

@Composable
private fun ToggleRow(label: String, value: Boolean, onChange: (Boolean) -> Unit) {
    Row(Modifier.fillMaxWidth().padding(vertical = 4.dp), verticalAlignment = Alignment.CenterVertically) {
        Text(label, color = INKDIM, fontSize = 13.sp, modifier = Modifier.weight(1f))
        Switch(checked = value, onCheckedChange = onChange,
            colors = SwitchDefaults.colors(checkedThumbColor = Color.White, checkedTrackColor = TEAL))
    }
}
