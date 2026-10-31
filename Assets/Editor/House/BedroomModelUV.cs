using System;
using UnityEditor;

// Unity's Calculate packing is available only through ModelImporter. Separate
// room-only FBX files recover that packing without unwrapping the probe-lit tree.
public sealed class BedroomModelUV : AssetPostprocessor
{
    public static bool IsRoomLightmapSource(string path)
    {
        return path.StartsWith("Assets/Art/Environments/House/Bedroom/V17/LightmapSources/LightmapRoom_",StringComparison.OrdinalIgnoreCase)
            &&path.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase);
    }

    public override uint GetVersion(){return 2;}

    void OnPreprocessModel()
    {
        bool full=assetPath.StartsWith("Assets/Art/Environments/House/Bedroom/V17/Models/Bedroom_",StringComparison.OrdinalIgnoreCase)&&assetPath.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase);
        if(full||IsRoomLightmapSource(assetPath))Configure((ModelImporter)assetImporter,IsRoomLightmapSource(assetPath));
    }

    public static void Configure(ModelImporter importer,bool roomOnly)
    {
        importer.globalScale=1;importer.useFileScale=true;importer.isReadable=true;
        importer.generateSecondaryUV=roomOnly;importer.secondaryUVMarginMethod=ModelImporterSecondaryUVMarginMethod.Calculate;
        importer.secondaryUVMinLightmapResolution=40;importer.secondaryUVMinObjectScale=1;
        importer.secondaryUVAngleDistortion=8;importer.secondaryUVAreaDistortion=15;importer.secondaryUVHardAngle=88;importer.secondaryUVPackMargin=12;
        importer.meshCompression=ModelImporterMeshCompression.Off;importer.importNormals=ModelImporterNormals.Import;
        importer.importTangents=ModelImporterTangents.CalculateMikk;importer.importAnimation=false;
        importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
    }
}
