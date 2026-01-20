# MAUI Map Polygon Opacity Demo

A minimal reproduction project for investigating .NET MAUI map polygon opacity rendering issues.

## The Issue

When adding polygons to a .NET MAUI `Map` control with semi-transparent `FillColor` values (alpha < 1.0), the polygons may render with **incorrect opacity** - often appearing fully opaque even though the alpha channel is correctly set.

### Expected Behavior
Polygons should render with the specified transparency:
- Red polygon: 25% opacity (very transparent, map clearly visible through it)
- Green polygon: 50% opacity (semi-transparent)
- Blue polygon: 75% opacity (mostly opaque but some map visible)

### Actual Behavior
All polygons may render as fully opaque (100%), ignoring the alpha channel.

## Related GitHub Issues

- [#18234](https://github.com/dotnet/maui/issues/18234) - Android Opacity not set at startup
- [#22510](https://github.com/dotnet/maui/issues/22510) - Multiple polylines corruption (handler state issues)
- [#9759](https://github.com/dotnet/maui/issues/9759) - Polyline Fill not working (fixed in .NET 7/8)
- [#1711](https://github.com/CommunityToolkit/Maui/issues/1711) - Map elements never render (Windows/CommunityToolkit)

## Setup

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code with MAUI workload

### Platform-Specific Requirements

#### Android
1. Android SDK installed
2. Get a Google Maps API key from the [Google Cloud Console](https://console.cloud.google.com/)
3. Enable "Maps SDK for Android"
4. Replace `YOUR_GOOGLE_MAPS_API_KEY_HERE` in `Platforms/Android/AndroidManifest.xml`

#### iOS (Mac only)
1. Xcode installed
2. Apple Maps is used automatically (no API key required)

#### Windows
1. Windows 10/11 with Developer Mode enabled
2. Bing Maps is used automatically (no API key required for basic usage)
3. Note: Map elements may have different behavior on Windows - see [CommunityToolkit issue #1711](https://github.com/CommunityToolkit/Maui/issues/1711)

### Build and Run

```bash
# Restore dependencies
dotnet restore

# Build and run for Windows
dotnet build -f net9.0-windows10.0.19041.0
dotnet run -f net9.0-windows10.0.19041.0

# Build for Android
dotnet build -f net9.0-android

# Run on Android emulator/device
dotnet build -t:Run -f net9.0-android

# Build for iOS (Mac only)
dotnet build -f net9.0-ios

# Run on iOS simulator (Mac only)
dotnet build -t:Run -f net9.0-ios
```

## Test Scenarios

The demo app provides four buttons to test different scenarios:

1. **Add Polygons** - Adds three polygons immediately. This often triggers the opacity bug.

2. **Add with Delay** - Adds polygons after a 500ms delay. Some users report this helps.

3. **Nudge Map** - Moves the map by ~1 meter to force a redraw. This workaround may fix the opacity after polygons are added.

4. **Clear** - Removes all polygons (with handler disconnect workaround).

## Workarounds Attempted

### 1. Delay before adding polygons
```csharp
await Task.Delay(500);
AddPolygons();
```

### 2. Map nudge to force redraw
```csharp
var region = map.VisibleRegion;
var nudged = new MapSpan(
    new Location(region.Center.Latitude + 0.00001, region.Center.Longitude),
    region.LatitudeDegrees, region.LongitudeDegrees);
map.MoveToRegion(nudged);
```

### 3. Disconnect handlers before clearing
```csharp
foreach (var element in map.MapElements.ToList())
    element.Handler?.DisconnectHandler();
map.MapElements.Clear();
```

## Platform Notes

| Platform | Map Provider | Polygon Support | Notes |
|----------|--------------|-----------------|-------|
| Android | Google Maps | **Yes** | Requires API key - **use for testing** |
| iOS | Apple Maps | **Yes** | No API key needed - **use for testing** |
| Windows | Bing Maps | **NO** | `NotImplementedException` - cannot test |

### Windows Limitation

**Windows cannot be used to test this issue.** The MAUI Maps control on Windows throws `NotImplementedException` when adding any `MapElement` (Polygon, Polyline, Circle). This is a known limitation documented in [CommunityToolkit issue #1711](https://github.com/CommunityToolkit/Maui/issues/1711).

The maintainer confirmed: *"From looking at the code this was indeed never implemented on our side for Windows."*

**You must test on Android or iOS to reproduce the polygon opacity issue.**

## Environment

- .NET MAUI 9.0.0
- Microsoft.Maui.Controls.Maps 9.0.0
- Target Frameworks: net9.0-android, net9.0-ios, net9.0-windows10.0.19041.0

## Contributing

If you can reproduce or have additional findings, please share:
1. Device/emulator details and OS version
2. Exact MAUI version
3. Screenshots showing the issue
4. Any workarounds that helped
5. Debug output from the polygon color logging
