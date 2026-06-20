package com.example.oncoguardpatient

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Email
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.launch

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun LoginScreen(
    onLoginSuccess: () -> Unit,
    onGoRegister: () -> Unit
) {
    var email by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    var loading by remember { mutableStateOf(false) }
    var error by remember { mutableStateOf<String?>(null) }
    val scope = rememberCoroutineScope()

    val tealDark = Color(0xFF0C6B62)
    val teal = Color(0xFF1FB6A6)

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Brush.verticalGradient(listOf(Color(0xFF0F1720), Color(0xFF16212E))))
            .padding(24.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(
            horizontalAlignment = Alignment.CenterHorizontally,
            modifier = Modifier.fillMaxWidth()
        ) {
            Box(
                modifier = Modifier
                    .size(72.dp)
                    .background(Brush.linearGradient(listOf(teal, tealDark)), RoundedCornerShape(20.dp)),
                contentAlignment = Alignment.Center
            ) {
                Text("🛡", fontSize = 34.sp)
            }
            Spacer(Modifier.height(18.dp))
            Text("ONCOGUARD-AI", color = Color.White, fontSize = 22.sp, fontWeight = FontWeight.Bold)
            Text("Patient Portal", color = teal, fontSize = 14.sp)
            Spacer(Modifier.height(6.dp))
            Text(
                "Sign in to record your daily health updates",
                color = Color(0xFF93A4B8), fontSize = 13.sp, textAlign = TextAlign.Center
            )
            Spacer(Modifier.height(30.dp))

            OutlinedTextField(
                value = email,
                onValueChange = { email = it },
                label = { Text("Email") },
                leadingIcon = { Icon(Icons.Filled.Email, null) },
                singleLine = true,
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Email),
                modifier = Modifier.fillMaxWidth(),
                colors = OutlinedTextFieldDefaults.colors(
                    focusedBorderColor = teal,
                    unfocusedBorderColor = Color(0xFF26384C),
                    focusedLabelColor = teal,
                    unfocusedLabelColor = Color(0xFF5E7088),
                    focusedTextColor = Color.White,
                    unfocusedTextColor = Color.White,
                    focusedLeadingIconColor = teal,
                    unfocusedLeadingIconColor = Color(0xFF5E7088),
                    cursorColor = teal
                )
            )
            Spacer(Modifier.height(14.dp))
            OutlinedTextField(
                value = password,
                onValueChange = { password = it },
                label = { Text("Password") },
                leadingIcon = { Icon(Icons.Filled.Lock, null) },
                singleLine = true,
                visualTransformation = PasswordVisualTransformation(),
                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Password),
                modifier = Modifier.fillMaxWidth(),
                colors = OutlinedTextFieldDefaults.colors(
                    focusedBorderColor = teal,
                    unfocusedBorderColor = Color(0xFF26384C),
                    focusedLabelColor = teal,
                    unfocusedLabelColor = Color(0xFF5E7088),
                    focusedTextColor = Color.White,
                    unfocusedTextColor = Color.White,
                    focusedLeadingIconColor = teal,
                    unfocusedLeadingIconColor = Color(0xFF5E7088),
                    cursorColor = teal
                )
            )

            if (error != null) {
                Spacer(Modifier.height(12.dp))
                Text(error!!, color = Color(0xFFEF4444), fontSize = 13.sp)
            }

            Spacer(Modifier.height(22.dp))
            Button(
                onClick = {
                    error = null
                    if (email.isBlank() || password.isBlank()) {
                        error = "Email and password are required."
                        return@Button
                    }
                    loading = true
                    scope.launch {
                        try {
                            val resp = RetrofitClient.api.login(LoginRequest(email.trim(), password))
                            if (resp.isSuccessful && resp.body() != null) {
                                AppState.token = resp.body()!!.token
                                AppState.patientId = resp.body()!!.patientId
                                AppState.patientName = email.substringBefore("@")
                                onLoginSuccess()
                            } else {
                                error = "Sign-in failed. Please check your credentials."
                            }
                        } catch (e: Exception) {
                            error = "Cannot reach the server. Is the backend running?"
                        } finally {
                            loading = false
                        }
                    }
                },
                enabled = !loading,
                modifier = Modifier
                    .fillMaxWidth()
                    .height(50.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(containerColor = teal)
            ) {
                if (loading) CircularProgressIndicator(Modifier.size(22.dp), color = Color.White, strokeWidth = 2.dp)
                else Text("Sign In", fontSize = 15.sp, fontWeight = FontWeight.SemiBold, color = Color(0xFF04130F))
            }

            Spacer(Modifier.height(20.dp))
            Row {
                Text("Don't have an account? ", color = Color(0xFF93A4B8), fontSize = 13.sp)
                Text(
                    "Register",
                    color = teal,
                    fontSize = 13.sp,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.clickable(
                        interactionSource = remember { MutableInteractionSource() },
                        indication = null
                    ) { onGoRegister() }
                )
            }
        }
    }
}