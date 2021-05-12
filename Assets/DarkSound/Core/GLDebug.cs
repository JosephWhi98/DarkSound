using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GLDebug : MonoBehaviour
{
    private struct Line
    {
        public Vector3 start;
        public Vector3 end;
        public Color color;
        public float startTime;
        public float duration;

        public Line(Vector3 start, Vector3 end, Color color, float startTime, float duration)
        {
            this.start = start;
            this.end = end;
            this.color = color;
            this.startTime = startTime;
            this.duration = duration;
        }

        public bool DurationElapsed(bool drawLine)
        {
            if (drawLine)
            {
                GL.Color(color);
                GL.Vertex(start);
                GL.Vertex(end);
            }
            return Time.time - startTime >= duration;
        }
    }

    private static GLDebug instance;
    private static Material matZOn;
    private static Material matZOff;

    public KeyCode toggleKey;
    public bool displayLines = true;

    private List<Line> linesZOn;
    private List<Line> linesZOff;

    void Awake()
    {
        if (instance)
        {
            DestroyImmediate(this);
            return;
        }
        instance = this;
        SetMaterial();
        linesZOn = new List<Line>();
        linesZOff = new List<Line>();
    }

    void SetMaterial()
    {
        Shader shader1 = Shader.Find("Lines/GLlineZOn");
        matZOn = new Material(shader1);

        matZOn.shader.hideFlags = HideFlags.None;

        Shader shader2 = Shader.Find("Lines/GLlineZOff");
        matZOff = new Material(shader2);

        matZOff.shader.hideFlags = HideFlags.None;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            displayLines = !displayLines;

        if (!displayLines)
        {
            linesZOn = linesZOn.Where(l => !l.DurationElapsed(false)).ToList();
            linesZOff = linesZOff.Where(l => !l.DurationElapsed(false)).ToList();
        }
    }

    void OnPostRender()
    {
        if (!displayLines) 
            return;

        matZOn.SetPass(0);
        GL.Begin(GL.LINES);
        linesZOn = linesZOn.Where(l => !l.DurationElapsed(true)).ToList();
        GL.End();

        matZOff.SetPass(0);
        GL.Begin(GL.LINES);
        linesZOff = linesZOff.Where(l => !l.DurationElapsed(true)).ToList();
        GL.End();
    }

    private static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0, bool depthTest = false)
    {
        if (duration == 0 && !instance.displayLines)
            return;
        if (start == end)
            return;
        if (depthTest)
            instance.linesZOn.Add(new Line(start, end, color, Time.time, duration));
        else
            instance.linesZOff.Add(new Line(start, end, color, Time.time, duration));
    }

    public static void DrawLine(Vector3 start, Vector3 end, Color? color = null, float duration = 0, bool depthTest = false)
    {
        DrawLine(start, end, color ?? Color.white, duration, depthTest);
    }

    public static void DrawSquare(Matrix4x4 matrix, Color? color = null, float duration = 0, bool depthTest = false)
    {
        Vector3
                p_1 = matrix.MultiplyPoint3x4(new Vector3(.5f, 0, .5f)),
                p_2 = matrix.MultiplyPoint3x4(new Vector3(.5f, 0, -.5f)),
                p_3 = matrix.MultiplyPoint3x4(new Vector3(-.5f, 0, -.5f)),
                p_4 = matrix.MultiplyPoint3x4(new Vector3(-.5f, 0, .5f));

        DrawLine(p_1, p_2, color, duration, depthTest);
        DrawLine(p_2, p_3, color, duration, depthTest);
        DrawLine(p_3, p_4, color, duration, depthTest);
        DrawLine(p_4, p_1, color, duration, depthTest);
    }

    public static void DrawCube(Vector3 pos, Vector3? rot = null, Vector3? scale = null, Color? color = null, float duration = 0, bool depthTest = false)
    {
        DrawCube(Matrix4x4.TRS(pos, Quaternion.Euler(rot ?? Vector3.zero), scale ?? Vector3.one), color, duration, depthTest);
    }

    public static void DrawCube(Matrix4x4 matrix, Color? color = null, float duration = 0, bool depthTest = false)
    {
        Vector3
                down_1 = matrix.MultiplyPoint3x4(new Vector3(.5f, -.5f, .5f)),
                down_2 = matrix.MultiplyPoint3x4(new Vector3(.5f, -.5f, -.5f)),
                down_3 = matrix.MultiplyPoint3x4(new Vector3(-.5f, -.5f, -.5f)),
                down_4 = matrix.MultiplyPoint3x4(new Vector3(-.5f, -.5f, .5f)),
                up_1 = matrix.MultiplyPoint3x4(new Vector3(.5f, .5f, .5f)),
                up_2 = matrix.MultiplyPoint3x4(new Vector3(.5f, .5f, -.5f)),
                up_3 = matrix.MultiplyPoint3x4(new Vector3(-.5f, .5f, -.5f)),
                up_4 = matrix.MultiplyPoint3x4(new Vector3(-.5f, .5f, .5f));

        DrawLine(down_1, down_2, color, duration, depthTest);
        DrawLine(down_2, down_3, color, duration, depthTest);
        DrawLine(down_3, down_4, color, duration, depthTest);
        DrawLine(down_4, down_1, color, duration, depthTest);

        DrawLine(down_1, up_1, color, duration, depthTest);
        DrawLine(down_2, up_2, color, duration, depthTest);
        DrawLine(down_3, up_3, color, duration, depthTest);
        DrawLine(down_4, up_4, color, duration, depthTest);

        DrawLine(up_1, up_2, color, duration, depthTest);
        DrawLine(up_2, up_3, color, duration, depthTest);
        DrawLine(up_3, up_4, color, duration, depthTest);
        DrawLine(up_4, up_1, color, duration, depthTest);
    }

}