using System;
using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
using Polaris.Res;

namespace Polaris
{
    /// <summary>
    /// 模组入口的基类：继承它代替直接继承 <c>BaseUnityPlugin</c>，Awake 里该做的杂事就都替你做了——
    /// 异常兜底、逐类安全应用本程序集的 Harmony 补丁、拿到资源句柄、退出时撤补丁。
    /// 设置项（<c>[PolarisSetting]</c>）与属性订阅的事件（<c>[PolarisSubscribe]</c>）由 Core 在所有模组 Awake 之后统一扫描，无需任何代码。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 用法：
    /// <code>
    /// [BepInPlugin("me.mymod", "My Mod", "1.0.0")]
    /// [BepInDependency("Polaris.Core")]   // BepInEx 不读继承来的依赖，这一行必须写在你自己的类上
    /// public class MyMod : PolarisMod
    /// {
    ///     protected override void OnLoad() { /* 补丁已应用；在这里初始化 */ }
    /// }
    /// </code>
    /// </para>
    /// <para>不要再自己写 <c>Awake</c>/<c>Start</c>/<c>OnDestroy</c>：同名方法会盖住基类的，Unity 只会调到最派生的那一个。要做的事放进下面的 <c>OnLoad</c>/<c>OnReady</c>/<c>OnUnload</c>。</para>
    /// </remarks>
    public abstract class PolarisMod : BaseUnityPlugin
    {
        /// <summary>本模组专属的 Harmony 实例（ID 为插件 GUID）。</summary>
        protected Harmony Harmony { get; private set; }

        /// <summary>
        /// 本模组的资源句柄：根目录是 dll 所在目录下、与 dll 同名的子文件夹（见 <see cref="Infra.PathsAPI.DefaultResRootOf"/>）。
        /// 第一次访问时才创建。
        /// </summary>
        protected ModResources Res => res ??= ResAPI.For(Info.Metadata.GUID, PolarisAPI.Paths.DefaultResRootOf(GetType().Assembly));

        ModResources res;

        /// <summary>补丁已应用后调用，相当于 <c>Awake</c>。异常会被上报并吞掉，不会让整个模组起不来。</summary>
        protected virtual void OnLoad() { }

        /// <summary>所有模组 Awake 之后（Unity <c>Start</c> 阶段）调用；需要依赖其它模组或设置值已读出时用这个。</summary>
        protected virtual void OnReady() { }

        /// <summary>模组被销毁（游戏退出）时调用，之后本类会撤掉自己的补丁。</summary>
        protected virtual void OnUnload() { }

        void Awake()
        {
            Harmony = new Harmony(Info.Metadata.GUID);
            PolarisAPI.Patching.ApplyAll(Harmony, GetType().Assembly, Logger);
            PolarisAPI.Errors.Guard(OnLoad, $"{Info.Metadata.Name}.OnLoad", GetType().Assembly);
        }

        void Start()
        {
            PolarisAPI.Errors.Guard(OnReady, $"{Info.Metadata.Name}.OnReady", GetType().Assembly);
        }

        void OnDestroy()
        {
            PolarisAPI.Errors.Guard(OnUnload, $"{Info.Metadata.Name}.OnUnload", GetType().Assembly);
            Harmony?.UnpatchSelf();
        }
    }
}
