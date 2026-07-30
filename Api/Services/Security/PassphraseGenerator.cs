using System.Security.Cryptography;

namespace HrManagementSystem.Infrastructure.Security;

public static class PassphraseGenerator
{
    private static readonly string[] Words =
    [
        "River", "Coffee", "Moon", "Train", "Bamboo", "Tiger", "Rain", "Quiet",
        "Horse", "Purple", "Window", "Cedar", "Ocean", "Lantern", "Maple", "Stone",
        "Cloud", "Garden", "Silver", "Falcon", "Amber", "Bridge", "Willow", "Summit",
        "Coral", "Meadow", "Pine", "Harbor", "Nova", "Orchid", "Pepper", "Canyon"
    ];

    /// <summary>Builds a memorable passphrase such as River-Coffee-Moon-Train-84.</summary>
    public static string Generate()
    {
        var picks = new string[4];
        for (var i = 0; i < picks.Length; i++)
        {
            picks[i] = Words[RandomNumberGenerator.GetInt32(Words.Length)];
        }

        var number = RandomNumberGenerator.GetInt32(10, 100);
        return $"{string.Join('-', picks)}-{number}";
    }
}
