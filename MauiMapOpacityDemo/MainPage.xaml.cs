using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace MauiMapOpacityDemo;

/// <summary>
/// Minimal reproduction for MAUI Maps polygon bugs on Android.
///
/// BUG 1: MapElements.Clear() removes items from the collection (Count becomes 0),
///        but the native Google Map still displays the polygon visuals.
///
/// BUG 2: Changing polygon properties (FillColor, StrokeColor) updates the MAUI object,
///        but the native Google Map polygon is not updated to reflect the changes.
///
/// Platform: Android (Google Maps)
/// MAUI Version: 9.0.0
/// </summary>
public partial class MainPage : ContentPage
{
    // Denver, CO - center point for test polygons
    private const double CenterLat = 39.7392;
    private const double CenterLon = -104.9903;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Center map on Denver
        Location center = new Location(CenterLat, CenterLon);
        MapSpan mapSpan = MapSpan.FromCenterAndRadius(center, Distance.FromMiles(15));
        TestMap.MoveToRegion(mapSpan);
    }

    /// <summary>
    /// Add 3 test polygons with different colors and 50% opacity.
    /// </summary>
    private void OnAddPolygonsClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("=== ADD POLYGONS ===");
        Console.WriteLine($"Before: MapElements.Count = {TestMap.MapElements.Count}");

        // Only add if not already present
        if (TestMap.MapElements.Count > 0)
        {
            StatusLabel.Text = "Polygons already added. Clear first or restart app.";
            return;
        }

        // Red polygon (northwest)
        Polygon redPolygon = CreatePolygon(
            CenterLat + 0.05, CenterLon - 0.08,
            Color.FromRgba(1.0, 0.0, 0.0, 0.5),
            Colors.DarkRed);

        // Green polygon (northeast)
        Polygon greenPolygon = CreatePolygon(
            CenterLat + 0.05, CenterLon + 0.02,
            Color.FromRgba(0.0, 1.0, 0.0, 0.5),
            Colors.DarkGreen);

        // Blue polygon (south)
        Polygon bluePolygon = CreatePolygon(
            CenterLat - 0.05, CenterLon - 0.03,
            Color.FromRgba(0.0, 0.0, 1.0, 0.5),
            Colors.DarkBlue);

        TestMap.MapElements.Add(redPolygon);
        TestMap.MapElements.Add(greenPolygon);
        TestMap.MapElements.Add(bluePolygon);

        Console.WriteLine($"After: MapElements.Count = {TestMap.MapElements.Count}");
        StatusLabel.Text = $"Added 3 polygons. Count = {TestMap.MapElements.Count}";
    }

    /// <summary>
    /// Clear all map elements.
    /// BUG: Count becomes 0, but polygons remain visible on native map.
    /// </summary>
    private void OnClearClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("=== CLEAR ===");
        Console.WriteLine($"Before Clear(): MapElements.Count = {TestMap.MapElements.Count}");

        TestMap.MapElements.Clear();

        Console.WriteLine($"After Clear(): MapElements.Count = {TestMap.MapElements.Count}");
        Console.WriteLine("BUG: If polygons are still visible, the native map was not updated!");

        StatusLabel.Text = $"Clear() called. Count = {TestMap.MapElements.Count}. Check if polygons still visible (BUG).";
    }

    /// <summary>
    /// Check current count of map elements.
    /// </summary>
    private void OnCountClicked(object? sender, EventArgs e)
    {
        int count = TestMap.MapElements.Count;
        Console.WriteLine($"MapElements.Count = {count}");
        StatusLabel.Text = $"MapElements.Count = {count}";
    }

    /// <summary>
    /// Set FillColor alpha to 0 on all polygons (should make them invisible).
    /// BUG: MAUI property changes, but native polygon remains visible.
    /// </summary>
    private void OnSetAlphaZeroClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("=== SET ALPHA TO 0 ===");

        if (TestMap.MapElements.Count == 0)
        {
            StatusLabel.Text = "No polygons to update. Add polygons first.";
            return;
        }

        foreach (MapElement element in TestMap.MapElements)
        {
            if (element is Polygon polygon)
            {
                Color oldFill = polygon.FillColor;
                Color oldStroke = polygon.StrokeColor;

                // Set alpha to 0 (should make polygon invisible)
                polygon.FillColor = Color.FromRgba(oldFill.Red, oldFill.Green, oldFill.Blue, 0.0);
                polygon.StrokeColor = Color.FromRgba(oldStroke.Red, oldStroke.Green, oldStroke.Blue, 0.0);

                Console.WriteLine($"Polygon {polygon.GetHashCode()}: Set alpha to 0");
                Console.WriteLine($"  FillColor.Alpha is now: {polygon.FillColor.Alpha}");
            }
        }

        Console.WriteLine("BUG: If polygons are still visible, the native map was not updated!");
        StatusLabel.Text = "Set Alpha=0 on all polygons. They should be invisible (but aren't - BUG).";
    }

    /// <summary>
    /// Print diagnostic info showing MAUI state vs visual state.
    /// </summary>
    private void OnDiagnosticsClicked(object? sender, EventArgs e)
    {
        Console.WriteLine("=== DIAGNOSTICS ===");
        Console.WriteLine($"MapElements.Count: {TestMap.MapElements.Count}");
        Console.WriteLine($"Map.Handler: {TestMap.Handler?.GetType().Name ?? "null"}");

        for (int i = 0; i < TestMap.MapElements.Count; i++)
        {
            if (TestMap.MapElements[i] is Polygon p)
            {
                Console.WriteLine($"[{i}] Polygon:");
                Console.WriteLine($"    FillColor: R={p.FillColor.Red:F2} G={p.FillColor.Green:F2} B={p.FillColor.Blue:F2} A={p.FillColor.Alpha:F2}");
                Console.WriteLine($"    StrokeColor: R={p.StrokeColor.Red:F2} G={p.StrokeColor.Green:F2} B={p.StrokeColor.Blue:F2} A={p.StrokeColor.Alpha:F2}");
                Console.WriteLine($"    Handler: {p.Handler?.GetType().Name ?? "null"}");
            }
        }

        Console.WriteLine("");
        Console.WriteLine("If Alpha=0.00 above but polygons are visible on map,");
        Console.WriteLine("the MapElementHandler is not syncing property changes to native GoogleMap.");

        StatusLabel.Text = "Diagnostics printed to console. Check output.";
    }

    private Polygon CreatePolygon(double centerLat, double centerLon, Color fillColor, Color strokeColor)
    {
        double size = 0.04; // ~4km square

        Polygon polygon = new Polygon
        {
            FillColor = fillColor,
            StrokeColor = strokeColor,
            StrokeWidth = 3
        };

        polygon.Geopath.Add(new Location(centerLat + size, centerLon - size));
        polygon.Geopath.Add(new Location(centerLat + size, centerLon + size));
        polygon.Geopath.Add(new Location(centerLat - size, centerLon + size));
        polygon.Geopath.Add(new Location(centerLat - size, centerLon - size));

        return polygon;
    }
}
