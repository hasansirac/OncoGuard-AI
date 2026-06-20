package com.example.oncoguardpatient

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp

@Composable
fun ConfirmScreen(onBackHome: () -> Unit, onLogout: () -> Unit) {
    val teal = Color(0xFF1FB6A6)
    Box(
        Modifier.fillMaxSize()
            .background(Brush.verticalGradient(listOf(Color(0xFF0F1720), Color(0xFF16212E))))
            .padding(28.dp),
        contentAlignment = Alignment.Center
    ) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Box(
                Modifier.size(96.dp).background(teal.copy(alpha = 0.15f), CircleShape),
                contentAlignment = Alignment.Center
            ) {
                Box(
                    Modifier.size(64.dp).background(teal, CircleShape),
                    contentAlignment = Alignment.Center
                ) {
                    Icon(Icons.Filled.Check, null, tint = Color(0xFF04130F), modifier = Modifier.size(36.dp))
                }
            }
            Spacer(Modifier.height(24.dp))
            Text("Thank you!", color = Color.White, fontSize = 24.sp, fontWeight = FontWeight.Bold)
            Spacer(Modifier.height(10.dp))
            Text(
                "Your data has been recorded and shared with your doctor.",
                color = Color(0xFF93A4B8), fontSize = 15.sp,
                textAlign = TextAlign.Center, lineHeight = 22.sp
            )
            Spacer(Modifier.height(8.dp))
            Text(
                "Your doctor will review it and contact you if needed.",
                color = Color(0xFF5E7088), fontSize = 13.sp,
                textAlign = TextAlign.Center, lineHeight = 20.sp
            )
            Spacer(Modifier.height(36.dp))
            Button(
                onClick = onBackHome,
                modifier = Modifier.fillMaxWidth().height(50.dp),
                shape = RoundedCornerShape(12.dp),
                colors = ButtonDefaults.buttonColors(containerColor = teal)
            ) {
                Text("Back to Home", fontSize = 15.sp, fontWeight = FontWeight.SemiBold, color = Color(0xFF04130F))
            }
            Spacer(Modifier.height(12.dp))
            TextButton(onClick = onLogout) {
                Text("Sign Out", color = Color(0xFF93A4B8), fontSize = 14.sp)
            }
        }
    }
}