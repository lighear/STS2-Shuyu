using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;
using Shuyu.Characters;

namespace Shuyu.Vfx;

internal static class NPowerVfxLayout
{
    // Calibrated against Shuyu's idle silhouette. The staff is intentionally
    // excluded, and this persistent-effect center is independent of CenterPos.
    private const float ShuyuIdleWidthToHeightFactor = 0.80f;
    private const float ShuyuIdleHeightScale = 1.20f;
    private static readonly Vector2 ShuyuIdleCenterFactor = new(0.475f, 0.46f);

    public static void Resolve(
        Creature creature,
        NCreature creatureNode,
        out Vector2 visualSize,
        out Vector2 localCenter)
    {
        NCreatureVisuals creatureVisuals = creatureNode.Visuals;
        Control creatureBounds = creatureVisuals.Bounds;
        visualSize = creatureBounds.Size;
        localCenter = creatureBounds.Size * 0.5f;

        if (creature.Player?.Character is not ShuyuCharacter)
        {
            return;
        }

        if (creatureVisuals.IsSpineNode)
        {
            float visualHeight = creatureBounds.Size.Y;
            visualSize = new Vector2(
                visualHeight * ShuyuIdleWidthToHeightFactor,
                visualHeight * ShuyuIdleHeightScale);
            localCenter = creatureBounds.Size * ShuyuIdleCenterFactor;
            return;
        }

        if (creatureVisuals.GetCurrentBody() is Sprite2D sprite && sprite.Texture != null)
        {
            visualSize = sprite.Texture.GetSize() * sprite.Scale.Abs();
            localCenter =
                creatureBounds.GetGlobalTransform().AffineInverse()
                * sprite.GlobalPosition;
        }
    }
}
