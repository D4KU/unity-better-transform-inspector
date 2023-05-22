using System;
using UnityEngine;
using UnityEditor;
using SimpleExpressionEngine;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace BetterTransformInspector
{
    public class ExpressiveTransformEditor : IContext
    {
        private const float DELTA = 0.0001f;

        private readonly string[] textX = new string[6];
        private readonly string[] textY = new string[6];
        private readonly string[] textZ = new string[6];

        private int selectionIndex;
        private int vectorIndex;
        private Object[] targets;
        private List<Vector3[]> originals;

        private Transform Current => (Transform)targets[selectionIndex];
        private bool LocalVector => vectorIndex < 3;

        public ExpressiveTransformEditor()
        {
            Undo.undoRedoPerformed += Clear;
        }

        ~ExpressiveTransformEditor()
        {
            Undo.undoRedoPerformed -= Clear;
        }

        public void Clear()
        {
            originals = null;
            Array.Clear(textX, 0, textX.Length);
            Array.Clear(textY, 0, textY.Length);
            Array.Clear(textZ, 0, textZ.Length);
        }

        public void OnInspectorGUI(Object[] targets)
        {
            this.targets = targets;
            originals ??= targets
                .Cast<Transform>()
                .Select(x => new[]
                {
                    x.localPosition,
                    x.localEulerAngles,
                    x.localScale,
                    x.position,
                    x.eulerAngles,
                    x.lossyScale,
                })
                .ToList();
        }

        public void DrawLocalVectors()
        {
            vectorIndex = 0;
            DrawVector(
                getter: t => t.localPosition,
                setter: (t, v) => t.localPosition = v);

            vectorIndex = 1;
            DrawVector(
                getter: t => t.localEulerAngles,
                setter: (t, v) => t.localEulerAngles = v);

            vectorIndex = 2;
            DrawVector(
                getter: t => t.localScale,
                setter: (t, v) => t.localScale = v);
        }

        public void DrawGlobalVectors()
        {
            vectorIndex = 3;
            DrawVector(
                getter: t => t.position,
                setter: (t, v) => t.position = v);

            vectorIndex = 4;
            DrawVector(
                getter: t => t.eulerAngles,
                setter: (t, v) => t.eulerAngles = v);

            vectorIndex = 5;
            DrawVector(
                getter: t => t.lossyScale,
                setter: TransformEditor.SetLossyScale);
        }

        private void DrawVector(
                Func<Transform, Vector3> getter,
                Action<Transform, Vector3> setter)
        {
            IEnumerable<Vector3> vectors = targets.Cast<Transform>().Select(getter);
            Vector3 first = vectors.First();

            bool sameX = true;
            bool sameY = true;
            bool sameZ = true;

            foreach (Vector3 v in vectors.Skip(1))
            {
                sameX &= Math.Abs(v.x - first.x) < DELTA;
                sameY &= Math.Abs(v.y - first.y) < DELTA;
                sameZ &= Math.Abs(v.z - first.z) < DELTA;
            }

            string label = vectorIndex switch
            {
                0 or 3 => "Position",
                1 or 4 => "Rotation",
                2 or 5 => "Scale",
                _ => string.Empty,
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.MinWidth(10));

            Node nodeX = DrawTextField(sameX, first.x, "x", ref textX[vectorIndex]);
            Node nodeY = DrawTextField(sameY, first.y, "y", ref textY[vectorIndex]);
            Node nodeZ = DrawTextField(sameZ, first.z, "z", ref textZ[vectorIndex]);

            EditorGUILayout.EndHorizontal();

            if (nodeX == null && nodeY == null && nodeZ == null)
                return;

            Undo.RecordObjects(targets, "Set Transform");
            for (selectionIndex = 0; selectionIndex < targets.Length; selectionIndex++)
            {
                Transform t = Current;
                Vector3 v = getter(t);
                if (nodeX != null) v.x = Eval(nodeX);
                if (nodeY != null) v.y = Eval(nodeY);
                if (nodeZ != null) v.z = Eval(nodeZ);
                setter(t, v);
            }
        }

        private Node DrawTextField(bool same, float first, string varName, ref string text)
        {
            EditorGUILayout.LabelField(
                label: varName.ToUpper(),
                style: new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter },
                options: GUILayout.MaxWidth(14));

            if (string.IsNullOrEmpty(text))
                text = same ? first.ToString() : varName;

            string newText = EditorGUILayout.TextField(text);
            if (newText == text)
                return null;

            text = newText;

            try
            {
                return Parser.Parse(newText);
            }
            catch
            {
                return null;
            }
        }

        private float Eval(Node node) => (float)node.Eval(this);

        private float ResolveXYZ(string varName, int objectIndex)
        {
            if (objectIndex < 0)
                objectIndex += targets.Length;

            int vi = char.ToLower(varName[0]) switch
            {
                'p' => LocalVector ? 0 : 3,
                'r' => LocalVector ? 1 : 4,
                's' => LocalVector ? 2 : 5,
                _ => vectorIndex,
            };

            char last = varName[varName.Length - 1];
            if (char.IsUpper(last))
                vi += LocalVector ? 3 : -3;

            Vector3 v = originals[objectIndex][vi];

            return char.ToLower(last) switch
            {
                'x' => v.x,
                'y' => v.y,
                'z' => v.z,
                _ => throw new ArgumentException(),
            };
        }

        public double ResolveVariable(string name)
        {
            return name switch
            {
                "i" => selectionIndex,
                "l" => targets.Length,
                "j" => Current.GetSiblingIndex(),
                "c" => Current.childCount,
                "e" => Math.E,
                "pi" => Math.PI,
                "r" => Random.value,
                _ => ResolveXYZ(name, selectionIndex),
            };
        }

        public double CallFunction(string name, double[] args)
        {
            return name switch
            {
                "abs" => Math.Abs(args[0]),
                "sqrt" => Math.Sqrt(args[0]),
                "mod" => args[0] % args[1],
                "log" => Math.Log(args[0], args.Length < 2 ? Math.E : args[1]),
                "pow" => Math.Pow(args[0], args[1]),
                "trunc" => Math.Truncate(args[0]),
                "frac" => Math.Abs(args[0]) - Math.Abs((int)args[0]),
                "sign" => Math.Sign(args[0]),
                "step" => args[0] >= args[1] ? 1 : 0,
                "round" => Math.Round(args[0]),
                "floor" => Math.Floor(args[0]),
                "ceil" => Math.Ceiling(args[0]),
                "clamp" => Math.Min(Math.Max(args[0], args[1]), args[2]),
                "quant" => Math.Floor(args[0] / args[1]) * args[1],
                "min" => args.Min(),
                "max" => args.Max(),
                "avg" => args.Average(),
                "sin" => Math.Sin(args[0]),
                "asin" => Math.Asin(args[0]),
                "cos" => Math.Cos(args[0]),
                "acos" => Math.Acos(args[0]),
                "tan" => Math.Tan(args[0]),
                "atan2" => Math.Atan2(args[0], args[1]),
                "rand" => Random.Range((float)args[0], (float)args[1]),
                _ => ResolveXYZ(name, (int)args[0]),
            };
        }
    }
}

