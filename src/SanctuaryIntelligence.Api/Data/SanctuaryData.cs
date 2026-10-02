using SanctuaryIntelligence.Api.Models;

namespace SanctuaryIntelligence.Api.Data;

public static class SanctuaryData
{
    private static readonly Knight[] Knights =
    [
        // Bronze Knights
        new Knight(
            Name: "Seiya",
            Rank: "Bronze",
            Constellation: "Pegasus",
            Element: "Light",
            Techniques: ["Pegasus Ryusei Ken", "Pegasus Suisei Ken", "Pegasus Rolling Crush"],
            Armor: "Pegasus Bronze Cloth",
            CosmosLevel: 72,
            ZodiacHouse: "",
            Description: "The Pegasus Saint, known for his indomitable spirit and ability to surpass his limits in every battle. His cosmos burns brightest when protecting Athena."
        ),
        new Knight(
            Name: "Shiryu",
            Rank: "Bronze",
            Constellation: "Dragon",
            Element: "Water",
            Techniques: ["Rozan Shoryuha", "Rozan Hyakuryuha", "Excalibur"],
            Armor: "Dragon Bronze Cloth",
            CosmosLevel: 74,
            ZodiacHouse: "",
            Description: "The Dragon Saint, trained at the Five Old Peaks in China. His shield is the strongest among all cloths, and his discipline is unmatched."
        ),
        new Knight(
            Name: "Hyoga",
            Rank: "Bronze",
            Constellation: "Cygnus",
            Element: "Ice",
            Techniques: ["Diamond Dust", "Aurora Thunder Attack", "Aurora Execution"],
            Armor: "Cygnus Bronze Cloth",
            CosmosLevel: 71,
            ZodiacHouse: "",
            Description: "The Cygnus Saint, master of ice techniques learned in Siberia under the Crystal Saint and Aquarius Camus. His cold cosmos can freeze anything."
        ),
        new Knight(
            Name: "Shun",
            Rank: "Bronze",
            Constellation: "Andromeda",
            Element: "Psychic",
            Techniques: ["Nebula Chain", "Nebula Stream", "Nebula Storm"],
            Armor: "Andromeda Bronze Cloth",
            CosmosLevel: 70,
            ZodiacHouse: "",
            Description: "The Andromeda Saint, gentle in nature but devastating in power. His chains have a will of their own and can attack and defend simultaneously."
        ),
        new Knight(
            Name: "Ikki",
            Rank: "Bronze",
            Constellation: "Phoenix",
            Element: "Fire",
            Techniques: ["Hoyoku Tensho", "Genma Ken", "Phoenix Genma Ken"],
            Armor: "Phoenix Bronze Cloth",
            CosmosLevel: 78,
            ZodiacHouse: "",
            Description: "The Phoenix Saint, the strongest Bronze Saint. His cloth regenerates from destruction and he is reborn stronger after every defeat, like the mythical phoenix."
        ),

        // Gold Knights
        new Knight(
            Name: "Mu",
            Rank: "Gold",
            Constellation: "Aries",
            Element: "Psychic",
            Techniques: ["Crystal Wall", "Starlight Extinction", "Stardust Revolution"],
            Armor: "Aries Gold Cloth",
            CosmosLevel: 92,
            ZodiacHouse: "Aries",
            Description: "Guardian of the First House. A Lemurian descendant with the rare ability to repair cloths. His telekinetic powers and Crystal Wall are nearly impenetrable."
        ),
        new Knight(
            Name: "Aiolia",
            Rank: "Gold",
            Constellation: "Leo",
            Element: "Lightning",
            Techniques: ["Lightning Bolt", "Lightning Plasma", "Photon Burst"],
            Armor: "Leo Gold Cloth",
            CosmosLevel: 94,
            ZodiacHouse: "Leo",
            Description: "Guardian of the Fifth House. Brother of Sagittarius Aiolos, he fights with the speed of light and devastating lightning attacks. His sense of justice is absolute."
        ),
        new Knight(
            Name: "Shaka",
            Rank: "Gold",
            Constellation: "Virgo",
            Element: "Light",
            Techniques: ["Tenbu Horin", "Tenma Kofuku", "Rikudo Rinne"],
            Armor: "Virgo Gold Cloth",
            CosmosLevel: 99,
            ZodiacHouse: "Virgo",
            Description: "Guardian of the Sixth House, known as 'The Man Closest to God'. His cosmos rivals the gods themselves. He can remove all five senses from his opponents."
        ),
        new Knight(
            Name: "Milo",
            Rank: "Gold",
            Constellation: "Scorpio",
            Element: "Fire",
            Techniques: ["Scarlet Needle", "Antares", "Restriction"],
            Armor: "Scorpio Gold Cloth",
            CosmosLevel: 91,
            ZodiacHouse: "Scorpio",
            Description: "Guardian of the Eighth House. His Scarlet Needle strikes 15 points on the body, and the final strike, Antares, is lethal. Fierce but honorable in combat."
        ),
        new Knight(
            Name: "Saga",
            Rank: "Gold",
            Constellation: "Gemini",
            Element: "Darkness",
            Techniques: ["Galaxian Explosion", "Another Dimension", "Genrou Maouken"],
            Armor: "Gemini Gold Cloth",
            CosmosLevel: 97,
            ZodiacHouse: "Gemini",
            Description: "Guardian of the Third House. Possesses a dual personality — one noble, one evil. His Galaxian Explosion rivals the power of a supernova. Once posed as the Pope."
        )
    ];

    public static IReadOnlyList<Knight> GetKnights() => Knights;

    public static Knight? GetKnightByName(string name) =>
        Knights.FirstOrDefault(k => k.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
