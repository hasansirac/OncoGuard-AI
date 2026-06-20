package com.example.oncoguardpatient

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue

/**
 * Uygulamanin basit hafizasi: giris yapan kullanicinin token'i
 * ve hangi ekranda oldugumuz burada tutulur.
 */
object AppState {
    var token by mutableStateOf<String?>(null)
    var patientName by mutableStateOf("")
    var patientId by mutableStateOf<Int?>(null)
}

/** Hangi ekranda oldugumuzu belirten basit liste */
enum class Screen {
    LOGIN, REGISTER, HOME, CONFIRM
}