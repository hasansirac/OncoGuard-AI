package com.example.oncoguardpatient

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.ArrowDropDown
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun RegisterScreen(
    onRegisterSuccess: () -> Unit,
    onGoLogin: () -> Unit
) {
    val teal = Color(0xFF1FB6A6)
    val scope = rememberCoroutineScope()

    var username by remember { mutableStateOf("") }
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var age by remember { mutableStateOf("") }
    var height by remember { mutableStateOf("") }
    var weight by remember { mutableStateOf("") }

    var gender by remember { mutableStateOf(0) }          // 1=Male,2=Female
    var cancerType by remember { mutableStateOf(0) }      // 1..5
    var treatmentType by remember { mutableStateOf(0) }   // 1..5

    var hospitals by remember { mutableStateOf<List<HospitalDto>>(emptyList()) }
    var doctors by remember { mutableStateOf<List<DoctorDto>>(emptyList()) }
    var selectedHospital by remember { mutableStateOf<HospitalDto?>(null) }
    var selectedDoctor by remember { mutableStateOf<DoctorDto?>(null) }

    var loading by remember { mutableStateOf(false) }
    var message by remember { mutableStateOf<String?>(null) }
    var isError by remember { mutableStateOf(false) }

    // Acilista hastaneleri yukle
    LaunchedEffect(Unit) {
        try {
            val resp = RetrofitClient.api.getHospitals()
            if (resp.isSuccessful) hospitals = resp.body() ?: emptyList()
        } catch (_: Exception) {}
    }

    val fieldColors = OutlinedTextFieldDefaults.colors(
        focusedBorderColor = teal, unfocusedBorderColor = Color(0xFF26384C),
        focusedLabelColor = teal, unfocusedLabelColor = Color(0xFF5E7088),
        focusedTextColor = Color.White, unfocusedTextColor = Color.White,
        cursorColor = teal
    )

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Brush.verticalGradient(listOf(Color(0xFF0F1720), Color(0xFF16212E))))
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(24.dp)
        ) {
            Spacer(Modifier.height(12.dp))
            Text("Create Patient Account", color = Color.White, fontSize = 22.sp, fontWeight = FontWeight.Bold)
            Text("Fill in your details and select your doctor", color = Color(0xFF93A4B8), fontSize = 13.sp)
            Spacer(Modifier.height(22.dp))

            OutlinedTextField(username, { username = it }, label = { Text("Full name") },
                singleLine = true, modifier = Modifier.fillMaxWidth(), colors = fieldColors)
            Spacer(Modifier.height(12.dp))

            OutlinedTextField(email, { email = it }, label = { Text("Email") },
                singleLine = true, keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
                modifier = Modifier.fillMaxWidth(), colors = fieldColors)
            Spacer(Modifier.height(12.dp))

            OutlinedTextField(password, { password = it }, label = { Text("Password") },
                singleLine = true, visualTransformation = PasswordVisualTransformation(),
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
                modifier = Modifier.fillMaxWidth(), colors = fieldColors)
            Spacer(Modifier.height(12.dp))

            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
                OutlinedTextField(age, { age = it }, label = { Text("Age") },
                    singleLine = true, keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    modifier = Modifier.weight(1f), colors = fieldColors)
                OutlinedTextField(height, { height = it }, label = { Text("Height (cm)") },
                    singleLine = true, keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    modifier = Modifier.weight(1f), colors = fieldColors)
            }
            Spacer(Modifier.height(12.dp))

            OutlinedTextField(weight, { weight = it }, label = { Text("Weight (kg)") },
                singleLine = true, keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                modifier = Modifier.fillMaxWidth(), colors = fieldColors)
            Spacer(Modifier.height(12.dp))

            // Cinsiyet
            DropdownField("Gender", when (gender) { 1 -> "Male"; 2 -> "Female"; else -> "" },
                listOf("Male" to 1, "Female" to 2), teal) { gender = it }
            Spacer(Modifier.height(12.dp))

            // Kanser tipi
            DropdownField("Cancer type", when (cancerType) {
                1 -> "Lung"; 2 -> "Breast"; 3 -> "Colon"; 4 -> "Prostate"; 5 -> "Other"; else -> "" },
                listOf("Lung" to 1, "Breast" to 2, "Colon" to 3, "Prostate" to 4, "Other" to 5), teal) { cancerType = it }
            Spacer(Modifier.height(12.dp))

            // Tedavi tipi
            DropdownField("Treatment type", when (treatmentType) {
                1 -> "Chemotherapy"; 2 -> "Radiotherapy"; 3 -> "Immunotherapy"; 4 -> "Targeted therapy"; 5 -> "Hormone therapy"; else -> "" },
                listOf("Chemotherapy" to 1, "Radiotherapy" to 2, "Immunotherapy" to 3, "Targeted therapy" to 4, "Hormone therapy" to 5), teal) { treatmentType = it }
            Spacer(Modifier.height(12.dp))

            // Hastane secimi (gercek liste)
            DropdownField("Hospital", selectedHospital?.let { "${it.name} — ${it.city}" } ?: "",
                hospitals.map { ("${it.name} — ${it.city}") to it.id }, teal) { hid ->
                selectedHospital = hospitals.find { it.id == hid }
                selectedDoctor = null
                doctors = emptyList()
                // secilen hastanenin doktorlarini cek
                scope.launch {
                    try {
                        val resp = RetrofitClient.api.getDoctors(hid)
                        if (resp.isSuccessful) doctors = resp.body() ?: emptyList()
                    } catch (_: Exception) {}
                }
            }
            Spacer(Modifier.height(12.dp))

            // Doktor secimi (hastaneye gore)
            DropdownField(
                if (selectedHospital == null) "Doctor (select hospital first)" else "Doctor",
                selectedDoctor?.name ?: "",
                doctors.map { it.name to it.id }, teal
            ) { did -> selectedDoctor = doctors.find { it.id == did } }

            if (message != null) {
                Spacer(Modifier.height(14.dp))
                Text(message!!, color = if (isError) Color(0xFFEF4444) else Color(0xFF22C55E), fontSize = 13.sp)
            }

            Spacer(Modifier.height(22.dp))
            Button(
                onClick = {
                    message = null; isError = false
                    // dogrulama
                    if (username.isBlank() || email.isBlank() || password.isBlank() ||
                        age.isBlank() || height.isBlank() || weight.isBlank() ||
                        gender == 0 || cancerType == 0 || treatmentType == 0 ||
                        selectedHospital == null || selectedDoctor == null) {
                        message = "Please fill in all fields and make all selections."
                        isError = true
                        return@Button
                    }
                    loading = true
                    scope.launch {
                        try {
                            val req = RegisterPatientRequest(
                                username = username.trim(),
                                email = email.trim(),
                                password = password,
                                hospitalId = selectedHospital!!.id,
                                doctorId = selectedDoctor!!.id,
                                age = age.toIntOrNull() ?: 0,
                                gender = gender,
                                height = height.toDoubleOrNull() ?: 0.0,
                                weight = weight.toDoubleOrNull() ?: 0.0,
                                cancerType = cancerType,
                                treatmentType = treatmentType
                            )
                            val resp = RetrofitClient.api.registerPatient(req)
                            if (resp.isSuccessful) {
                                message = "Account created. You can sign in now."
                                isError = false
                                kotlinx.coroutines.delay(1200)
                                onRegisterSuccess()
                            } else {
                                message = "Registration failed. The email may already be in use."
                                isError = true
                            }
                        } catch (e: Exception) {
                            message = "Cannot reach the server. Is the backend running?"
                            isError = true
                        } finally {
                            loading = false
                        }
                    }
                },
                enabled = !loading,
                modifier = Modifier.fillMaxWidth().height(50.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(containerColor = teal)
            ) {
                if (loading) CircularProgressIndicator(Modifier.size(22.dp), color = Color.White, strokeWidth = 2.dp)
                else Text("Create Account", fontSize = 15.sp, fontWeight = FontWeight.SemiBold, color = Color(0xFF04130F))
            }

            Spacer(Modifier.height(16.dp))
            Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.Center) {
                Text("Already have an account? ", color = Color(0xFF93A4B8), fontSize = 13.sp)
                Text("Sign in", color = teal, fontSize = 13.sp, fontWeight = FontWeight.Bold,
                    modifier = Modifier.clickable { onGoLogin() })
            }
            Spacer(Modifier.height(24.dp))
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DropdownField(
    label: String,
    selectedText: String,
    options: List<Pair<String, Int>>,
    teal: Color,
    onSelect: (Int) -> Unit
) {
    var expanded by remember { mutableStateOf(false) }
    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { expanded = !expanded }) {
        OutlinedTextField(
            value = selectedText,
            onValueChange = {},
            readOnly = true,
            label = { Text(label) },
            trailingIcon = { Icon(Icons.Filled.ArrowDropDown, null) },
            modifier = Modifier
                .fillMaxWidth()
                .menuAnchor(),
            colors = OutlinedTextFieldDefaults.colors(
                focusedBorderColor = teal, unfocusedBorderColor = Color(0xFF26384C),
                focusedLabelColor = teal, unfocusedLabelColor = Color(0xFF5E7088),
                focusedTextColor = Color.White, unfocusedTextColor = Color.White,
                focusedTrailingIconColor = teal, unfocusedTrailingIconColor = Color(0xFF5E7088)
            )
        )
        ExposedDropdownMenu(expanded = expanded, onDismissRequest = { expanded = false }) {
            if (options.isEmpty()) {
                DropdownMenuItem(text = { Text("No options") }, onClick = { expanded = false })
            } else {
                options.forEach { (text, value) ->
                    DropdownMenuItem(text = { Text(text) }, onClick = {
                        onSelect(value); expanded = false
                    })
                }
            }
        }
    }
}
