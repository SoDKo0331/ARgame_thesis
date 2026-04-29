using UnityEngine;

public static class GeoCoordinateUtils
{
    private const double EarthRadiusMeters = 6371000d;
    private const double MinLatitude = -85.05112878d;
    private const double MaxLatitude = 85.05112878d;

    public static float HaversineMeters(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        double latitudeDelta = DegreesToRadians(latitude2 - latitude1);
        double longitudeDelta = DegreesToRadians(longitude2 - longitude1);

        double latitude1Radians = DegreesToRadians(latitude1);
        double latitude2Radians = DegreesToRadians(latitude2);

        double haversine =
            System.Math.Sin(latitudeDelta * 0.5d) * System.Math.Sin(latitudeDelta * 0.5d) +
            System.Math.Cos(latitude1Radians) * System.Math.Cos(latitude2Radians) *
            System.Math.Sin(longitudeDelta * 0.5d) * System.Math.Sin(longitudeDelta * 0.5d);

        double arc = 2d * System.Math.Atan2(System.Math.Sqrt(haversine), System.Math.Sqrt(1d - haversine));
        return (float)(EarthRadiusMeters * arc);
    }

    public static bool TryGetAnchoredPosition(
        double latitude,
        double longitude,
        double centerLatitude,
        double centerLongitude,
        float zoom,
        Vector2 viewportSize,
        float padding,
        out Vector2 anchoredPosition)
    {
        anchoredPosition = Vector2.zero;

        if (viewportSize.x <= 0f || viewportSize.y <= 0f)
        {
            return false;
        }

        Vector2 centerPixel = LatLonToWorldPixel(centerLatitude, centerLongitude, zoom);
        Vector2 targetPixel = LatLonToWorldPixel(latitude, longitude, zoom);

        anchoredPosition = new Vector2(
            targetPixel.x - centerPixel.x,
            -(targetPixel.y - centerPixel.y));

        float halfWidth = viewportSize.x * 0.5f + padding;
        float halfHeight = viewportSize.y * 0.5f + padding;

        return
            Mathf.Abs(anchoredPosition.x) <= halfWidth &&
            Mathf.Abs(anchoredPosition.y) <= halfHeight;
    }

    public static float MetersToPixels(double latitude, float zoom, float meters)
    {
        double clampedLatitude = System.Math.Max(MinLatitude, System.Math.Min(MaxLatitude, latitude));
        double latitudeRadians = DegreesToRadians(clampedLatitude);
        double metersPerPixel =
            System.Math.Cos(latitudeRadians) * 2d * System.Math.PI * EarthRadiusMeters /
            (256d * System.Math.Pow(2d, zoom));

        if (metersPerPixel <= 0d)
        {
            return 0f;
        }

        return (float)(meters / metersPerPixel);
    }

    public static float CalculateZoomForVisibleMeters(double latitude, float visibleMeters, float targetPixels)
    {
        if (visibleMeters <= 0.001f || targetPixels <= 1f)
        {
            return 0f;
        }

        double clampedLatitude = System.Math.Max(MinLatitude, System.Math.Min(MaxLatitude, latitude));
        double latitudeRadians = DegreesToRadians(clampedLatitude);
        double numerator = System.Math.Cos(latitudeRadians) * 2d * System.Math.PI * EarthRadiusMeters * targetPixels;
        double denominator = 256d * visibleMeters;

        if (numerator <= 0d || denominator <= 0d)
        {
            return 0f;
        }

        return (float)(System.Math.Log(numerator / denominator, 2d));
    }

    public static Vector2 PanCenterByScreenDelta(
        double centerLatitude,
        double centerLongitude,
        float zoom,
        Vector2 screenDelta)
    {
        Vector2 centerPixel = LatLonToWorldPixel(centerLatitude, centerLongitude, zoom);
        Vector2 shiftedPixel = centerPixel + new Vector2(-screenDelta.x, screenDelta.y);
        return WorldPixelToLonLat(shiftedPixel, zoom);
    }

    public static Vector2 WorldPixelToLonLat(Vector2 worldPixel, float zoom)
    {
        double scale = 256d * System.Math.Pow(2d, zoom);
        double longitude = worldPixel.x / scale * 360d - 180d;

        double n = System.Math.PI - 2d * System.Math.PI * worldPixel.y / scale;
        double latitude = RadiansToDegrees(System.Math.Atan(System.Math.Sinh(n)));
        latitude = System.Math.Max(MinLatitude, System.Math.Min(MaxLatitude, latitude));

        return new Vector2((float)longitude, (float)latitude);
    }

    private static Vector2 LatLonToWorldPixel(double latitude, double longitude, float zoom)
    {
        double clampedLatitude = System.Math.Max(MinLatitude, System.Math.Min(MaxLatitude, latitude));
        double latitudeRadians = DegreesToRadians(clampedLatitude);
        double scale = 256d * System.Math.Pow(2d, zoom);

        double x = (longitude + 180d) / 360d * scale;
        double y = (1d - System.Math.Log(System.Math.Tan(latitudeRadians) + 1d / System.Math.Cos(latitudeRadians)) / System.Math.PI) * 0.5d * scale;

        return new Vector2((float)x, (float)y);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * System.Math.PI / 180d;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180d / System.Math.PI;
    }
}
