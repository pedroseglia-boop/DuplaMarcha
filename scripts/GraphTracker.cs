using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class GraphTracker : MonoBehaviour
{
    [Header("Arquivo CSV")]
    public string csvFileName = "Dados_Marcha_2.csv";

    [Header("Escala do gráfico")]
    public float xScale = 0.01f;
    public float yScale = 1f;

    [Header("Eixo para plotar")]
    public Axis axis = Axis.X;

    public enum Axis
    {
        X,
        Y,
        Z
    }

    private Dictionary<int, List<Vector3>> trackers =
        new Dictionary<int, List<Vector3>>();

    private readonly Color[] colors =
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        Color.magenta,
        Color.cyan
    };

    void Start()
    {
        LoadCSV();
        DrawGraphs();
    }

    void LoadCSV()
    {
        string path = Path.Combine(Application.streamingAssetsPath, csvFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"Arquivo não encontrado: {path}");
            return;
        }

        string[] lines = File.ReadAllLines(path);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] data = lines[i].Split(';');

            if (data.Length < 5)
                continue;

            int trackerID = ExtractTrackerID(data[1]);

            float x = float.Parse(
                data[2].Replace(',', '.'),
                CultureInfo.InvariantCulture);

            float y = float.Parse(
                data[3].Replace(',', '.'),
                CultureInfo.InvariantCulture);

            float z = float.Parse(
                data[4].Replace(',', '.'),
                CultureInfo.InvariantCulture);

            if (!trackers.ContainsKey(trackerID))
                trackers[trackerID] = new List<Vector3>();

            trackers[trackerID].Add(new Vector3(x, y, z));
        }
    }

    int ExtractTrackerID(string id)
    {
        string[] parts = id.Split('/');
        return int.Parse(parts[3]);
    }

    void DrawGraphs()
    {
        foreach (var tracker in trackers)
        {
            GameObject graphObj = new GameObject($"Tracker_{tracker.Key}");

            LineRenderer line = graphObj.AddComponent<LineRenderer>();

            line.material = new Material(
                Shader.Find("Sprites/Default"));

            line.startWidth = 0.05f;
            line.endWidth = 0.05f;

            line.startColor = colors[(tracker.Key - 1) % colors.Length];
            line.endColor = colors[(tracker.Key - 1) % colors.Length];

            List<Vector3> values = tracker.Value;

            line.positionCount = values.Count;

            for (int i = 0; i < values.Count; i++)
            {
                float value = axis switch
                {
                    Axis.X => values[i].x,
                    Axis.Y => values[i].y,
                    Axis.Z => values[i].z,
                    _ => values[i].x
                };

                Vector3 point = new Vector3(
                    i * xScale,
                    value * yScale,
                    0f);

                line.SetPosition(i, point);
            }
        }
    }
}