using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using Map = Microsoft.Maui.Controls.Maps.Map;

namespace MauiMapOpacityDemo;

/// <summary>
/// Demo page to reproduce MAUI map polygon opacity rendering issues.
///
/// ISSUE: Polygons added to a MAUI Map control may render with incorrect opacity,
/// appearing fully opaque even when FillColor has an alpha channel less than 1.0.
///
/// This demo tests various scenarios:
/// 1. Adding polygons immediately
/// 2. Adding polygons after a delay
/// 3. "Nudging" the map to force a redraw
/// 4. Clearing and re-adding polygons
///
/// NOTE: Windows does not support MapElements (Polygon, Polyline, Circle).
/// This is a known limitation - see https://github.com/CommunityToolkit/Maui/issues/1711
/// </summary>
public partial class MainPage : ContentPage
{
    // Denver, CO area - center point for demo
    private const double CenterLat = 39.7392;
    private const double CenterLon = -104.9903;

    private bool _isWindows;

    public MainPage()
    {
        InitializeComponent();
        _isWindows = DeviceInfo.Platform == DevicePlatform.WinUI;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Center map on Denver
        Location center = new Location(CenterLat, CenterLon);
        MapSpan mapSpan = MapSpan.FromCenterAndRadius(center, Distance.FromMiles(15));
        TestMap.MoveToRegion(mapSpan);

        if (_isWindows)
        {
            StatusLabel.Text = "WARNING: Windows does not support map polygons. Test on Android/iOS.";
            StatusLabel.TextColor = Colors.Red;
            PolygonInfoLabel.Text = "MapElements (Polygon, Polyline, Circle) throw NotImplementedException on Windows.";
        }
        else
        {
            StatusLabel.Text = "Map centered on Denver, CO. Tap 'Add Polygons' to test.";
        }
    }

    /// <summary>
    /// Test 1: Add polygons immediately without any delay.
    /// This is the simplest case and often shows the opacity bug.
    /// </summary>
    private async void OnAddPolygonsClicked(object? sender, EventArgs e)
    {
        if (_isWindows)
        {
            await DisplayAlert("Not Supported",
                "Windows does not support map polygons.\n\n" +
                "MapElements (Polygon, Polyline, Circle) are not implemented for the Windows platform.\n\n" +
                "Please test on Android or iOS.", "OK");
            return;
        }

        ClearPolygons();
        AddTestPolygons();
        StatusLabel.Text = $"Added 3 polygons at {DateTime.Now:HH:mm:ss}. Check if opacity is correct.";
    }

    /// <summary>
    /// Test 2: Add polygons after a delay.
    /// Some users report this helps with the opacity issue.
    /// </summary>
    private async void OnAddWithDelayClicked(object? sender, EventArgs e)
    {
        if (_isWindows)
        {
            await DisplayAlert("Not Supported",
                "Windows does not support map polygons. Please test on Android or iOS.", "OK");
            return;
        }

        ClearPolygons();
        StatusLabel.Text = "Waiting 500ms before adding polygons...";

        await Task.Delay(500);

        AddTestPolygons();
        StatusLabel.Text = $"Added 3 polygons after delay at {DateTime.Now:HH:mm:ss}. Check opacity.";
    }

    /// <summary>
    /// Test 3: Nudge the map slightly to force a redraw.
    /// This workaround may help re-render polygons with correct opacity.
    /// </summary>
    private void OnNudgeMapClicked(object? sender, EventArgs e)
    {
        MapSpan? currentRegion = TestMap.VisibleRegion;
        if (currentRegion != null)
        {
            // Nudge by a tiny amount (0.00001 degrees ≈ 1 meter)
            Location nudgedCenter = new Location(
                currentRegion.Center.Latitude + 0.00001,
                currentRegion.Center.Longitude);
            MapSpan nudgedRegion = new MapSpan(
                nudgedCenter,
                currentRegion.LatitudeDegrees,
                currentRegion.LongitudeDegrees);
            TestMap.MoveToRegion(nudgedRegion);

            StatusLabel.Text = $"Map nudged at {DateTime.Now:HH:mm:ss}. Check if opacity changed.";
        }
    }

    /// <summary>
    /// Clear all polygons from the map.
    /// </summary>
    private void OnClearClicked(object? sender, EventArgs e)
    {
        int beforeCount = TestMap.MapElements.Count;
        ClearPolygons();
        int afterCount = TestMap.MapElements.Count;
        StatusLabel.Text = $"Clear: {beforeCount} → {afterCount} elements. Visual count may differ (BUG).";
    }

    private void ClearPolygons()
    {
        System.Diagnostics.Debug.WriteLine($"[Clear] Before: {TestMap.MapElements.Count} elements");

        // Try disconnecting handlers first (workaround for Issue #22510)
        foreach (MapElement element in TestMap.MapElements.ToList())
        {
            System.Diagnostics.Debug.WriteLine($"[Clear] Disconnecting handler for {element.GetType().Name}");
            element.Handler?.DisconnectHandler();
        }

        TestMap.MapElements.Clear();
        System.Diagnostics.Debug.WriteLine($"[Clear] After Clear(): {TestMap.MapElements.Count} elements");
    }

    /// <summary>
    /// Alternative clear: Remove elements one by one.
    /// </summary>
    private void OnClearOneByOneClicked(object? sender, EventArgs e)
    {
        int beforeCount = TestMap.MapElements.Count;

        System.Diagnostics.Debug.WriteLine($"[ClearOneByOne] Before: {beforeCount} elements");

        while (TestMap.MapElements.Count > 0)
        {
            MapElement element = TestMap.MapElements[0];
            System.Diagnostics.Debug.WriteLine($"[ClearOneByOne] Removing {element.GetType().Name}");
            element.Handler?.DisconnectHandler();
            TestMap.MapElements.RemoveAt(0);
        }

        int afterCount = TestMap.MapElements.Count;
        System.Diagnostics.Debug.WriteLine($"[ClearOneByOne] After: {afterCount} elements");

        StatusLabel.Text = $"Clear 1-by-1: {beforeCount} → {afterCount} elements. Check visuals.";
    }

    /// <summary>
    /// Show current element count.
    /// </summary>
    private void OnCountClicked(object? sender, EventArgs e)
    {
        int count = TestMap.MapElements.Count;
        StatusLabel.Text = $"MapElements.Count = {count}";
        System.Diagnostics.Debug.WriteLine($"[Count] MapElements.Count = {count}");
    }

    /// <summary>
    /// Add three test polygons with different opacity levels.
    ///
    /// Expected result:
    /// - Red polygon: 25% opacity (0.25 alpha) - should be very transparent
    /// - Green polygon: 50% opacity (0.50 alpha) - should be semi-transparent
    /// - Blue polygon: 75% opacity (0.75 alpha) - should be mostly opaque
    ///
    /// BUG: All three may render as fully opaque (100% opacity).
    /// </summary>
    private void AddTestPolygons()
    {
        // Polygon 1: RED with 25% opacity
        Polygon redPolygon = CreatePolygon(
            centerLat: CenterLat + 0.05,
            centerLon: CenterLon - 0.08,
            fillColor: Color.FromRgba(1.0, 0.0, 0.0, 0.25),  // Red, 25% opacity
            strokeColor: Colors.DarkRed,
            label: "Red 25%");

        // Polygon 2: GREEN with 50% opacity
        Polygon greenPolygon = CreatePolygon(
            centerLat: CenterLat + 0.05,
            centerLon: CenterLon + 0.02,
            fillColor: Color.FromRgba(0.0, 1.0, 0.0, 0.50),  // Green, 50% opacity
            strokeColor: Colors.DarkGreen,
            label: "Green 50%");

        // Polygon 3: BLUE with 75% opacity
        Polygon bluePolygon = CreatePolygon(
            centerLat: CenterLat - 0.05,
            centerLon: CenterLon - 0.03,
            fillColor: Color.FromRgba(0.0, 0.0, 1.0, 0.75),  // Blue, 75% opacity
            strokeColor: Colors.DarkBlue,
            label: "Blue 75%");

        // Log the actual color values for debugging
        LogPolygonColor("Red", redPolygon.FillColor);
        LogPolygonColor("Green", greenPolygon.FillColor);
        LogPolygonColor("Blue", bluePolygon.FillColor);

        // Add to map
        TestMap.MapElements.Add(redPolygon);
        TestMap.MapElements.Add(greenPolygon);
        TestMap.MapElements.Add(bluePolygon);
    }

    private Polygon CreatePolygon(double centerLat, double centerLon, Color fillColor, Color strokeColor, string label)
    {
        // Create a simple square polygon
        double size = 0.04; // ~4km

        Polygon polygon = new Polygon
        {
            FillColor = fillColor,
            StrokeColor = strokeColor,
            StrokeWidth = 3
        };

        // Add corners (square shape)
        polygon.Geopath.Add(new Location(centerLat + size, centerLon - size)); // Top-left
        polygon.Geopath.Add(new Location(centerLat + size, centerLon + size)); // Top-right
        polygon.Geopath.Add(new Location(centerLat - size, centerLon + size)); // Bottom-right
        polygon.Geopath.Add(new Location(centerLat - size, centerLon - size)); // Bottom-left

        return polygon;
    }

    private void LogPolygonColor(string name, Color color)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[PolygonOpacity] {name} polygon - R:{color.Red:F2} G:{color.Green:F2} B:{color.Blue:F2} A:{color.Alpha:F2}");
    }
}
