namespace FUnity.Runtime.Audio
{
    /// <summary>
    /// サウンドサービスへのグローバルアクセサを提供するロケーター。
    /// ランタイム初期化時に <see cref="FUnitySimpleSoundService"/> を登録し、Visual Scripting から参照できるようにする。
    /// </summary>
    public static class FUnitySoundServiceLocator
    {
        /// <summary>現在アクティブなサウンドサービス。null の場合は未初期化。</summary>
        public static IFUnitySoundService Service { get; set; }
    }
}
