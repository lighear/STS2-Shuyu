using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace Shuyu.Vfx;

public static class VFXUtil
{
    // Mod 独立的场景缓存（避免被 PreloadManager 清理）
    public static readonly ConcurrentDictionary<string, PackedScene> ModSceneCache = new();

    public static void PreloadScenes()
    {
        //你的场景字符串列表
        var paths = new List<string>
        {
            "res://Shuyu/scenes/vfx_ChillPower_particle.tscn",
            "res://Shuyu/scenes/vfx_ChillPower_background.tscn",
            "res://Shuyu/scenes/vfx_BingWuPower.tscn",
            "res://Shuyu/scenes/vfx/vfx_HanXingZhuiLuo.tscn",
        };
        foreach (var path in paths)
        {
            if (ModSceneCache.ContainsKey(path)) continue;
            var scene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
            if (scene != null)
            {
                ModSceneCache[path] = scene;
            }
        }
    }

    public static Node2D GenVFXNode(string scenePath) {
        if (ModSceneCache.TryGetValue(scenePath, out var modScene)) {
            return modScene.Instantiate<Node2D>();
        }
        return PreloadManager.Cache.GetScene(scenePath).Instantiate<Node2D>();
    }

    public static T GenVFXNode<T>(string scenePath) where T : CanvasItem {
        if (ModSceneCache.TryGetValue(scenePath, out var modScene)) {
            return modScene.Instantiate<T>();
        }
        return PreloadManager.Cache.GetScene(scenePath).Instantiate<T>();
    }

    public static Node2D? PlaySimple(string scenePath, Vector2 position, float lifetime = 2f) {
        if (!TestMode.IsOn && NCombatRoom.Instance != null) {
            Node2D node2D = GenVFXNode(scenePath);
            NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(node2D);
            node2D.GlobalPosition = position;
            
            // 创建定时器，超时后销毁
            SceneTreeTimer timer = node2D.GetTree().CreateTimer(lifetime);
            timer.Timeout += () => {
                if (GodotObject.IsInstanceValid(node2D)) {
                    node2D.QueueFreeSafely();
                }
            };
            return node2D;
        }
        return null;
    }
    
    public static Node2D? PlaySimpleBack(string scenePath, Vector2 position, float lifetime = 2f) {
        if (!TestMode.IsOn && NCombatRoom.Instance != null) {
            Node2D node2D = GenVFXNode(scenePath);
            NCombatRoom.Instance.BackCombatVfxContainer.AddChildSafely(node2D);
            node2D.GlobalPosition = position;
            
            // 创建定时器，超时后销毁
            SceneTreeTimer timer = node2D.GetTree().CreateTimer(lifetime);
            timer.Timeout += () => {
                if (GodotObject.IsInstanceValid(node2D)) {
                    node2D.QueueFreeSafely();
                }
            };
            return node2D;
        }
        return null;
    }
}