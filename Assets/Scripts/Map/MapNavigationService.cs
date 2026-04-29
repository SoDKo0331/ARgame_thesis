using System;
using System.Globalization;
using UnityEngine;

public static class MapNavigationService
{
    public static bool OpenDirections(TourismSpot spot, LocationTracker locationTracker)
    {
        if (spot == null)
        {
            return false;
        }

        string destination = string.Format(
            CultureInfo.InvariantCulture,
            "{0},{1}",
            spot.latitude,
            spot.longitude);

        string encodedSpotName = Uri.EscapeDataString(string.IsNullOrEmpty(spot.spotName) ? "Tourism Spot" : spot.spotName);
        string url;

#if UNITY_IOS
        url = "http://maps.apple.com/?dirflg=w&daddr=" + destination + "&q=" + encodedSpotName;

        if (locationTracker != null && locationTracker.IsLocationReady)
        {
            string source = string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1}",
                locationTracker.Latitude,
                locationTracker.Longitude);
            url += "&saddr=" + source;
        }
#else
        url = "https://www.google.com/maps/dir/?api=1&destination=" + Uri.EscapeDataString(destination) + "&travelmode=walking";

        if (locationTracker != null && locationTracker.IsLocationReady)
        {
            string origin = string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1}",
                locationTracker.Latitude,
                locationTracker.Longitude);
            url += "&origin=" + Uri.EscapeDataString(origin);
        }
#endif

        Application.OpenURL(url);
        return true;
    }
}
