package com.example.oncoguardpatient

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path

interface ApiService {

    // Giris -> token doner
    @POST("api/Auth/login")
    suspend fun login(@Body request: LoginRequest): Response<LoginResponse>

    // Hasta kaydi
    @POST("api/Auth/register-patient")
    suspend fun registerPatient(@Body request: RegisterPatientRequest): Response<Unit>

    // Tum hastaneler (kayit ekraninda secim)
    @GET("api/Hospitals")
    suspend fun getHospitals(): Response<List<HospitalDto>>

    // Secilen hastanenin doktorlari
    @GET("api/Hospitals/{hospitalId}/doctors")
    suspend fun getDoctors(@Path("hospitalId") hospitalId: Int): Response<List<DoctorDto>>

    @GET("api/PatientDailyEntryStatus/patient/{patientId}")
    suspend fun getPatientDailyEntryStatus(
        @Path("patientId") patientId: Int
    ): Response<PatientDailyEntryStatusResponse>


    @GET("api/DailyLogs/patient/{patientId}/calendar")
    suspend fun getDailyEntryCalendar(
        @Path("patientId") patientId: Int
    ): Response<DailyEntryCalendarResponse>

    @GET("api/DailyLogs/patient/{patientId}/date/{date}")
    suspend fun getDailyLogByDate(
        @Path("patientId") patientId: Int,
        @Path("date") date: String
    ): Response<DailyLogDetailResponse>

    @POST("api/DailyLogs")
    suspend fun createDailyLog(
        @Body request: CreateDailyLogRequest
    ): Response<CreateDailyLogResponse>

    @POST("api/FoodLogs")
    suspend fun createFoodLog(
        @Body request: CreateFoodLogRequest
    ): Response<Unit>
}