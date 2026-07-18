using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace Shuyu.Vfx;

public static class VFXUtil
{
    public static readonly string PowerVfxPath = "res://Shuyu/scenes/powers";
    public static readonly string CardVfxPath = "res://Shuyu/scenes/cards";

    // Mod 独立的场景缓存（避免被 PreloadManager 清理）
    public static readonly ConcurrentDictionary<string, PackedScene> ModSceneCache = new();

    public static void PreloadScenes()
    {
        //你的场景字符串列表
        var paths = new List<string>
        {
            $"{PowerVfxPath}/vfx_IceThornsPower.tscn",
            $"{PowerVfxPath}/vfx_IceShieldPower.tscn",
            $"{PowerVfxPath}/vfx_ChillPower_particle.tscn",
            $"{PowerVfxPath}/vfx_ChillPower_background.tscn",
            $"{PowerVfxPath}/vfx_BingWuPower.tscn",
            $"{PowerVfxPath}/vfx_WanBiBuPoPower_ring.tscn",
            $"{PowerVfxPath}/vfx_LianXuJingGePower.tscn",
            $"{CardVfxPath}/vfx_HanXingZhuiLuo.tscn",
            $"{CardVfxPath}/vfx_BoWenGongZhen.tscn",
            $"{CardVfxPath}/vfx_LinZhiTong.tscn",
            $"{CardVfxPath}/vfx_BingJingMoZhen.tscn",
            $"{CardVfxPath}/vfx_JueWangYongChang.tscn",
            $"{CardVfxPath}/vfx_HuanRaoDaJi.tscn",
            $"{CardVfxPath}/vfx_SuiLieWuSheng.tscn",
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
    
    public static void FitVFX(this Node2D node, Vector2 nodeStartPos, Vector2 nodeEndPos, Vector2 sceneStartPos, Vector2 sceneEndPos)
    {
        Vector2 val = nodeStartPos - nodeEndPos;
        Vector2 val2 = sceneStartPos - sceneEndPos;
        float rotation = ((Vector2)(val2)).Angle() - ((Vector2)(val)).Angle();
        float num = ((Vector2)(val2)).Length() / ((Vector2)(val)).Length();
        node.Rotation = rotation;
        node.Scale = Vector2.One * num;
    }
}
