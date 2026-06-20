package com.example.oncoguardpatient

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.foundation.layout.padding
import androidx.compose.ui.unit.dp
class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            MaterialTheme {
                Surface(
                    modifier = Modifier.fillMaxSize(),
                    color = Color(0xFF0F1720)
                ) {
                    AppRoot()
                }
            }
        }
    }
}

@Composable
fun AppRoot() {
    // Hangi ekrandayiz?
    var screen by remember { mutableStateOf(Screen.LOGIN) }

    when (screen) {
        Screen.LOGIN -> LoginScreen(
            onLoginSuccess = { screen = Screen.HOME },
            onGoRegister = { screen = Screen.REGISTER }
        )

        Screen.REGISTER -> RegisterScreen(
            onRegisterSuccess = { screen = Screen.LOGIN },
            onGoLogin = { screen = Screen.LOGIN }
        )

        Screen.HOME -> HomeScreen(
            onSubmitted = { screen = Screen.CONFIRM },
            onLogout = {
                AppState.token = null
                AppState.patientId = null
                screen = Screen.LOGIN
            }
        )

        Screen.CONFIRM -> ConfirmScreen(
            onBackHome = { screen = Screen.HOME },
            onLogout = {
                AppState.token = null
                AppState.patientId = null
                screen = Screen.LOGIN
            }
        )
    }
    @androidx.compose.runtime.Composable
    fun TempScreen(message: String, onBack: () -> Unit) {
        androidx.compose.foundation.layout.Box(
            modifier = androidx.compose.ui.Modifier.fillMaxSize(),
            contentAlignment = androidx.compose.ui.Alignment.Center
        ) {
            androidx.compose.foundation.layout.Column(
                horizontalAlignment = androidx.compose.ui.Alignment.CenterHorizontally
            ) {
                androidx.compose.material3.Text(
                    message,
                    color = Color.White,
                    modifier = androidx.compose.ui.Modifier.padding(24.dp)
                )
                androidx.compose.material3.Button(onClick = onBack) {
                    androidx.compose.material3.Text("Back to Login")
                }
            }
        }
    }
}