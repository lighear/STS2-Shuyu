using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Interfaces;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class YiLiuXingTaiPower : ModPowerTemplate, ICantDrawForHandFull
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    public async Task CantDrawForHandFull(PlayerChoiceContext choiceContext, int count, Player player)
    {
        if (player.Creature == Owner)
        {
            for (int i = 0; i < count; i++)
            {
                Flash();
                Creature[] targets = Owner.CombatState!.HittableEnemies.ToArray();
                NYiLiuXingTaiWaveVfx.Play(Owner);
                await Cmd.Wait(0.1f);

                foreach (Creature target in targets)
                {
                    NYiLiuXingTaiImpactVfx.Play(Owner, target);
                }

                await CreatureCmd.Damage(choiceContext, targets, Amount, ValueProp.Unpowered, Owner);
            }
        }
    }
}
