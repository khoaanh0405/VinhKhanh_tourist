import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'providers/app_provider.dart';
import 'providers/audio_player_provider.dart';
import 'providers/geofence_provider.dart';
import 'providers/language_provider.dart';
import 'screens/main_screen.dart';
import 'package:vinh_khanh_tourism/core/constants/app_constants.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const VinhKhanhTourismApp());
}

class VinhKhanhTourismApp extends StatelessWidget {
  const VinhKhanhTourismApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => LanguageProvider()),
        ChangeNotifierProvider(create: (_) => AppProvider()),
        ChangeNotifierProvider(create: (_) => AudioPlayerProvider()),
        ChangeNotifierProvider(create: (_) => GeofenceProvider()),
      ],
      child: Consumer<LanguageProvider>(
        builder: (context, languageProvider, child) {
          return MaterialApp(
            title: AppConstants.appName,
            debugShowCheckedModeBanner: false,
            theme: ThemeData(
              primaryColor: AppConstants.primaryColor,
              colorScheme: ColorScheme.fromSeed(
                seedColor: AppConstants.primaryColor,
                brightness: Brightness.light,
              ),
              fontFamily: 'Roboto',
              useMaterial3: true,
              appBarTheme: const AppBarTheme(
                centerTitle: true,
                elevation: 0,
              ),
            ),
            locale: languageProvider.currentLocale,
            supportedLocales: const [
              Locale('vi', 'VN'),
              Locale('en', 'US'),
              Locale('zh', 'CN'),
              Locale('ja', 'JP'),
              Locale('ko', 'KR'),
            ],
            localizationsDelegates: const [
              GlobalMaterialLocalizations.delegate,
              GlobalWidgetsLocalizations.delegate,
              GlobalCupertinoLocalizations.delegate,
            ],
            home: const MainScreen(),
          );
        },
      ),
    );
  }
}
