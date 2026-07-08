using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Powers;

[RegisterPower]
public class JiBingYinJiPower : ModPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerAssetProfile AssetProfile => new(
        IconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png",
        BigIconPath: $"{Entry.ResPath}/images/powers/{GetType().Name}.png"
    );

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            Flash();
            await PowerCmd.Apply<ChillPower>(new ThrowingPlayerChoiceContext(), Owner, 1, Applier, null);

            Creature? enemy = CombatState?.HittableEnemies
                .Where(enemy => !enemy.HasPower<JiBingYinJiPower>())
                .TakeRandom(1, CombatState.RunState.Rng.CombatTargets)
                .FirstOrDefault();
            if (enemy != null)
            {
                await PowerCmd.Apply<JiBingYinJiPower>(new ThrowingPlayerChoiceContext(), enemy, 1, Applier, null);
            }
        }
    }
}