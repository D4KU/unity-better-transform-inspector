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
    /// <summary>
    /// Draws text fields to manipulate a <see cref="Transform"/> via an
    /// extended expression engine
    /// </summary>
    public class ExpressiveTransformEditor : IContext
    {
        /// <summary>
        /// Difference below which two numbers are considered equal
        /// </summary>
        private const float DELTA = 0.0001f;

        /// <summary>
        /// Caches for expressions typed into the inspector. Each entry
        /// corresponds to a value of <see cref="vectorIndex"/>.
        /// </summary>
        private readonly string[] textX = new string[6];
        private readonly string[] textY = new string[6];
        private readonly string[] textZ = new string[6];

        /// <summary>
        /// Index of the currently processed Transform inside the list
        /// of selected targets
        /// </summary>
        private int selectionIndex;

        /// <summary>
        /// Index of the currently processed vector value:
        /// 1 Local Position
        /// 2 Local Rotation
        /// 3 Local Scale
        /// 4 Global Position
        /// 5 Global Rotation
        /// 6 Lossy Scale
        /// </summary>
        private int vectorIndex;

        /// <summary>
        /// Transforms manipulated by this Editor
        /// </summary>
        private Object[] targets;

        /// <summary>
        /// Initial values of each Transform when this Editor was created.
        /// Each entry in the vector array corresponds to a value of
        /// <see cref="vectorIndex"/>.
        /// </summary>
        private List<Vector3[]> originals;

        /// <summary>
        /// The currently processed Transform
        /// </summary>
        private Transform Current => (Transform)targets[selectionIndex];

        public ExpressiveTransformEditor()
        {
            Undo.undoRedoPerformed += Clear;
        }

        ~ExpressiveTransformEditor()
        {
            Undo.undoRedoPerformed -= Clear;
        }

        /// <summary>
        /// Clear cached values
        /// </summary>
        public void Clear()
        {
            originals = null;
            Array.Clear(textX, 0, textX.Length);
            Array.Clear(textY, 0, textY.Length);
            Array.Clear(textZ, 0, textZ.Length);
        }

        /// <summary>
        /// Must be called before the first call to Draw*Vectors()
        /// </summary>
        /// <param name="targets">
        /// Transforms manipulated by this Editor
        /// </param>
        public void Initialize(Object[] targets)
        {
            this.targets = targets;

            // Store initial values that variables and functions in
            // expressions reference
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

        /// <summary>
        /// Draw a fake vector field for local position, rotation, and scale,
        /// respectively
        /// </summary>
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

        /// <summary>
        /// Draw a fake vector field for global position, rotation, and scale,
        /// respectively
        /// </summary>
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

        /// <summary>
        /// Draw three text fields that look like a vector field but can
        /// actually process arbitrary text
        /// </summary>
        /// <param name="getter">
        /// Function to get the vector to show from a Transform
        /// </param>
        /// <param name="setter">
        /// Function to set the edited vector in a Transform
        /// </param>
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

            // Pick label for the fake vector field
            string label = vectorIndex switch
            {
                0 or 3 => "Position",
                1 or 4 => "Rotation",
                2 or 5 => "Scale",
                _ => string.Empty,
            };

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.MinWidth(10));

            Node nodeX = DrawTextField("x", first.x, sameX, ref textX[vectorIndex]);
            Node nodeY = DrawTextField("y", first.y, sameY, ref textY[vectorIndex]);
            Node nodeZ = DrawTextField("z", first.z, sameZ, ref textZ[vectorIndex]);

            EditorGUILayout.EndHorizontal();

            // No text has been changed in the current frame. Nothing to do.
            if (nodeX == null && nodeY == null && nodeZ == null)
                return;

            // Create Undo step before manipulating Transforms
            Undo.RecordObjects(targets, "Set Transform");

            for (selectionIndex = 0; selectionIndex < targets.Length; selectionIndex++)
            {
                Transform t = Current;

                // Update the changed coordinate with the evaluated value and
                // set the vector
                Vector3 v = getter(t);
                if (nodeX != null) v.x = (float)nodeX.Eval(this);
                if (nodeY != null) v.y = (float)nodeY.Eval(this);
                if (nodeZ != null) v.z = (float)nodeZ.Eval(this);
                setter(t, v);
            }
        }

        /// <summary>
        /// Draw a text input field for a vector coordinate
        /// </summary>
        /// <param name="value">
        /// Value of this coordinate
        /// </param>
        /// <param name="same">
        /// True if all targets have the same value at this coordinate
        /// </param>
        /// <param name="text">
        /// Expression typed into this field so far
        /// </param>
        /// <returns>
        /// The tree of the parsed expression typed into the field.
        /// Null when the expression couldn't be parsed or the text hasn't
        /// been edited this frame.
        /// </returns>
        private Node DrawTextField(string label, float value, bool same, ref string text)
        {
            /// Draw x/y/z label
            EditorGUILayout.LabelField(
                label: label.ToUpper(),
                style: new(GUI.skin.label) { alignment = TextAnchor.MiddleCenter },
                options: GUILayout.MaxWidth(14));

            /// If no text has been typed so far, insert the value if it is
            /// equal for all targets, or a variable otherwise
            if (string.IsNullOrEmpty(text))
                text = same ? value.ToString() : label;

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

        /// <summary>
        /// Get the value of a variable referring to a vector coordinate
        /// </summary>
        /// <param name="varName">
        /// Name of the variable to resolve
        /// </param>
        /// <param name="objectIndex">
        /// Index into the list of targets to get the object of which to
        /// get the vector coordinate from.
        /// </param>
        private float ResolveXYZ(string varName, int objectIndex)
        {
            // Negative indices count from the back
            if (objectIndex < 0)
                objectIndex += targets.Length;

            // True when a local vector is requested
            bool localVector = vectorIndex < 3;

            // If variable starts with a special letter, then access position,
            // rotation, or scale values directly. If not the currently edited
            // vector is assumed.
            int vi = char.ToLower(varName[0]) switch
            {
                'p' => localVector ? 0 : 3,
                'r' => localVector ? 1 : 4,
                's' => localVector ? 2 : 5,
                _ => vectorIndex,
            };

            char last = varName[varName.Length - 1];

            // Upper characters switch the space: access global values in
            // local inspector fields and local values in global inspector
            // fields
            if (char.IsUpper(last))
                vi += localVector ? 3 : -3;

            Vector3 v = originals[objectIndex][vi];

            return char.ToLower(last) switch
            {
                'x' => v.x,
                'y' => v.y,
                'z' => v.z,
                _ => throw new ArgumentException(),
            };
        }

        /// <inheritdoc cref="IContext.ResolveVariable"/>
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

        /// <inheritdoc cref="IContext.CallFunction"/>
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

