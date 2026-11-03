using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Imports the supplied Blockbench glTF robot into a native Unity prefab.</summary>
public static class RobotGltfImporter
{
    private const string Source = "Assets/_Project/Models/simple-little-robot-model/source/model.gltf";
    private const string Output = "Assets/_Project/Models/simple-little-robot-model/converted";
    private const string Prefab = Output + "/DesignedRobot.prefab";

    public static GameObject EnsureImported()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        if (existing != null) return existing;
        if (!File.Exists(Source)) return null;

        JObject gltf = JObject.Parse(File.ReadAllText(Source));
        JArray views = (JArray)gltf["bufferViews"];
        JArray accessors = (JArray)gltf["accessors"];
        byte[] buffer = Convert.FromBase64String(((string)gltf["buffers"][0]["uri"]).Split(',')[1]);
        Directory.CreateDirectory(Output);
        string texturePath = Output + "/RobotAtlas.png";
        string imageUri = (string)gltf["images"][0]["uri"];
        File.WriteAllBytes(texturePath, Convert.FromBase64String(imageUri.Split(',')[1]));
        AssetDatabase.Refresh();
        Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        const string materialAssetPath = Output + "/DesignedRobot.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialAssetPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
            { name = "Designed Robot Atlas" };
            material.mainTexture = atlas;
            AssetDatabase.CreateAsset(material, materialAssetPath);
        }

        GameObject root = new GameObject("DesignedRobot");
        Transform[] nodeTransforms = new Transform[gltf["nodes"].Count()];
        for (int i = 0; i < nodeTransforms.Length; i++)
        {
            JObject node = (JObject)gltf["nodes"][i];
            GameObject go = new GameObject((string)node["name"] ?? "Robot Part " + i);
            nodeTransforms[i] = go.transform;
            if (node["translation"] is JArray t) go.transform.localPosition = new Vector3((float)t[0], (float)t[1], (float)t[2]);
            if (node["rotation"] is JArray q) go.transform.localRotation = new Quaternion((float)q[0], (float)q[1], (float)q[2], (float)q[3]);
            if (node["scale"] is JArray s) go.transform.localScale = new Vector3((float)s[0], (float)s[1], (float)s[2]);
            if (node["mesh"] != null) AddMesh(go, (int)node["mesh"], gltf, views, accessors, buffer, material, i);
        }
        for (int i = 0; i < nodeTransforms.Length; i++)
        {
            JObject node = (JObject)gltf["nodes"][i];
            if (node["children"] is JArray children)
                foreach (JToken child in children) nodeTransforms[(int)child].SetParent(nodeTransforms[i], false);
        }
        int sceneIndex = (int)gltf["scene"];
        int rootNode = (int)gltf["scenes"][sceneIndex]["nodes"][0];
        nodeTransforms[rootNode].SetParent(root.transform, false);
        PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
    }

    private static void AddMesh(GameObject target, int meshIndex, JObject gltf, JArray views,
        JArray accessors, byte[] buffer, Material material, int nodeIndex)
    {
        JObject primitive = (JObject)gltf["meshes"][meshIndex]["primitives"][0];
        JObject attrs = (JObject)primitive["attributes"];
        Vector3[] vertices = ReadVector3((int)attrs["POSITION"], accessors, views, buffer);
        Vector2[] uv = attrs["TEXCOORD_0"] != null
            ? ReadVector2((int)attrs["TEXCOORD_0"], accessors, views, buffer)
            : new Vector2[vertices.Length];
        int[] triangles = ReadIndices((int)primitive["indices"], accessors, views, buffer);
        // glTF uses a right-handed winding convention; Unity renders front faces
        // with the opposite winding when retaining the source coordinate system.
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            int swap = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = swap;
        }
        Mesh mesh = new Mesh { name = "RobotPart_" + nodeIndex };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        string meshPath = Output + "/RobotPart_" + nodeIndex + ".asset";
        AssetDatabase.CreateAsset(mesh, meshPath);
        target.AddComponent<MeshFilter>().sharedMesh = mesh;
        target.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static int Offset(JObject accessor, JArray views)
    {
        JObject view = (JObject)views[(int)accessor["bufferView"]];
        return ((int?)view["byteOffset"] ?? 0) + ((int?)accessor["byteOffset"] ?? 0);
    }

    private static Vector3[] ReadVector3(int index, JArray accessors, JArray views, byte[] data)
    {
        JObject a = (JObject)accessors[index]; int count = (int)a["count"]; int start = Offset(a, views);
        int stride = (int?)((JObject)views[(int)a["bufferView"]])["byteStride"] ?? 12;
        Vector3[] result = new Vector3[count];
        for (int i = 0; i < count; i++) result[i] = new Vector3(F(data, start + i * stride), F(data, start + i * stride + 4), F(data, start + i * stride + 8));
        return result;
    }

    private static Vector2[] ReadVector2(int index, JArray accessors, JArray views, byte[] data)
    {
        JObject a = (JObject)accessors[index]; int count = (int)a["count"]; int start = Offset(a, views);
        int stride = (int?)((JObject)views[(int)a["bufferView"]])["byteStride"] ?? 8;
        Vector2[] result = new Vector2[count];
        for (int i = 0; i < count; i++) result[i] = new Vector2(F(data, start + i * stride), 1f - F(data, start + i * stride + 4));
        return result;
    }

    private static int[] ReadIndices(int index, JArray accessors, JArray views, byte[] data)
    {
        JObject a = (JObject)accessors[index]; int count = (int)a["count"]; int start = Offset(a, views);
        int component = (int)a["componentType"]; int width = component == 5123 ? 2 : 4;
        int[] result = new int[count];
        for (int i = 0; i < count; i++) result[i] = width == 2 ? BitConverter.ToUInt16(data, start + i * width) : (int)BitConverter.ToUInt32(data, start + i * width);
        return result;
    }

    private static float F(byte[] bytes, int offset) => BitConverter.ToSingle(bytes, offset);
}
