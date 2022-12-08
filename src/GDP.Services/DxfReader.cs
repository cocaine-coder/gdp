using OSGeo.OGR;
using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OSR;

namespace GDP.Services;

public class DxfReader
{
    private void Test()
    {
        GdalBase.ConfigureAll();
        Gdal.SetConfigOption("GDAL_FILENAME_IS_UTF8", "YES");//支持中文路径
        Gdal.SetConfigOption("DXF_ENCODING", "UTF-8");

        //创建北京54坐标系
        var bj54 = new SpatialReference("");
        bj54.ImportFromEPSG(4214);//4214
        var clong = 120 + 35 / 60.0;
        bj54.SetTM(0, clong, 1.0, 50805, -3421129);
        bj54.SetTOWGS84(-148.309, -218.731, -86.562, -1.49779, 4.016604, -3.591758, -6.44782521);

        var wgs84 = new SpatialReference("");
        wgs84.ImportFromEPSG(4326);
        var wgs84Geo = wgs84.CloneGeogCS();

        //创建转换
        var coordinateTransformation = new CoordinateTransformation(bj54, wgs84Geo);

        var extfile = "C:\\Users\\home-1\\Desktop\\test\\mapbox\\data\\eq.geojson";
        if (File.Exists(extfile))
            File.Delete(extfile);

        //获取geojson驱动
        using var geojsonDriver = Ogr.GetDriverByName("GeoJSON");
        using var geojsonDataSource = geojsonDriver.CreateDataSource(extfile, null);
        using var geojsonLayer = geojsonDataSource.CreateLayer("JZD", wgs84Geo, wkbGeometryType.wkbUnknown, null);
        geojsonLayer.CreateField(new FieldDefn("id", FieldType.OFTString), 1);
        geojsonLayer.CreateField(new FieldDefn("layer", FieldType.OFTString), 1);
        geojsonLayer.CreateField(new FieldDefn("style", FieldType.OFTString), 1);
        geojsonLayer.CreateField(new FieldDefn("color", FieldType.OFTString), 1);
        var geojsonLayerDefn = geojsonLayer.GetLayerDefn();

        //读取文件转化为DataSource
        using var ds = Ogr.Open(@"1.dxf", 0);
        using var dxfLayer = ds.GetLayerByIndex(0);

        Feature dxfFeature;
        var index = 0;

        using var logger = new StreamWriter("log.text", false);

        while ((dxfFeature = dxfLayer.GetNextFeature()) != null)
        {
            var geometry = dxfFeature.GetGeometryRef();
            var geometryType = dxfFeature.GetGeometryRef().GetGeometryType();

            if (geometryType == wkbGeometryType.wkbPolygon || geometryType == wkbGeometryType.wkbMultiPolygon || geometryType == wkbGeometryType.wkbPolygon25D)
            {
                var style = dxfFeature.GetStyleString();
                var layerName = dxfFeature.GetFieldAsString("Layer");
                var feature = new Feature(geojsonLayerDefn);

                geometry.Transform(coordinateTransformation);
                geometry.SwapXY();

                // wkbPolygon25D 映射MutilPolygon
                if (geometryType == wkbGeometryType.wkbPolygon25D)
                {
                    var count = geometry.GetGeometryCount();
                    var polygons = new List<Geometry>();

                    for (int i = 0; i < count; i++)
                    {
                        var polygon = Ogr.ForceToPolygon(geometry.GetGeometryRef(i));
                        polygons.Add(polygon);
                    }

                    geometry = new Geometry(wkbGeometryType.wkbMultiPolygon);
                    foreach (var polygon in polygons)
                    {
                        geometry.AddGeometry(polygon);
                    }
                }

                feature.SetGeometry(geometry);
                feature.SetField("id", index.ToString());
                feature.SetField("layer", layerName);
                feature.SetField("style", style);
                var color = GetColorFromStyle(style);
                feature.SetField("color", GetColorFromStyle(style));
                geojsonLayer.CreateFeature(feature);

                index++;
            }
            else
            {
            }
        }
    }

    private string GetColorFromStyle(string style)
    {
        var values = style.Split('(', ')')[1].Split(',');
        var colorValue = values.FirstOrDefault(x => x.StartsWith("c:") || x.StartsWith("fc:"));
        return colorValue.Split(':')[1][..7];
    }
}