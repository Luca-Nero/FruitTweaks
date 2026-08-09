using Il2CppInterop.Runtime;
using Il2CppVoxelMeshGeneration;
using MelonLoader;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FruitTweaks
{
    // ── Blood decal atlas loader ──────────────────────────────────────────────
    internal static class BloodDecalAtlas
    {
        private static Texture2D _atlas;
        private static Shader    _shader;
        private static Material  _sharedMat;
        private static readonly System.Random _rng = new System.Random();

        public static Texture2D Atlas     => _atlas;
        public static Shader    Shader    => _shader;
        public static Material  SharedMat => _sharedMat;

        public static void Init()
        {
            if (_atlas != null && _shader != null) return;

            foreach (var t in Resources.FindObjectsOfTypeAll<Texture2D>())
            {
                if (t.name == "Pixelblood") { _atlas = t; break; }
            }
            if (_atlas != null)
                MelonLogger.Msg("[FruitTweaks] Loaded Pixelblood atlas");
            else
                MelonLogger.Warning("[FruitTweaks] Pixelblood texture not found, blood decals disabled");

            foreach (var s in Resources.FindObjectsOfTypeAll<Shader>())
            {
                if (s.name == "Sprites/Default") { _shader = s; break; }
            }
            if (_shader != null)
                MelonLogger.Msg("[FruitTweaks] Loaded Pixelblood shader");
            else
                MelonLogger.Warning("[FruitTweaks] Pixelblood shader not found, blood decals disabled");

            if (_atlas != null && _shader != null)
            {
                _sharedMat             = new Material(_shader);
                _sharedMat.mainTexture = _atlas;
                _sharedMat.color       = WoundEjectVFX.ColMuscle;
                _sharedMat.renderQueue = 3000;
                UnityEngine.Object.DontDestroyOnLoad(_sharedMat);
                MelonLogger.Msg("[FruitTweaks] Blood decal shared material created");
            }
        }

        public static Rect GetRandomTileRect(int gridCols, int gridRows)
        {
            if (_atlas == null) return Rect.zero;
            int idx = _rng.Next(gridCols * gridRows);
            int col = idx % gridCols;
            int row = idx / gridCols;
            float u = col / (float)gridCols;
            float v = row / (float)gridRows;
            float w = 1f / gridCols;
            float h = 1f / gridRows;
            return new Rect(u, v, w, h);
        }
    }

    internal static class WoundEjectVFX
    {
        internal static int ActiveChunks { get; private set; }
        internal static int ActiveDecals { get; private set; }

        private static readonly System.Random _rng = new System.Random();
        private static bool _layerSetup;

        private sealed class DecalHandle { public bool ShouldEvict; }
        private sealed class ChunkHandle { public bool ShouldEvict; }
        private static readonly LinkedList<DecalHandle> _decalQueue = new LinkedList<DecalHandle>();
        private static readonly LinkedList<ChunkHandle> _chunkQueue = new LinkedList<ChunkHandle>();

        internal static void Update()
        {
            float pressure = FruitLib.FruitPerfMon.PressureLevel;
            float longFps  = FruitLib.FruitPerfMon.LongFps;
            float kFloor   = Config.TargetFPS;

            int toEvict = pressure > 0.25f
                ? Mathf.RoundToInt(Mathf.Clamp01((pressure - 0.25f) / 0.75f) * 10f)
                : 0;

            if (longFps > 0f && longFps < kFloor)
                toEvict = Mathf.Max(toEvict, Mathf.RoundToInt((kFloor - longFps) * Config.CullSpeed));

            if (toEvict <= 0) return;

            if (_decalQueue.Count > 0)
            {
                int n = Mathf.Min(toEvict, _decalQueue.Count);
                var decalNode = _decalQueue.First;
                for (int i = 0; i < n && decalNode != null; i++)
                {
                    decalNode.Value.ShouldEvict = true;
                    decalNode = decalNode.Next;
                }
            }

            if (_chunkQueue.Count > 0)
            {
                int nc = Mathf.Min(Mathf.Max(toEvict / 2, 1), _chunkQueue.Count);
                var chunkNode = _chunkQueue.First;
                for (int i = 0; i < nc && chunkNode != null; i++)
                {
                    chunkNode.Value.ShouldEvict = true;
                    chunkNode = chunkNode.Next;
                }
            }
        }

        internal static readonly Color ColSkin   = new Color(0.55f, 0.38f, 0.28f);
        internal static readonly Color ColMuscle = new Color(0.42f, 0.05f, 0.05f);
        internal static readonly Color ColBone   = new Color(0.56f, 0.53f, 0.47f);
        internal static readonly Color ColOrgan  = new Color(0.46f, 0.17f, 0.23f);

        internal static Color AtlasToColor(RGBAtlasColor c)
        {
            if (RGBAtlasColor.deu(c, RGBAtlasColor.wtv)) return ColSkin;
            if (RGBAtlasColor.deu(c, RGBAtlasColor.wtw)) return ColMuscle;
            if (RGBAtlasColor.deu(c, RGBAtlasColor.wtx)) return ColBone;
            if (RGBAtlasColor.deu(c, RGBAtlasColor.wty)) return ColOrgan;
            return ColMuscle;
        }

        public static void Spawn(Vector3 exitPos, Vector3 dir, List<Color> colors, Rigidbody hostRb = null)
        {
            if (!Config.ExitVoxelEnabled || colors.Count == 0) return;
            if (!_layerSetup)
            {
                Physics.IgnoreLayerCollision(2, 2, true);
                _layerSetup = true;
            }
            Collider[] ragdollCols = hostRb != null
                ? hostRb.transform.root.GetComponentsInChildren<Collider>()
                : null;
            float pressure = FruitLib.FruitPerfMon.PressureLevel;
            int maxCount = Mathf.RoundToInt(Config.ExitVoxelMaxCount * Mathf.Clamp01(1f - pressure));
            int count = Mathf.Min(colors.Count, maxCount);
            for (int i = 0; i < count; i++)
                MelonCoroutines.Start(AnimateChunk(exitPos, dir, colors[i], ragdollCols));
        }

        private static IEnumerator AnimateChunk(Vector3 p0, Vector3 forward, Color col, Collider[] ragdollCols)
        {
            ActiveChunks++;
            var handle = new ChunkHandle();
            var node   = _chunkQueue.AddLast(handle);

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name  = "VFX_WoundEject";
            go.layer = 2;

            go.transform.localScale = Vector3.one * 0.05f;
            go.transform.position   = p0;
            var boxCol = go.GetComponent<BoxCollider>();
            if (boxCol != null) boxCol.enabled = false;

            var mat      = new Material(Shader.Find("Unlit/Color")) { color = col };
            var renderer = go.GetComponent<Renderer>();
            renderer.material = mat;
            renderer.enabled  = Config.ExitVoxelVisible;

            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.005f;
            rb.drag = 0.3f;
            rb.angularDrag = 0.5f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation          = RigidbodyInterpolation.Interpolate;

            Vector3 spread = UnityEngine.Random.insideUnitSphere * Config.ExitVoxelSpread;
            rb.linearVelocity  = (forward + spread).normalized * Config.ExitVoxelSpeed * (0.5f + (float)_rng.NextDouble());
            rb.angularVelocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(5f, 15f);

            bool      stuck       = false;
            Rigidbody hostRb      = null;
            Vector3   localOffset = Vector3.zero;

            yield return new WaitForSeconds(0.1f);
            if (go == null || handle.ShouldEvict)
            {
                _chunkQueue.Remove(node);
                ActiveChunks--;
                if (go  != null) GameObject.Destroy(go);
                if (mat != null) GameObject.Destroy(mat);
                yield break;
            }
            if (boxCol != null)
            {
                boxCol.enabled = true;
                if (ragdollCols != null)
                    foreach (var c in ragdollCols)
                        if (c != null) Physics.IgnoreCollision(boxCol, c);
            }

            const float kFast    = 2f;
            const float kSettled = 0.05f;
            float       elapsed  = 0f;

            while (elapsed < 2f && go != null && !stuck && !handle.ShouldEvict)
            {
                float   dt    = Time.deltaTime;
                elapsed += dt;
                Vector3 vel   = rb.linearVelocity;
                float   speed = vel.magnitude;

                if (speed > kFast)
                {
                    float castRange = speed * dt + 0.02f;
                    if (Physics.Raycast(go.transform.position, vel.normalized, out RaycastHit hit, castRange)
                        && hit.collider.gameObject != go)
                    {
                        rb.linearVelocity  = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.isKinematic     = true;

                        SpawnBloodDecals(hit.point, hit.normal, hit.collider.transform);

                        if (BulletWound.IsLimb(hit.collider.gameObject))
                        {
                            var limbRb = hit.collider.GetComponentInParent<Rigidbody>();
                            if (limbRb != null)
                            {
                                stuck       = true;
                                hostRb      = limbRb;
                                localOffset = hostRb.transform.InverseTransformPoint(hit.point);

                                var ownCollider = go.GetComponent<Collider>();
                                if (ownCollider != null) ownCollider.enabled = false;
                            }
                        }
                    }
                }
                else if (speed < kSettled)
                {
                    rb.isKinematic = true;
                    break;
                }

                yield return null;
            }

            if (go == null || handle.ShouldEvict)
            {
                _chunkQueue.Remove(node);
                ActiveChunks--;
                if (go  != null) { rb.isKinematic = true; GameObject.Destroy(go); }
                if (mat != null) GameObject.Destroy(mat);
                yield break;
            }

            if (!rb.isKinematic && !stuck)
            {
                rb.linearVelocity  = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic     = true;
            }

            Vector3 fullScale   = go.transform.localScale;
            float   lifetime    = Config.ExitVoxelLifetime;
            float   settleElap  = 0f;
            float   fadeStart   = lifetime * 0.65f;

            while (settleElap < lifetime && go != null && !handle.ShouldEvict)
            {
                settleElap += Time.deltaTime;

                if (stuck && hostRb != null)
                    go.transform.position = hostRb.transform.TransformPoint(localOffset);

                if (settleElap > fadeStart)
                {
                    float ft = (settleElap - fadeStart) / (lifetime - fadeStart);
                    go.transform.localScale = Vector3.Lerp(fullScale, Vector3.zero, ft);
                }

                yield return null;
            }

            _chunkQueue.Remove(node);
            ActiveChunks--;
            if (go  != null) GameObject.Destroy(go);
            if (mat != null) GameObject.Destroy(mat);
        }

        public static void SpawnBloodDecals(Vector3 hitPoint, Vector3 hitNormal, Transform surfaceTransform)
        {
            if (!Config.BloodDecalEnabled) return;
            if (BloodDecalAtlas.Atlas == null) return;

            Vector3 tangent = Vector3.Cross(hitNormal, Vector3.up);
            if (tangent.sqrMagnitude < 0.01f) tangent = Vector3.Cross(hitNormal, Vector3.forward);
            tangent = tangent.normalized;
            Vector3 bitangent = Vector3.Cross(tangent, hitNormal).normalized;

            float radius = Config.BloodDecalSize;

            float pressure = FruitLib.FruitPerfMon.PressureLevel;
            int   target   = Mathf.RoundToInt(Config.BloodDecalCountPerHit * Mathf.Clamp01(1f - pressure * 2f));
            if (target <= 0) return;

            const float kCornerCastDist  = 0.05f;
            const int   kRetryMultiplier = 3;
            const int   kSubdivGrid      = 6; 
            int budget = target * kRetryMultiplier;
            int placed = 0;

            for (int attempt = 0; attempt < budget && placed < target; attempt++)
            {
                float angle = (float)_rng.NextDouble() * Mathf.PI * 2f;
                float dist  = radius * Mathf.Pow((float)_rng.NextDouble(), 0.7f) * (0.3f + (float)_rng.NextDouble() * 1.4f);

                Vector3 candidate = hitPoint
                    + tangent    * (Mathf.Cos(angle) * dist)
                    + bitangent  * (Mathf.Sin(angle) * dist);

                if (!Physics.Raycast(candidate + hitNormal * 0.05f, -hitNormal, out RaycastHit hit, 0.15f))
                    continue;

                float size = Config.BloodDecalSize * 0.35f * (0.7f + UnityEngine.Random.value * 0.6f);
                float rotZ = UnityEngine.Random.Range(0f, 360f);
                Quaternion rot = Quaternion.FromToRotation(Vector3.forward, hit.normal)
                               * Quaternion.Euler(0f, 0f, rotZ);
                float h0 = size * 0.5f;
                Rect uvRect = BloodDecalAtlas.GetRandomTileRect(Config.BloodAtlasCols, Config.BloodAtlasRows);

                bool c0 = CornerHasSurface(hit.point, rot, -h0, -h0, hit.normal, kCornerCastDist);
                bool c1 = CornerHasSurface(hit.point, rot,  h0, -h0, hit.normal, kCornerCastDist);
                bool c2 = CornerHasSurface(hit.point, rot,  h0,  h0, hit.normal, kCornerCastDist);
                bool c3 = CornerHasSurface(hit.point, rot, -h0,  h0, hit.normal, kCornerCastDist);

                Vector3[] verts;
                Vector2[] uvs;
                int[]     tris;

                if (c0 && c1 && c2 && c3)
                {
                    verts = new Vector3[] {
                        new Vector3(-h0, -h0, 0f),
                        new Vector3( h0, -h0, 0f),
                        new Vector3( h0,  h0, 0f),
                        new Vector3(-h0,  h0, 0f),
                    };
                    uvs = new Vector2[] {
                        new Vector2(uvRect.xMin, uvRect.yMin),
                        new Vector2(uvRect.xMax, uvRect.yMin),
                        new Vector2(uvRect.xMax, uvRect.yMax),
                        new Vector2(uvRect.xMin, uvRect.yMax),
                    };
                    tris = new int[] { 0, 1, 2, 0, 2, 3 };
                }
                else
                {
                    int gv = kSubdivGrid + 1;
                    bool[] hits = new bool[gv * gv];
                    for (int j = 0; j < gv; j++)
                    {
                        float ly = Mathf.Lerp(-h0, h0, j / (float)kSubdivGrid);
                        for (int i = 0; i < gv; i++)
                        {
                            float lx = Mathf.Lerp(-h0, h0, i / (float)kSubdivGrid);
                            Vector3 wp = hit.point + rot * new Vector3(lx, ly, 0f);
                            hits[j * gv + i] = Physics.Raycast(wp + hit.normal * 0.02f, -hit.normal, kCornerCastDist + 0.02f);
                        }
                    }

                    var vList  = new List<Vector3>(gv * gv);
                    var uvList = new List<Vector2>(gv * gv);
                    var tList  = new List<int>(kSubdivGrid * kSubdivGrid * 6);
                    int[] indexMap = new int[gv * gv];
                    for (int k = 0; k < indexMap.Length; k++) indexMap[k] = -1;

                    for (int j = 0; j < kSubdivGrid; j++)
                    {
                        for (int i = 0; i < kSubdivGrid; i++)
                        {
                            int a = j * gv + i;
                            int b = j * gv + (i + 1);
                            int c = (j + 1) * gv + (i + 1);
                            int d = (j + 1) * gv + i;
                            if (!(hits[a] && hits[b] && hits[c] && hits[d])) continue;

                            int va = UseGridVertex(a, gv, kSubdivGrid, h0, uvRect, vList, uvList, indexMap);
                            int vb = UseGridVertex(b, gv, kSubdivGrid, h0, uvRect, vList, uvList, indexMap);
                            int vc = UseGridVertex(c, gv, kSubdivGrid, h0, uvRect, vList, uvList, indexMap);
                            int vd = UseGridVertex(d, gv, kSubdivGrid, h0, uvRect, vList, uvList, indexMap);
                            tList.Add(va); tList.Add(vb); tList.Add(vc);
                            tList.Add(va); tList.Add(vc); tList.Add(vd);
                        }
                    }

                    if (tList.Count == 0) continue;

                    verts = vList.ToArray();
                    uvs   = uvList.ToArray();
                    tris  = tList.ToArray();
                }

                MelonCoroutines.Start(AnimateBloodDecal(hit.point, hit.normal, surfaceTransform, verts, uvs, tris, rotZ));
                placed++;
            }
        }

        private static bool CornerHasSurface(Vector3 origin, Quaternion rot, float lx, float ly, Vector3 normal, float castDist)
        {
            Vector3 wp = origin + rot * new Vector3(lx, ly, 0f);
            return Physics.Raycast(wp + normal * 0.02f, -normal, castDist + 0.02f);
        }

        private static int UseGridVertex(int idx, int gv, int grid, float h0, Rect uvRect,
                                         List<Vector3> verts, List<Vector2> uvs, int[] indexMap)
        {
            if (indexMap[idx] != -1) return indexMap[idx];
            int gi = idx % gv, gj = idx / gv;
            float lx = Mathf.Lerp(-h0, h0, gi / (float)grid);
            float ly = Mathf.Lerp(-h0, h0, gj / (float)grid);
            float u  = Mathf.Lerp(uvRect.xMin, uvRect.xMax, gi / (float)grid);
            float v  = Mathf.Lerp(uvRect.yMin, uvRect.yMax, gj / (float)grid);
            indexMap[idx] = verts.Count;
            verts.Add(new Vector3(lx, ly, 0f));
            uvs.Add(new Vector2(u, v));
            return indexMap[idx];
        }

        private static IEnumerator AnimateBloodDecal(Vector3 hitPoint, Vector3 hitNormal, Transform surfaceTransform,
                                                     Vector3[] verts, Vector2[] uvs, int[] tris, float rotZ)
        {
            if (BloodDecalAtlas.Atlas == null || BloodDecalAtlas.Shader == null) yield break;
            if (verts == null || tris == null || tris.Length == 0) yield break;
            ActiveDecals++;

            var handle = new DecalHandle();
            var node   = _decalQueue.AddLast(handle);

            bool     ownsMat = BloodDecalAtlas.SharedMat == null;
            Material mat     = ownsMat ? new Material(BloodDecalAtlas.Shader) : null;
            if (ownsMat)
            {
                mat.mainTexture = BloodDecalAtlas.Atlas;
                mat.color       = ColMuscle;
                mat.renderQueue = 3000;
            }

            var go = new GameObject("VFX_BloodDecal");
            go.layer = LayerMask.NameToLayer("Default");

            go.transform.position = hitPoint + hitNormal * 0.001f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.forward, hitNormal) * Quaternion.Euler(0, 0, rotZ);

            var mesh = new Mesh();
            mesh.vertices  = verts;
            mesh.uv        = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();

            go.AddComponent<MeshFilter>().sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ownsMat ? mat : BloodDecalAtlas.SharedMat;
            mr.receiveShadows = false;

            if (surfaceTransform != null)
                go.transform.SetParent(surfaceTransform, true);

            float     lifetime  = Config.BloodDecalLifetime;
            float     elapsed   = 0f;
            float     fadeStart = lifetime * 0.7f;
            Vector3   fullScale = go.transform.localScale;

            while (elapsed < lifetime && go != null && !handle.ShouldEvict)
            {
                elapsed += Time.deltaTime;

                if (elapsed > fadeStart)
                {
                    float t = (elapsed - fadeStart) / (lifetime - fadeStart);
                    go.transform.localScale = Vector3.Lerp(fullScale, Vector3.zero, t);
                }

                yield return null;
            }
           
            _decalQueue.Remove(node);
            ActiveDecals--;

            if (go != null)
            {
                GameObject.Destroy(mesh);
                if (ownsMat && mat != null) GameObject.Destroy(mat);
                GameObject.Destroy(go);
            }
        }
    }
}
