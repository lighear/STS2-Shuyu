using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using Shuyu.Characters;
using Shuyu.Vfx;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System.Runtime.CompilerServices;

namespace Shuyu.Cards
{
    [RegisterCard(typeof(ShuyuCardPool))]
    public class JinShuNiShang : ModCardTemplate
    {
        // CombatHistory 没有“战斗初始生命”条目，也不会完整记录治疗、直接设置生命等变化，
        // 因此不能用“当前生命 + 受到的伤害”可靠反推。这里为每场战斗保存一次玩家生命快照。
        // ConditionalWeakTable 以战斗状态为弱引用键，旧战斗不再被引用后，其快照可随之回收。
        private static readonly ConditionalWeakTable<ICombatState, Dictionary<ulong, int>>
            HpAtCombatStartByCombat = new();

        public JinShuNiShang() : base(
            baseCost: 3,
            CardType.Skill,
            CardRarity.Rare,
            TargetType.Self)
        { }

        public override CardAssetProfile AssetProfile => new(PortraitPath: $"{Entry.ResPath}/images/cards/{GetType().Name}.png");

        protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
            HoverTipFactory.FromCard<EYun>()
        ];

        public override IEnumerable<CardKeyword> CanonicalKeywords => [
            CardKeyword.Ethereal
        ];

        protected override HashSet<CardTag> CanonicalTags => [
            ShuyuCardTags.Taboo
        ];

        internal static void InitializeCombatStartHpTracking()
        {
            // CombatSetUp 发生在 BeforeCombatStart 效果之前，并且不依赖这张卡当时是否存在。
            // 先移除再订阅，避免 Mod 重载或重复初始化时同一个处理器被注册多次。
            CombatManager.Instance.CombatSetUp -= RecordHpAtCombatStart;
            CombatManager.Instance.CombatSetUp += RecordHpAtCombatStart;
        }

        private static void RecordHpAtCombatStart(CombatState combatState)
        {
            // 以 NetId 区分多人游戏中的玩家，使战斗中生成、复制或转化出的卡牌实例
            // 都能读取该玩家在本场战斗开始时的同一份生命记录。
            Dictionary<ulong, int> hpByPlayer = combatState.Players.ToDictionary(
                player => player.NetId,
                player => player.Creature.CurrentHp);

            HpAtCombatStartByCombat.Remove(combatState);
            HpAtCombatStartByCombat.Add(combatState, hpByPlayer);
        }

        private int GetHpToRestore()
        {
            if (CombatState == null
                || !HpAtCombatStartByCombat.TryGetValue(CombatState, out Dictionary<ulong, int>? hpByPlayer)
                || !hpByPlayer.TryGetValue(Owner.NetId, out int hpAtCombatStart))
            {
                return 0;
            }

            // 当前最大生命可能在战斗中降低；按 MaxHp 封顶，保证显示的恢复量与 Heal 的实际结果一致。
            // Math.Max 同时保证当前生命高于战斗初始生命时不会受到影响。
            int targetHp = Math.Min(hpAtCombatStart, Owner.Creature.MaxHp);
            return Math.Max(0, targetHp - Owner.Creature.CurrentHp);
        }

        protected override void AddExtraArgsToDescription(LocString description)
        {
            description.Add("HealAmount", GetHpToRestore());
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            NJinShuNiShangEdgeVfx.Play();
            await NJinShuNiShangFogRingVfx.PlayOpening(Owner.Creature);

            await CreatureCmd.TriggerAnim(
                Owner.Creature,
                "Cast",
                Owner.Character.CastAnimDelay);

            int hpToRestore = GetHpToRestore();
            if (hpToRestore > 0)
            {
                await CreatureCmd.Heal(Owner.Creature, hpToRestore);
            }

            await EYun.CreateInDrawPile(Owner, 2, CombatState!);
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
        }
    }
}
