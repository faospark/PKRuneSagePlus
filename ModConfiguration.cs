using BepInEx.Configuration;

namespace PKRuneSagePlus;

public sealed class ModConfiguration
{
    private readonly ConfigFile _config;

    // Suikoden 1 Castle Rune Master (Jeane Shop Expansion) Settings
    public ConfigEntry<bool> EnableS1JeaneRuneShop { get; private set; }

    // Suikoden 2 Sajah Village Rare Shop Early Unlock Settings
    public ConfigEntry<bool> UnlockSajahRareShopEarly { get; private set; }

    public ModConfiguration(ConfigFile config)
    {
        _config = config;
    }

    public void Init()
    {
        EnableS1JeaneRuneShop = _config.Bind(
            "Suikoden 1 Rune Sage",
            "EnableS1JeaneRuneShop",
            true,
            "Expands Jeane's Castle Rune Master menu with a 'Buy' option (Attach, Remove, Buy, Exit). She acts as the Chandler of Runes, progressively selling Rune Crystals from town rune shops visited across the world."
        );

        UnlockSajahRareShopEarly = _config.Bind(
            "Suikoden 2 Rune Sage",
            "UnlockSajahRareShopEarly",
            true,
            "Unlocks the hidden rare item vendor and counter trigger inside the Sajah Village item shop before defeating the Beast Rune, allowing late-game access to rare Magic Runes (Rage, Thunder, Flowing, Mother Earth, Cyclone)."
        );
    }
}

