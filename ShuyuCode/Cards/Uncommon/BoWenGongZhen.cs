using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.ValueProps;
using Shuyu.Characters;
using Shuyu.Powers;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class BoWenGongZhen : ModCardTemplate
    {
        public BoWenGongZhen() : base(
            baseCost: 0,
            CardType.Attack,
            CardRarity.Uncommon,
            TargetType.AnyEnemy)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<DynamicVar> CanonicalVars => [
            new DynamicVar("Multiple", 3),
            new CalculationBaseVar(0m),
            new ExtraDamageVar(1m),
            new CalculatedDamageVar(ValueProp.Move).WithMultiplier((CardModel card, Creature? _) =>
            {
                return card.Owner.Creature.Block * card.DynamicVars["Multiple"].IntValue / ((BoWenGongZhen)card).CardPlayDivisor;
            }),
        ];
        
        private int CardPlayDivisor
        {
            get
            {
                int num = CombatManager.Instance.History.Entries.OfType<CardPlayFinishedEntry>().Count((CardPlayFinishedEntry e) => e.HappenedThisTurn(base.CombatState) && e.CardPlay.Card.Owner == base.Owner);
                return Convert.ToInt32(Math.Pow(2,Math.Min(num,4)));
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            Node2D? vfxNode = VFXUtil.PlaySimple("res://Shuyu/scenes/vfx/vfx_BoWenGongZhen.tscn", NCombatRoom.Instance?.GetCreatureNode(cardPlay.Target)?.VfxSpawnPosition ?? Vector2.Zero, 2f);
            if (vfxNode != null)
            {
                if (DynamicVars.CalculatedDamage.Calculate(cardPlay.Target) > 20)
                {
                    vfxNode.Scale = new Vector2((float)0.5, (float)0.5);
                }
                else
                {
                    vfxNode.Scale = new Vector2((float)0.25, (float)0.25);
                }
            }
            
            await DamageCmd.Attack(DynamicVars.CalculatedDamage)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target!)
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["Multiple"].UpgradeValueBy(1);
        }
    }
}