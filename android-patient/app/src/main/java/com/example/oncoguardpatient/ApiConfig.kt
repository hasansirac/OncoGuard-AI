package com.example.oncoguardpatient

/**
 * Backend adresi tek yerden yonetilir.
 *
 * EMULATOR icin:  http://10.0.2.2:5080/   (bilgisayarin localhost'u)
 * TELEFON icin:   http://<BILGISAYAR_IP>:5080/   (orn: http://192.168.1.23:5080/)
 *
 * Emulator'de calistirirken EMULATOR satirini,
 * telefonda calistirirken TELEFON satirini aktif birak.
 */
object ApiConfig {

    // === EMULATOR (varsayilan) ===
    const val BASE_URL = "http://10.0.2.2:5080/"

    // === TELEFON (kullanirken ustteki satiri // ile kapat, bunu ac + kendi IP'ni yaz) ===
    // const val BASE_URL = "http://192.168.1.23:5080/"
}