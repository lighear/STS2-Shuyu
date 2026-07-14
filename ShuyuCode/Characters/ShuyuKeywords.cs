using MegaCrit.Sts2.Core.Entities.Cards;
using Shuyu;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace Shuyu.Characters;

[RegisterOwnedCardKeyword(nameof(Frostforged))]
[RegisterOwnedCardKeyword(nameof(Targeted))]
public class ShuyuKeywords
{
    public static readonly CardKeyword Frostforged = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Frostforged)).GetModCardKeyword();
    public static readonly CardKeyword Targeted = ModContentRegistry.GetQualifiedKeywordId(Entry.ModId, nameof(Targeted)).GetModCardKeyword();
}