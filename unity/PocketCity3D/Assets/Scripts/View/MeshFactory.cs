using UnityEngine;

namespace PocketCity
{
    /// <summary>
    /// 외부 에셋 없이 코드로 메시와 머티리얼을 만든다.
    /// 임포트할 파일이 없으므로 프로젝트를 열자마자 바로 실행된다.
    /// </summary>
    public static class MeshFactory
    {
        /// <summary>바닥 중심이 원점인 1x1x1 큐브. 높이를 스케일로 조절하기 편하다.</summary>
        public static Mesh CreateCube()
        {
            Mesh mesh = new Mesh();
            mesh.name = "PC_Cube";

            // y는 0..1 (바닥 기준), x/z는 -0.5..0.5
            Vector3[] v = new Vector3[24];
            Vector3[] nrm = new Vector3[24];

            Vector3 p000 = new Vector3(-0.5f, 0f, -0.5f);
            Vector3 p100 = new Vector3(0.5f, 0f, -0.5f);
            Vector3 p101 = new Vector3(0.5f, 0f, 0.5f);
            Vector3 p001 = new Vector3(-0.5f, 0f, 0.5f);
            Vector3 p010 = new Vector3(-0.5f, 1f, -0.5f);
            Vector3 p110 = new Vector3(0.5f, 1f, -0.5f);
            Vector3 p111 = new Vector3(0.5f, 1f, 0.5f);
            Vector3 p011 = new Vector3(-0.5f, 1f, 0.5f);

            // 윗면
            v[0] = p010; v[1] = p110; v[2] = p111; v[3] = p011;
            // 아랫면
            v[4] = p001; v[5] = p101; v[6] = p100; v[7] = p000;
            // 앞(-z)
            v[8] = p000; v[9] = p100; v[10] = p110; v[11] = p010;
            // 뒤(+z)
            v[12] = p101; v[13] = p001; v[14] = p011; v[15] = p111;
            // 왼(-x)
            v[16] = p001; v[17] = p000; v[18] = p010; v[19] = p011;
            // 오른(+x)
            v[20] = p100; v[21] = p101; v[22] = p111; v[23] = p110;

            for (int i = 0; i < 4; i++) nrm[i] = Vector3.up;
            for (int i = 4; i < 8; i++) nrm[i] = Vector3.down;
            for (int i = 8; i < 12; i++) nrm[i] = Vector3.back;
            for (int i = 12; i < 16; i++) nrm[i] = Vector3.forward;
            for (int i = 16; i < 20; i++) nrm[i] = Vector3.left;
            for (int i = 20; i < 24; i++) nrm[i] = Vector3.right;

            int[] tris = new int[36];
            for (int face = 0; face < 6; face++)
            {
                int t = face * 6;
                int b = face * 4;
                tris[t] = b;
                tris[t + 1] = b + 1;
                tris[t + 2] = b + 2;
                tris[t + 3] = b;
                tris[t + 4] = b + 2;
                tris[t + 5] = b + 3;
            }

            Vector2[] uv = new Vector2[24];
            for (int face = 0; face < 6; face++)
            {
                int b = face * 4;
                uv[b] = new Vector2(0f, 0f);
                uv[b + 1] = new Vector2(1f, 0f);
                uv[b + 2] = new Vector2(1f, 1f);
                uv[b + 3] = new Vector2(0f, 1f);
            }

            mesh.vertices = v;
            mesh.normals = nrm;
            mesh.uv = uv;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>XZ 평면에 놓인 사각형. 지면과 물에 쓴다.</summary>
        public static Mesh CreateQuad(float size)
        {
            Mesh mesh = new Mesh();
            mesh.name = "PC_Quad";
            float h = size * 0.5f;
            mesh.vertices = new Vector3[]
            {
                new Vector3(-h, 0f, -h),
                new Vector3(h, 0f, -h),
                new Vector3(h, 0f, h),
                new Vector3(-h, 0f, h)
            };
            mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>GPU 인스턴싱을 켠 불투명 머티리얼.</summary>
        public static Material CreateMaterial(Color color, float smoothness, float metallic)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Diffuse");

            Material m = new Material(shader);
            m.color = color;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            m.enableInstancing = true;
            return m;
        }

        /// <summary>반투명 머티리얼 (선택 표시용).</summary>
        public static Material CreateTransparent(Color color)
        {
            Material m = CreateMaterial(color, 0.2f, 0f);
            if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 3f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            return m;
        }
    }
}
