using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Polaris.API
{
    /// <summary>热键绑定表，由 Core 的 Update 每帧推进；见 <see cref="PolarisAPI.Game.Input.Bind"/>。</summary>
    internal static class Hotkeys
    {
        sealed class Binding : IDisposable
        {
            internal Func<Key> Key;
            internal Action Action;

            public void Dispose() => bindings.Remove(this);
        }

        static readonly List<Binding> bindings = [];

        internal static IDisposable Add(Func<Key> key, Action action)
        {
            var binding = new Binding { Key = key, Action = action };
            bindings.Add(binding);
            return binding;
        }

        internal static void Tick()
        {
            if (bindings.Count == 0)
            {
                return;
            }

            // 回调里可能增删绑定，遍历副本。
            foreach (Binding binding in bindings.ToArray())
            {
                try
                {
                    Key key = binding.Key();
                    if (key != Key.None && PolarisAPI.Game.Input.WasPressed(key))
                    {
                        binding.Action();
                    }
                }
                catch (Exception ex)
                {
                    PolarisAPI.Errors.Report(ex, "a hotkey binding", binding.Action.Method?.DeclaringType?.Assembly);
                }
            }
        }
    }
}
