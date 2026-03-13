
# Import arcpy module
import os
import json
import arcpy
import shutil


def GeoJson_PolyGonQlh(json_str,xsw):
    arcgisjson_obj = json.loads(json_str)
    geojson_obj = arcgis_polygonjson_to_geojson(arcgisjson_obj)
    features = geojson_obj['features']

    res_names = ['Current_sp','Duration_a','Total_wate','At_time']
    for feature in features:
        properties = feature['properties']
        for attr in res_names:
            if attr in properties:
                properties[attr] = round(properties[attr],3)

        geo = feature['geometry']
        coordinates = geo['coordinates']
        for polygon in coordinates:
            for points in polygon:
                points[0] = round(points[0],xsw)
                points[1] = round(points[1],xsw)
                if(len(points) == 3):
                    points[2] = round(points[2],3)
    res_jsonstr = json.dumps(geojson_obj)
    return res_jsonstr

#arcgis_polygonjson to geojson
def arcgis_polygonjson_to_geojson(arcgis_json):
    geojson = {
        "type": "FeatureCollection",
        "features": []
    }

    geometry_type = arcgis_json["geometryType"]

    has_z_value = False
    if "hasZ" in arcgis_json:
        if arcgis_json["hasZ"]:
            has_z_value = True

    if has_z_value:
        geojson["hasZ"] = True

    for feature in arcgis_json["features"]:
        if geometry_type == "esriGeometryPolygon":
            geometry = {
                "type": "Polygon",
                "coordinates": feature["geometry"]["rings"]
            }
        elif geometry_type.upper() == "ESIGEOMETRYMULTIPOLYGON":
            geometry = {
                "type": "MultiPolygon",
                "coordinates": feature["geometry"]["rings"]
            }
        else:
            continue

        properties = feature["attributes"]

        geojson_feature = {
            "type": "Feature",
            "geometry": geometry,
            "properties": properties
        }

        geojson["features"].append(geojson_feature)
    return geojson


def Write_JsonFiles(json_qlh,jsonfile):
    res_file = open(jsonfile,"w")
    res_file.write(json_qlh)
    res_file.close()   


def main(h_raster, folder_temp, outputJson,cellsize):

    arcpy.CheckOutExtension("spatial")
    arcpy.CheckOutExtension("3D")

    try: arcpy.env.workspace = folder_temp
    except: pass
    folder_temp = folder_temp + "\\"

    # Process: x
    Times_ASCIIT1 = folder_temp + "times.tif"
    arcpy.Times_3d(h_raster, "100", Times_ASCIIT1)

    # Process: to int
    rasto_Inter = folder_temp + "Int_Times_AS1.tif"
    arcpy.gp.Int_sa(Times_ASCIIT1, rasto_Inter)

    # Process: chong fen lei
    Reclass = folder_temp + "Reclass_Int_2.tif"
    arcpy.gp.Reclassify_sa(rasto_Inter, "Value", "5 10 5;10 20 15;20 40 30;40 60 50;60 80 70;80 120 100;120 160 140;160 200 180;200 250 220;250 300 270;300 2000 350", Reclass, "DATA")

    # Process: raster to polygon
    ras_to_ploy = folder_temp + "RasterT_Reclass2.shp"
    arcpy.RasterToPolygon_conversion(Reclass, ras_to_ploy, "SIMPLIFY", "", "SINGLE_OUTER_PART", "")

    # Process: add field
    arcpy.AddField_management(ras_to_ploy, "Total_wate", "DOUBLE", "", "", "", "", "NULLABLE", "NON_REQUIRED", "")

    # Process: cal field
    arcpy.CalculateField_management(ras_to_ploy, "Total_wate", "!GRIDCODE!/100", "PYTHON3", "")

    # Process: select =>0.05m
    select005 = folder_temp + "select005.shp"
    arcpy.Select_analysis(ras_to_ploy, select005, "\"Total_wate\" >=0.05")

    # Process: select =>0.2m
    select020 = folder_temp + "select020.shp"
    arcpy.Select_analysis(select005, select020, "\"Total_wate\" >=0.2")

    # Process: select =>0.4m
    select040 = folder_temp + "select040.shp"
    arcpy.Select_analysis(select020, select040, "\"Total_wate\" >=0.4")

    # Process: select =>0.6m
    select060 = folder_temp + "select060.shp"
    arcpy.Select_analysis(select040, select060, "\"Total_wate\" >=0.6")

    # Process: select =>1m 
    select100 = folder_temp + "select100.shp"
    arcpy.Select_analysis(select060, select100, "\"Total_wate\" >=1.0")

    # Process: select =>1.5m 
    select150 = folder_temp + "select150.shp"
    arcpy.Select_analysis(select100, select150, "\"Total_wate\" >=1.5")

    # Process: select  =>2m
    select200 = folder_temp + "select200.shp"
    arcpy.Select_analysis(select150, select200, "\"Total_wate\" >=2")

    # Process: select  =>2.5m
    select250 = folder_temp + "select250.shp"
    arcpy.Select_analysis(select200, select250, "\"Total_wate\" >=2.5")

    # Process: select  =>3m
    select300 = folder_temp + "select300.shp"
    arcpy.Select_analysis(select250, select300, "\"Total_wate\" >=3")

    # Process:dissolve
    dissolve005 = folder_temp + "dissolve005.shp"
    arcpy.Dissolve_management(select005, dissolve005, "", "Total_wate MIN", "SINGLE_PART", "DISSOLVE_LINES")

    # Process: dissolve
    dissolve020 = folder_temp + "dissolve020.shp"
    arcpy.Dissolve_management(select020, dissolve020, "", "Total_wate MIN", "SINGLE_PART", "DISSOLVE_LINES")

    # Process: dissolve
    dissolve040 = folder_temp + "dissolve040.shp"
    arcpy.Dissolve_management(select040, dissolve040, "", "Total_wate MIN", "SINGLE_PART", "DISSOLVE_LINES")

    # Process: dissolve
    dissolve060 = folder_temp + "dissolve060.shp"
    arcpy.Dissolve_management(select060, dissolve060, "", "Total_wate MIN", "SINGLE_PART", "DISSOLVE_LINES")

    # Process: dissolve
    dissolve100 = folder_temp + "dissolve100.shp"
    arcpy.Dissolve_management(select100, dissolve100, "", "Total_wate MIN", "SINGLE_PART", "DISSOLVE_LINES")

    # Process: dissolve
    dissolve150 = folder_temp + "dissolve150.shp"
    arcpy.Dissolve_management(select150, dissolve150, "", "Total_wate MIN", "SINGLE_PART", "DISSOLVE_LINES")

    # Process: dissolve
    dissolve200 = folder_temp + "dissolve200.shp"
    arcpy.Dissolve_management(select200, dissolve200, "", "Total_wate MIN", "SINGLE_PART", "DISSOLVE_LINES")

    # Process: dissolve
    dissolve250 = folder_temp + "dissolve250.shp"
    arcpy.Dissolve_management(select250, dissolve250, "", "Total_wate MIN", "SINGLE_PART", "DISSOLVE_LINES")

    # Process: dissolve
    dissolve300 = folder_temp + "dissolve300.shp"
    arcpy.Dissolve_management(select300, dissolve300, "", "Total_wate MIN", "SINGLE_PART", "DISSOLVE_LINES")

    # Process: append
    inputs = [dissolve020,dissolve040,dissolve060,dissolve100,dissolve150,dissolve200,dissolve250,dissolve300]
    arcpy.Append_management(inputs, dissolve005, "TEST", "", "")

    # del small polygon
    arcpy.AddField_management(dissolve005, "area", "DOUBLE", "", "", "", "", "NULLABLE", "NON_REQUIRED", "")
    arcpy.CalculateField_management(dissolve005, "area", "!shape.area!", "PYTHON3", "")
    PolygonFilter = folder_temp + "PolygonFilter.shp"
    arcpy.Select_analysis(dissolve005, PolygonFilter, "\"area\" >={0}".format(cellsize * cellsize * 25))

    # Process: add field
    arcpy.AddField_management(PolygonFilter, "Total_wate", "DOUBLE", "", "", "", "", "NULLABLE", "NON_REQUIRED", "")

    # Process: cal field
    arcpy.CalculateField_management(PolygonFilter, "Total_wate", "!MIN_Total_!", "PYTHON3", "")

    # Process: smooth
    smoothPolygon = folder_temp + "smoothPolygon.shp"
    arcpy.SmoothPolygon_cartography(PolygonFilter, smoothPolygon, "PAEK", "{0} Meters".format(cellsize * 5), "FIXED_ENDPOINT", "NO_CHECK", "")

    # Process: general
    arcpy.Generalize_edit(smoothPolygon, "{0} Meters".format(cellsize * 0.05))

    # Process: del field
    arcpy.DeleteField_management(smoothPolygon, "MIN_Total_;InPoly_FID;area")

    #change to WGS84
    out_feature_wgs84 = folder_temp + "out_feature_wgs84.shp"
    out_coor_system = arcpy.SpatialReference(4326)
    arcpy.Project_management(smoothPolygon, out_feature_wgs84 ,out_coor_system)

    #feature to json
    json_filename = folder_temp + "h_tmpt_res.json"
    arcpy.FeaturesToJSON_conversion(out_feature_wgs84, json_filename,"NOT_FORMATTED", "Z_VALUES", "NO_M_VALUES", "NO_GEOJSON")
    
    myfile = open(json_filename,"r")
    res_json = myfile.read()
    json_qlh = GeoJson_PolyGonQlh(res_json,5)
    myfile.close()

    Write_JsonFiles(json_qlh,outputJson)

    return outputJson

