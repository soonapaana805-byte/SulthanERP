import 'package:flutter/material.dart';

import 'features/auth/login_page.dart';
import 'features/auth/session.dart';
import 'features/captain/table_dashboard_page.dart';

class CaptainApp extends StatefulWidget {
  const CaptainApp({super.key});

  @override
  State<CaptainApp> createState() => _CaptainAppState();
}

class _CaptainAppState extends State<CaptainApp> {
  CaptainSession? _session;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'Sulthan ERP Captain',
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF0F172A),
          primary: const Color(0xFF0F172A),
          secondary: const Color(0xFF0284C7),
        ),
        scaffoldBackgroundColor: const Color(0xFFF1F5F9),
        appBarTheme: const AppBarTheme(
          backgroundColor: Color(0xFF0F172A),
          foregroundColor: Colors.white,
          centerTitle: false,
        ),
        cardTheme: const CardThemeData(elevation: 0, margin: EdgeInsets.zero),
        inputDecorationTheme: const InputDecorationTheme(
          border: OutlineInputBorder(),
          filled: true,
          fillColor: Colors.white,
        ),
        filledButtonTheme: FilledButtonThemeData(
          style: FilledButton.styleFrom(
            minimumSize: const Size(0, 48),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(10),
            ),
          ),
        ),
      ),
      home: _session == null
          ? LoginPage(
              onLoggedIn: (session) => setState(() => _session = session),
            )
          : TableDashboardPage(
              session: _session!,
              onLogout: () => setState(() => _session = null),
            ),
    );
  }
}
