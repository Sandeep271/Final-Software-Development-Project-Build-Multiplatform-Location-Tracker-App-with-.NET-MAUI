# Location Heat Map App (.NET MAUI)

A C# .NET MAUI project that records foreground device locations, stores them in SQLite, and displays the saved route as a heat-map-style overlay on the .NET MAUI `Map` control.

## Included source files

- `LocationHeatMapApp.csproj`
- `App.xaml` / `App.xaml.cs`
- `MauiProgram.cs`
- `MainPage.xaml` / `MainPage.xaml.cs`
- `Models/LocationRecord.cs`
- `Data/LocationDatabase.cs`
- `Services/LocationTrackingService.cs`
- `Services/HeatMapService.cs`
- Android manifest/activity/application files
- iOS Info.plist/app delegate/program files

## Features

- Foreground location tracking with .NET MAUI Geolocation APIs.
- Runtime `LocationWhenInUse` permission handling.
- SQLite persistence using `sqlite-net-pcl`.
- Density-based heat map built from `Circle` map elements.
- Route `Polyline` to show movement order.
- Start, stop, refresh, and clear-history controls.
- Android Google Maps and iOS Apple Maps support.

## Prerequisites

1. Visual Studio with the .NET MAUI workload installed.
2. .NET 10 SDK / .NET MAUI 10.
3. Android SDK and an emulator or Android device, or a Mac build host for iOS.
4. For Android, a Google Maps SDK for Android API key.

## Configure Android Maps

Edit `Platforms/Android/AndroidManifest.xml` and replace:

```xml
android:value="PASTE-YOUR-GOOGLE-MAPS-API-KEY-HERE"
```

with your own restricted Google Maps API key. Do not commit a real unrestricted key to a public repository.

## Run

1. Open `LocationHeatMapApp.csproj` in Visual Studio.
2. Restore NuGet packages.
3. Select an Android emulator/device or iOS target.
4. Run the app.
5. Grant location permission.
6. Tap **Start Tracking** and move around, or use the emulator's simulated GPS controls.
7. Tap **Refresh Map** to redraw the saved heat map.

## Heat-map algorithm

The app groups nearby points into small geographic grid cells. Each cell becomes a semi-transparent circle. Cells with more recorded points get larger and use progressively warmer colors (blue -> cyan -> green -> yellow -> red). A route polyline is also drawn beneath the heat circles.

## Privacy note

Location data is stored only in the app's local SQLite database. This sample does not upload location data to a server. Use an appropriate privacy notice and retention policy before adapting it for production.

## Verified runtime test

The Android build was compiled successfully in Visual Studio 2026 and deployed to a Pixel 7 Android 16 (API 36) emulator. End-to-end testing verified Google Maps rendering, runtime location permission, foreground tracking, SQLite persistence, GPX route simulation, map refresh, route rendering, and heat-map density circles. The final route test stored and displayed 152 location points.

The locally used Google Maps API key is **not** included in this repository. Keep the placeholder in `AndroidManifest.xml` for public source control and add a restricted key only in a local development copy.

## Test evidence

- `Evidence/LocationHeatMapApp_Actual_Output.png` — actual Android emulator output after the route test.
- `TestData/LocationHeatMapApp_Test_Route.gpx` — GPX route used for repeatable emulator testing.

## GitHub repository

https://github.com/Sandeep271/Final-Software-Development-Project-Build-Multiplatform-Location-Tracker-App-with-.NET-MAUI

## References

- .NET MAUI Map: https://learn.microsoft.com/dotnet/maui/user-interface/controls/map
- .NET MAUI Geolocation: https://learn.microsoft.com/dotnet/maui/platform-integration/device/geolocation
- C# coding conventions: https://learn.microsoft.com/dotnet/csharp/fundamentals/coding-style/coding-conventions
- sqlite-net-pcl: https://www.nuget.org/packages/sqlite-net-pcl/
