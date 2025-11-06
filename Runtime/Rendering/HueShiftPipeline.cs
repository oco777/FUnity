using UnityEngine;

namespace FUnity.Runtime.Rendering
{
    /// <summary>
    /// <para>HueShift.shader を利用して色相回転を行うための軽量レンダリングパイプライン。</para>
    /// <para>UI Toolkit の VisualElement に直接マテリアルを割り当てられない制約を補い、RenderTexture へブリットした結果を返します。</para>
    /// </summary>
    public sealed class HueShiftPipeline
    {
        /// <summary>色相回転用のマテリアル。HueShift.shader を元に生成し、毎フレーム使い回す。</summary>
        private Material m_Material;

        /// <summary>ブリット結果を書き込む RenderTexture。ソース画像の解像度に合わせて確保・再利用する。</summary>
        private RenderTexture m_RenderTexture;

        /// <summary>
        /// HueShift.shader を検索してマテリアルを初期化する。シェーダーが見つからない場合は以降の描画をスキップする。
        /// </summary>
        public HueShiftPipeline()
        {
            var shader = Shader.Find("FUnity/Effects/HueShift");
            if (shader == null)
            {
                Debug.LogError("[FUnity.HueShiftPipeline] HueShift シェーダーを検出できませんでした。RenderTexture への書き出しを停止します。");
                return;
            }

            m_Material = new Material(shader)
            {
                name = "FUnity_HueShift_Material",
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        /// <summary>
        /// <para>指定したテクスチャを色相回転し、結果を RenderTexture として返す。</para>
        /// <para>シェーダーまたはテクスチャが無効な場合は null を返し、呼び出し側でフォールバック処理を行う。</para>
        /// </summary>
        /// <param name="source">元となるテクスチャ。null の場合は処理を中断する。</param>
        /// <param name="hueDegrees">適用する色相角。0～360 度を想定する。</param>
        /// <returns>色相を回転させた RenderTexture。生成できなかった場合は null。</returns>
        public RenderTexture Render(Texture source, float hueDegrees)
        {
            if (source == null)
            {
                Debug.LogWarning("[FUnity.HueShiftPipeline] ソーステクスチャが null のため、色相回転をスキップします。");
                return null;
            }

            if (m_Material == null)
            {
                Debug.LogWarning("[FUnity.HueShiftPipeline] HueShift マテリアルが初期化されていないため、RenderTexture を返せません。");
                return null;
            }

            EnsureRenderTexture(source.width, source.height);
            if (m_RenderTexture == null)
            {
                return null;
            }

            m_Material.SetFloat("_HueDegrees", hueDegrees);
            Graphics.Blit(source, m_RenderTexture, m_Material);
            return m_RenderTexture;
        }

        /// <summary>
        /// 内部で保持しているマテリアルと RenderTexture を破棄する。ActorView.OnDestroy などから呼び出すことを想定する。
        /// </summary>
        public void Dispose()
        {
            if (m_RenderTexture != null)
            {
                m_RenderTexture.Release();
                Object.Destroy(m_RenderTexture);
                m_RenderTexture = null;
            }

            if (m_Material != null)
            {
                Object.Destroy(m_Material);
                m_Material = null;
            }
        }

        /// <summary>
        /// <para>ソーステクスチャの解像度を基に RenderTexture を確保する。</para>
        /// <para>解像度が変化した場合は既存の RT を破棄し、再生成する。</para>
        /// </summary>
        /// <param name="width">ソーステクスチャの幅。</param>
        /// <param name="height">ソーステクスチャの高さ。</param>
        private void EnsureRenderTexture(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning("[FUnity.HueShiftPipeline] 不正な RenderTexture サイズが指定されました。");
                return;
            }

            if (m_RenderTexture != null && (m_RenderTexture.width != width || m_RenderTexture.height != height))
            {
                m_RenderTexture.Release();
                Object.Destroy(m_RenderTexture);
                m_RenderTexture = null;
            }

            if (m_RenderTexture == null)
            {
                m_RenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
                {
                    name = "FUnity_HueShift_RT",
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                };
                m_RenderTexture.Create();
            }
        }
    }
}
