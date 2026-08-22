using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Popcron
{
    public class GizmosInstance : MonoBehaviour
    {
        private const int DefaultQueueSize = 4096;

        private static GizmosInstance instance;
        private static Material defaultMaterial;

        private Material overrideMaterial;
        private int queueIndex = 0;
        private int lastFrame;
        private Element[] queue = new Element[DefaultQueueSize];

        /// <summary>
        /// The material being used to render
        /// </summary>
        public static Material Material
        {
            get
            {
                GizmosInstance inst = GetOrCreate();
                if (inst.overrideMaterial)
                {
                    return inst.overrideMaterial;
                }

                return DefaultMaterial;
            }
            set
            {
                GizmosInstance inst = GetOrCreate();
                inst.overrideMaterial = value;
            }
        }

        /// <summary>
        /// The default line renderer material
        /// </summary>
        public static Material DefaultMaterial
        {
            get
            {
                if (!defaultMaterial)
                {
                    // Unity has a built-in shader that is useful for drawing
                    // simple colored things.
                    Shader shader = Shader.Find("UI/Default");
                    defaultMaterial = new Material(shader)
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };

                    // Turn on alpha blending
                    defaultMaterial.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
                }

                return defaultMaterial;
            }
        }

        internal static GizmosInstance GetOrCreate()
        {
            if (!instance)
            {
                GizmosInstance[] gizmosInstances = FindObjectsOfType<GizmosInstance>();
                for (int i = 0; i < gizmosInstances.Length; i++)
                {
                    instance = gizmosInstances[i];

                    //destroy any extra gizmo instances
                    if (i > 0)
                    {
                        Destroy(gizmosInstances[i]);
                    }
                }

                //none were found, create a new one
                if (!instance)
                {
                    //instance = new GameObject(typeof(GizmosInstance).FullName).AddComponent<GizmosInstance>();
                    //instance.gameObject.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;
                    instance = Locator.GetPlayerCamera().gameObject.AddComponent<GizmosInstance>();
                }
            }

            return instance;
        }

        private float CurrentTime
        {
            get { return Time.time; }
        }

        /// <summary>
        /// Submits an array of points to draw into the queue.
        /// </summary>
        internal static void Submit(Vector3[] points, Color? color)
        {
            GizmosInstance inst = GetOrCreate();

            //if new frame, reset index
            if (inst.lastFrame != Time.frameCount)
            {
                inst.lastFrame = Time.frameCount;
                inst.queueIndex = 0;
            }

            //excedeed the length, so make it even bigger
            if (inst.queueIndex >= inst.queue.Length)
            {
                Element[] bigger = new Element[inst.queue.Length + DefaultQueueSize];
                for (int i = inst.queue.Length; i < bigger.Length; i++)
                {
                    bigger[i] = new Element();
                }

                Array.Copy(inst.queue, 0, bigger, 0, inst.queue.Length);
                inst.queue = bigger;
            }

            inst.queue[inst.queueIndex].color = color ?? Color.white;
            inst.queue[inst.queueIndex].points = points;

            inst.queueIndex++;
        }

        private void OnEnable()
        {
            //populate queue with empty elements
            queue = new Element[DefaultQueueSize];
            for (int i = 0; i < DefaultQueueSize; i++)
            {
                queue[i] = new Element();
            }
        }

        private void Update()
        {
            //always render something
            Gizmos.Line(default, default);
        }

        private void OnPostRender()
        {
            Material.SetPass(0);

            Vector3 offset = Gizmos.Offset;

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);

            List<Vector3> points = new List<Vector3>();

            //draw le elements
            for (int e = 0; e < queueIndex; e++)
            {
                //just in case
                if (queue.Length <= e)
                {
                    break;
                }

                Element element = queue[e];

                points.Clear();
                points.AddRange(element.points);

                GL.Color(element.color);
                for (int i = 0; i < points.Count; i++)
                {
                    GL.Vertex(points[i] + offset);
                }
            }

            GL.End();
            GL.PopMatrix();
        }
    }
}