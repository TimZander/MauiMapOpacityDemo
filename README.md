# MAUI Maps Polygon Bug Reproduction

**Minimal reproduction project demonstrating two critical bugs in .NET MAUI Maps on Android.**

## Summary

The `Microsoft.Maui.Controls.Maps.Map` control on Android has two bugs where MAUI object state does not sync to the native Google Map:

1. **BUG 1**: `MapElements.Clear()` removes items from the collection (Count becomes 0), but the native Google Map still displays the polygon visuals.

2. **BUG 2**: Changing polygon properties (`FillColor`, `StrokeColor`) updates the MAUI object, but the native Google Map polygon is not updated to reflect the changes.

Both bugs appear to be in the `MapElementHandler` which is not forwarding changes to the native platform.

## Reproduction Steps

### BUG 1: Clear() doesn't remove native polygons

1. Run the app on Android
2. Tap "1. Add 3 Polygons" - three colored polygons appear on the map
3. Tap "2. Clear()" - status shows "Count = 0"
4. **Observe**: Polygons are still visible on the map despite Count being 0

### BUG 2: Property changes don't update native polygons

1. Run the app on Android
2. Tap "1. Add Polygons" - three colored polygons appear
3. Tap "2. Set Alpha=0" - this sets `FillColor.Alpha` to 0.0 on all polygons
4. Tap "3. Diagnostics" - console shows `FillColor.Alpha = 0.00` for all polygons
5. **Observe**: Polygons are still visible despite MAUI property being 0

## Expected vs Actual Behavior

| Action | Expected | Actual |
|--------|----------|--------|
| `MapElements.Clear()` | Polygons removed from map | Polygons remain visible |
| `polygon.FillColor = transparent` | Polygon becomes invisible | Polygon unchanged |
| `MapElements.Count` after Clear() | 0 | 0 (correct) |
| `polygon.FillColor.Alpha` after setting to 0 | 0.00 | 0.00 (correct) |

The MAUI objects are updated correctly. The bug is that changes are not propagated to the native Google Map.

## Diagnostic Output

After adding polygons, then setting alpha to 0:

```
=== DIAGNOSTICS ===
MapElements.Count: 3
Map.Handler: MapHandler
[0] Polygon:
    FillColor: R=1.00 G=0.00 B=0.00 A=0.00   <-- Alpha is 0, but polygon still visible!
    StrokeColor: R=0.55 G=0.00 B=0.00 A=0.00
    Handler: MapElementHandler
[1] Polygon:
    FillColor: R=0.00 G=1.00 B=0.00 A=0.00
    ...
```

## Related GitHub Issues

- [#18234](https://github.com/dotnet/maui/issues/18234) - Android Opacity not set at startup
- [#22510](https://github.com/dotnet/maui/issues/22510) - Multiple polylines corruption (handler state issues)
- [#9759](https://github.com/dotnet/maui/issues/9759) - Polyline Fill not working (fixed in .NET 7/8)
- [#1711](https://github.com/CommunityToolkit/Maui/issues/1711) - Map elements never render (Windows/CommunityToolkit)

## Setup

### Prerequisites
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code with MAUI workload
- Android SDK
- Google Maps API key from [Google Cloud Console](https://console.cloud.google.com/)

### Configuration

1. Get a Google Maps API key and enable "Maps SDK for Android"
2. Replace the API key in `Platforms/Android/AndroidManifest.xml`:
   ```xml
   <meta-data android:name="com.google.android.geo.API_KEY" android:value="YOUR_KEY_HERE" />
   ```

### Build and Run

```bash
cd MauiMapOpacityDemo

# Build for Android
dotnet build -f net9.0-android

# Run on Android emulator/device
dotnet build -t:Run -f net9.0-android
```

## Workarounds Attempted (All Failed)

We tested many workarounds - none successfully clear the "ghost" polygons via MAUI APIs:

| Workaround | Result |
|------------|--------|
| `Handler?.DisconnectHandler()` before Clear() | No effect |
| Toggle `Map.IsVisible` | No effect |
| Cycle `Map.MapType` | No effect |
| Move map far away with `MoveToRegion()` | No effect |
| Nudge map position slightly | No effect |
| Remove elements one-by-one instead of Clear() | No effect |
| Set polygon colors to transparent before Clear() | No effect (BUG 2 - property changes don't sync) |
| Create entirely new Map control | Failed with MauiContext error |

### Note on Issue #30097 Workaround

The workaround described in [#30097](https://github.com/dotnet/maui/issues/30097) sets `Visible = false` and calls `Remove()` on elements. This works because it accesses **native Google Maps objects directly**, not the MAUI `MapElement` abstraction.

MAUI's `MapElement` inherits from `Element` (not `VisualElement`), so it has no `IsVisible` property. To use the #30097 workaround, you must write a custom handler that accesses native platform objects.

## Platform Notes

| Platform | Can Reproduce Bug? | Notes |
|----------|-------------------|-------|
| Android | **Yes** | Use this for testing |
| iOS | Untested | May have same issue |
| Windows | **No** | MapElements throw `NotImplementedException` |

## Environment

- .NET MAUI 9.0.0
- Microsoft.Maui.Controls.Maps 9.0.0
- Tested on: Android emulator (API 34)
- Target Framework: net9.0-android
