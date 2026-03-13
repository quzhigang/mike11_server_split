import os
import sys
import json
import shutil

import arcpy
import arcpy
import re
import datetime
import time

import pgsql as pg
import depth as dd


# CGCS2000
geo_sr = arcpy.SpatialReference(4490)

def delete_all_rater_files(directory,hz):
    for filename in os.listdir(directory):
        if filename.endswith(hz):
            file_path = os.path.join(directory, filename)
            try:
                arcpy.management.Delete(file_path)
                print(f"success: {file_path}")
            except Exception as e:
                print(f"fail: {file_path}. error_info: {e}")

def delete_all_files_and_folders(directory):
    for item in os.listdir(directory):
        item_path = os.path.join(directory, item)
        if os.path.isfile(item_path):
            os.remove(item_path)
        elif os.path.isdir(item_path):
            shutil.rmtree(item_path)
    shutil.rmtree(directory)

def Json_format(geojson_path):

    myfile = open(geojson_path,"r")
    geojson_str = myfile.read()
    myfile.close()

    geojson_obj = json.loads(geojson_str)

    features = geojson_obj['features']
    for feature in features:
        geo = feature['geometry']
        coordinates = geo['coordinates']
        type = geo["type"]
        if "MultiPolygon" == type: Json_format_multipolygon(coordinates)
        if "Polygon" == type: Json_format_polygon(coordinates)

    res_jsonstr = json.dumps(geojson_obj)

    res_file = open(geojson_path,"w")
    res_file.write(res_jsonstr)
    res_file.close()

    return None

def Json_format_polygon(coordinates):
    for polygon in coordinates:
        for points in polygon:
            points[0] = round(points[0],5)
            points[1] = round(points[1],5)
            if(len(points) == 3):
                points[2] = round(points[2],3)

def Json_format_multipolygon(coordinates):
    for polygons in coordinates:
        for polygon in polygons:
            for points in polygon:
                points[0] = round(points[0],5)
                points[1] = round(points[1],5)
                if(len(points) == 3):
                    points[2] = round(points[2],3)

def planFolder(case_folder, subdir):
    plan_folder = case_folder + "\\" + subdir
    if not os.path.exists(plan_folder): os.mkdir(plan_folder)
    return plan_folder

def fsdaFolder(case_folder, plan_code, fsda_code):
    fsda_folder = case_folder + "\\" + plan_code + "\\" + fsda_code
    if not os.path.exists(fsda_folder):
        os.mkdir(fsda_folder)
        os.mkdir(fsda_folder + "\\input")
        os.mkdir(fsda_folder + "\\input\\tiff")
        os.mkdir(fsda_folder + "\\input\\shp")
        os.mkdir(fsda_folder + "\\temp")
        os.mkdir(fsda_folder + "\\res")
        os.mkdir(fsda_folder + "\\res\\tiff")
        os.mkdir(fsda_folder + "\\res\\json")
    return fsda_folder

def main(res_folder, json_file, dem):

    temp_folder = os.path.dirname(json_file) + "\\" + "tj_temptdir"
    if not os.path.exists(temp_folder):
        os.makedirs(temp_folder)
    try: arcpy.env.workspace = temp_folder
    except: pass

    input_shp_wgs84 = temp_folder + "\\" + "feature_temp_wgs84.shp"
    input_shp = temp_folder + "\\" + "feature_temp.shp"
    arcpy.JSONToFeatures_conversion(json_file, input_shp_wgs84)
    out_coor_system = arcpy.SpatialReference(4547)
    arcpy.Project_management(input_shp_wgs84, input_shp ,out_coor_system)

    # raster_size
    cellsize = arcpy.GetRasterProperties_management(dem, "CELLSIZEX").getOutput(0)
    cellsize = float(cellsize)

    max_level_raster = temp_folder + "\\" + "max_level.tif"
    level_raster = []
    with arcpy.da.SearchCursor(input_shp, ['OID@']) as cursor:
        i = 0
        for row in cursor:
            oid = row[0]
            where_clause = f"FID = {oid}"
            temp_polygon = os.path.join(temp_folder, f"temp_polygon_{i}.shp")
            arcpy.Select_analysis(input_shp, temp_polygon, where_clause)

            # create temp_multipatch
            temp_polygon_featurelayer = arcpy.MakeFeatureLayer_management(temp_polygon, "temp_polygon_featurelayer")
            temp_multipatch = os.path.join(temp_folder, f"temp_mulp_{i}.shp")
            arcpy.Layer3DToFeatureClass_3d(temp_polygon_featurelayer, temp_multipatch,"","")

            # create temp_raster
            temp_raster = os.path.join(temp_folder, f"temp_raster_{i}.tif")
            arcpy.MultipatchToRaster_conversion(temp_multipatch, temp_raster, cellsize)
            level_raster.append(temp_raster)

            arcpy.Delete_management(temp_polygon_featurelayer)
            arcpy.Delete_management(temp_polygon)
            arcpy.Delete_management(temp_multipatch)
            i = i +1

        # combine_raster
        input_rester_files = ";".join(level_raster)
        proj_114e_str = arcpy.SpatialReference(4547)
        arcpy.MosaicToNewRaster_management(input_rester_files,temp_folder,"max_level.tif",proj_114e_str,"32_BIT_FLOAT",str(cellsize),1,"LAST","FIRST")

    arcpy.Delete_management(input_shp)

    # level- dem
    depth_raster = temp_folder + "\\" + "depth.tif"
    arcpy.gp.RasterCalculator_sa('"{0}"-"{1}"'.format(max_level_raster, dem), depth_raster)

    # depth_raster
    depth_raster_jz = temp_folder + "\\" + "depth_jz.tif"
    arcpy.gp.RasterCalculator_sa('Con("{0}">0.05,"{0}")'.format(depth_raster), depth_raster_jz)
    
    # to geojson
    h_json = res_folder + "\\" + "h.json"
    dd.main(depth_raster_jz, temp_folder, h_json,cellsize)

    #del raster dir
    delete_all_rater_files(temp_folder,'.tif')
    delete_all_rater_files(temp_folder,'.shp')
    delete_all_rater_files(temp_folder,'.xml')
    delete_all_rater_files(temp_folder,'.json')
    if os.path.isfile(json_file):os.remove(json_file)
    #shutil.rmtree(temp_folder)

    return h_json

if __name__ == "__main__":
    db = sys.argv[1].split(",")
    plan_code = sys.argv[2]
    json_file = sys.argv[3]
    model_instance = sys.argv[4]
    base_demfile = sys.argv[5]
    case_folder = os.path.dirname(json_file)

    # db = "10.20.2.153,54321,hnsl,Hnsl@6915,kingbase,mike11_gispolygon_result".split(",")
    # plan_code = "model_20240829100000"
    # json_file = r"C:\inetpub\wwwroot\wg_modelserver\hd_mike11server\wg_models\wg_mike11\model_20240829100000\results\max_level.json"
    # model_instance = "wg_mike11"
    # base_demfile = r'C:\inetpub\wwwroot\wg_modelserver\hd_mike11server\source\ymfx\base_dem\wg_20m.tif'
    # case_folder = r"C:\inetpub\wwwroot\wg_modelserver\hd_mike11server\wg_models\wg_mike11\model_20240829100000\results"
    
    arcpy.env.workspace = case_folder
    arcpy.env.overwriteOutput = True
    arcpy.CheckOutExtension("spatial")
    arcpy.CheckOutExtension("3D")

    # connect mysql
    PgSQL_OP = pg.PgSQL_OP(db, plan_code,model_instance)
    PgSQL_OP.connect()

    try:
        res_dir = planFolder(case_folder, "polygon_json")
        h_jsonfile = main(res_dir,json_file, base_demfile)

        PgSQL_OP.del_old()
        PgSQL_OP.insert(h_jsonfile)
        PgSQL_OP.succeed()

    except Exception as e:
        print(e)
        PgSQL_OP.fail()
    finally:
        PgSQL_OP.close()
        sys.exit()