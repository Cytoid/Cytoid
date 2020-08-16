using System.Collections.Generic;
using System.Text.RegularExpressions;

/*
 * Credits: https://forum.unity.com/threads/adjustable-character-spacing-free-script.288277/page-2#post-5453679
 */
namespace UnityEngine.UI
{
    [AddComponentMenu("UI/Letter Spacing", 14)]
    public class LetterSpacing : BaseMeshEffect
    {
        [SerializeField, Range(0, 100)] private float spacing;

        private const string SupportedTagRegexPattersn =
            @"<b>|</b>|<i>|</i>|<size=.*?>|</size>|<color=.*?>|</color>|<material=.*?>|</material>";

        protected LetterSpacing()
        {
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            Spacing = spacing;
            base.OnValidate();
        }
#endif
        
        private Text TextComponent
        {
            get
            {
                if (text == null)
                {
                    text = GetComponent<Text>();
                }

                return text;
            }
        }
        private Text text;

        private string[] GetLines()
        {
            var lineInfos = TextComponent.cachedTextGenerator.lines;
            var lines = new string[lineInfos.Count];
            for (var i = 0; i < lineInfos.Count; i++)
            {
                if ((i + 1) < lineInfos.Count)
                {
                    var length = (lineInfos[i + 1].startCharIdx - 1) - lineInfos[i].startCharIdx;
                    lines[i] = TextComponent.text.Substring(lineInfos[i].startCharIdx, length);
                }
                else
                {
                    lines[i] = TextComponent.text.Substring(lineInfos[i].startCharIdx);
                }
            }

            return lines;
        }

        public float Spacing
        {
            get => spacing;
            set
            {
                if (spacing == value) return;
                spacing = value;
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        public void ModifyVertices(List<UIVertex> verts)
        {
            if (!IsActive()) return;

            if (TextComponent == null)
            {
                Debug.LogWarning("LetterSpacing: Missing Text component");
                return;
            }

            var lines = GetLines();

            var letterOffset = Spacing * TextComponent.fontSize / 100f;
            float alignmentFactor = 0;
            var glyphIdx = 0;

            switch (TextComponent.alignment)
            {
                case TextAnchor.LowerLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.UpperLeft:
                    alignmentFactor = 0f;
                    break;

                case TextAnchor.LowerCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.UpperCenter:
                    alignmentFactor = 0.5f;
                    break;

                case TextAnchor.LowerRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.UpperRight:
                    alignmentFactor = 1f;
                    break;
            }

            for (var lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                var line = WithoutRichText(lines[lineIdx]);
                var lineLength = line.Length;

                var lineOffset = (lineLength - 1) * letterOffset * alignmentFactor;

                for (int charIdx = 0, charPositionIndex = 0; charIdx < line.Length; charIdx++, charPositionIndex++)
                {
                    if (line[charIdx] == ' ') continue;

                    var idx1 = glyphIdx * 6 + 0;
                    var idx2 = glyphIdx * 6 + 1;
                    var idx3 = glyphIdx * 6 + 2;
                    var idx4 = glyphIdx * 6 + 3;
                    var idx5 = glyphIdx * 6 + 4;
                    var idx6 = glyphIdx * 6 + 5;

                    // Check for truncated text (doesn't generate verts for all characters)
                    if (idx4 > verts.Count - 1) return;

                    var vert1 = verts[idx1];
                    var vert2 = verts[idx2];
                    var vert3 = verts[idx3];
                    var vert4 = verts[idx4];
                    var vert5 = verts[idx5];
                    var vert6 = verts[idx6];

                    var pos = Vector3.right * (letterOffset * charPositionIndex - lineOffset);

                    vert1.position += pos;
                    vert2.position += pos;
                    vert3.position += pos;
                    vert4.position += pos;
                    vert5.position += pos;
                    vert6.position += pos;

                    verts[idx1] = vert1;
                    verts[idx2] = vert2;
                    verts[idx3] = vert3;
                    verts[idx4] = vert4;
                    verts[idx5] = vert5;
                    verts[idx6] = vert6;

                    glyphIdx++;
                }
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive())
                return;

            var vertexList = new List<UIVertex>();
            vh.GetUIVertexStream(vertexList);

            ModifyVertices(vertexList);

            vh.Clear();
            vh.AddUIVertexTriangleStream(vertexList);
        }

        private static string WithoutRichText(string line)
        {
            line = Regex.Replace(line, SupportedTagRegexPattersn, "");
            return line;
        }
    }
}