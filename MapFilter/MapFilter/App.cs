using Mapbox.Vector.Tile;
using System;
using System.IO;
using System.Reflection;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace MapFilter
{
    public class App : Application
    {
        static public int ScreenWidth;
        static public int TileSize = 512;
        static public string VectorTileDataFolder = "VectorTileSampleData";

        public App()
        {
            var map = new Map(MapSpan.FromCenterAndRadius(new Position(38.2527, -85.7585), Distance.FromMiles(1.5)));

            var cp = new ContentPage
            {
                Content = map
            };

            MainPage = cp;
            map.PropertyChanged += Map_PropertyChanged;
        }

        private int ZoomLevel(Map map)
        {
            var LatLng = (map.VisibleRegion.LatitudeDegrees + map.VisibleRegion.LongitudeDegrees) / 2.0f;
            int zoom = (int)Math.Floor(Math.Log(360 / LatLng, 2));
            return zoom;
        }

        public TileData WorldToTilePos(double lon, double lat, Map map)
        {
            int zoom = ZoomLevel(map);
            int x, y;

            TileSystem.LatLongToPixelXY(lat, lon, zoom, out x, out y);

            var p = new TileData()
            {
                X = x / TileSize,
                Y = y / TileSize,
                Z = zoom
            };

            return p;
        }

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

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
