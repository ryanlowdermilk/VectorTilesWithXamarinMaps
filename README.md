## Vector Tiles With Xamarin Forms Maps
A sample Android, iOS and Windows Universal app that maps vector tile data onto a Xamarin Forms Map control

![Screenshot of UWP and iOS](https://github.com/ryanlowdermilk/VectorTilesWithXamarinMaps/blob/master/uwp_and_ios_screenshot.png?raw=true)


### Get Started

This solution contains 4 projects

* MapFilter - A portable class library that has all our shared code
* MapFilter.Droid - Xamarin.Android application
* MapFilter.iOS - Xamarin.iOS application
* MapFilter.UWP - Xamarin.UWP (Windows Universal) application

#### NuGet Restore

All projects have the required NuGet packages already installed, so there will be no need to install additional packages during the Hands on Lab. The first thing that we must do is restore all of the NuGet packages from the internet.

This can be done by **Right-clicking** on the **Solution** and clicking on **Restore NuGet packages...**

![Restore NuGets](https://github.com/ryanlowdermilk/VectorTilesWithXamarinMaps/blob/master/restore_nuget.png?raw=true)


#### Quick Walkthrough


##### Create a Map and event handler
In the App.cs file we initialize a simple Xamarin Forms Map control and wire up an event handler. When we move the map, tiles are requested.

``` csharp
public App()
        {
            var map = new Map(MapSpan.FromCenterAndRadius(
            	new Position(38.2527, -85.7585),
                Distance.FromMiles(1.5)));

            var cp = new ContentPage
            {
                Content = map
            };

            MainPage = cp;
            map.PropertyChanged += Map_PropertyChanged;
        }
```


##### Request tile data
Based on the center of the map, we ask for center tile and the surrounding 8 tiles. This happens every time we move the map.


``` csharp
private async void Map_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
{
    var m = (Map)sender;

    if (m.VisibleRegion == null)
        return;

    Exception error = null;

    try
    {
        var lat = m.VisibleRegion.Center.Latitude;
        var lng = m.VisibleRegion.Center.Longitude;

        TileData t = WorldToTilePos(lng, lat, m);

        for (int x = -1; x < 2; x++)
        {
            for (int y = -1; y < 2; y++)
            {
                ProcessTile(t, m, x, y);
            }
        }

    }
    catch (Exception ex)
    {
        error = ex;
    }

    if (error != null)
        await Application.Current.MainPage.DisplayAlert("Error!", error.Message, "OK");
}
```

##### Map tile data
Finally, for each tile we process any binary tile data we may have on our server or, in in this case, any local binary vector files.

``` csharp
private bool ProcessTile(TileData t, Map m, int xOffset, int yOffset)
{
    int x = t.X + xOffset;
    int y = t.Y + yOffset;

    string key = $"{x}_{y}";

    if (MapStore.XY.ContainsKey(key))
    {
        return false;
    }
    else
    {
        MapStore.XY.Add(key, true);
    }

    Assembly assembly = GetType().GetTypeInfo().Assembly;
    string binaryFile = $"MapFilter.{VectorTileDataFolder}.{t.Z}_{x}_{y}.mvt";

    Debug.WriteLine(binaryFile);

    using (Stream stream = assembly.GetManifestResourceStream(binaryFile))
    {
        if (stream == null)
            return false;

        var layerInfos = VectorTileParser.Parse(stream);

        if (layerInfos.Count == 0)
            return false;

        var fc = layerInfos[0]?.ToGeoJSON(x, y, t.Z);

        foreach (var geo in fc.Features)
        {
            var lng1 = ((GeoJSON.Net.Geometry.GeographicPosition)((GeoJSON.Net.Geometry.Point)geo.Geometry).Coordinates).Longitude;
            var lat1 = ((GeoJSON.Net.Geometry.GeographicPosition)((GeoJSON.Net.Geometry.Point)geo.Geometry).Coordinates).Latitude;
            m.Pins.Add(new Pin() { Position = new Position(lat1, lng1), Label = $"{lng1},{lat1}" });
        }
    }
    return true;
}
``` 

### TileData and MapStore

TileData is a POCO wich provides a simple Tile with an X, Y and Z property. This allows our code to be a bit more readable.


```csharp
namespace MapFilter
{
    public class TileData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }
}
```
MapStore, is a POCO that we use to cache our tile requests. We want to ask for a particular tile once.

``` csharp
using System.Collections.Generic;

namespace MapFilter
{
    public static class MapStore
    {
        public static Dictionary<string, bool> XY = new Dictionary<string, bool>();
    }
}
```

### TileSystem

Per the Map Team at Bing, we utilize some geo industry standard constants and calculations. We do not use all the methods from the TileSystem. In fact, the method we are most interested is LatLongToPixelXY which converts a given points long, lat and zoom lefvel to pixel XY coordinates. This is the one of the methods we use to place markers onto our map.

``` csharp
        /// <summary>
        /// Converts a point from latitude/longitude WGS-84 coordinates (in degrees)
        /// into pixel XY coordinates at a specified level of detail.
        /// </summary>
        /// <param name="latitude">Latitude of the point, in degrees.</param>
        /// <param name="longitude">Longitude of the point, in degrees.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <param name="pixelX">Output parameter receiving the X coordinate in pixels.</param>
        /// <param name="pixelY">Output parameter receiving the Y coordinate in pixels.</param>
        public static void LatLongToPixelXY(double latitude, double longitude,
        									int levelOfDetail, out int pixelX,
                                            out int pixelY)
        {
            latitude = Clip(latitude, MinLatitude, MaxLatitude);
            longitude = Clip(longitude, MinLongitude, MaxLongitude);

            double x = (longitude + 180) / 360;
            double sinLatitude = Math.Sin(latitude * Math.PI / 180);
            double y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

            uint mapSize = MapSize(levelOfDetail);
            pixelX = (int)Clip(x * mapSize + 0.5, 0, mapSize - 1);
            pixelY = (int)Clip(y * mapSize + 0.5, 0, mapSize - 1);
        }
```

