## Vector Tiles With Xamarin Forms Maps
A sample Android, iOS and Windows Universal app that maps vector tile data onto a Xamarin Forms Map control




### Get Started

This solution contains 4 projects

* MapFilter - A portable class library that has all our shared code
* MapFilter.Droid - Xamarin.Android application
* MapFilter.iOS - Xamarin.iOS application
* MapFilter.UWP - Xamarin

#### NuGet Restore

All projects have the required NuGet packages already installed, so there will be no need to install additional packages during the Hands on Lab. The first thing that we must do is restore all of the NuGet packages from the internet.

This can be done by **Right-clicking** on the **Solution** and clicking on **Restore NuGet packages...**

![Restore NuGets](https://github.com/ryanlowdermilk/VectorTilesWithXamarinMaps/blob/master/restore_nuget.png?raw=true)


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

Per the Map Team at Bing, we utilize some geo industry standard constants and calculations. We do not use all the methods from the TileSystem. The 

