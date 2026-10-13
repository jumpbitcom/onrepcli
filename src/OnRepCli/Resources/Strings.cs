using System.Globalization;
using System.Resources;

namespace OnRepCli.Resources;

public static class Strings
{
    private static readonly ResourceManager ResourceManager = new(
        "OnRepCli.Resources.Strings",
        typeof(Strings).Assembly);

    public static string AppTitle => Get(nameof(AppTitle));
    public static string BrandTitle => Get(nameof(BrandTitle));
    public static string BrandSubtitle => Get(nameof(BrandSubtitle));
    public static string Refresh => Get(nameof(Refresh));
    public static string Rubrics => Get(nameof(Rubrics));
    public static string IndependentNews => Get(nameof(IndependentNews));
    public static string Latest => Get(nameof(Latest));
    public static string LatestDescription => Get(nameof(LatestDescription));
    public static string AllNews => Get(nameof(AllNews));
    public static string News => Get(nameof(News));
    public static string WelcomeStatus => Get(nameof(WelcomeStatus));
    public static string RefreshAccessibleName => Get(nameof(RefreshAccessibleName));
    public static string SectionsAccessibleName => Get(nameof(SectionsAccessibleName));

    public static string LoadingStatus(string sectionName) =>
        string.Format(CultureInfo.CurrentCulture, Get(nameof(LoadingStatus)), sectionName);

    public static string OfflineStatus(string updateTime) =>
        string.Format(CultureInfo.CurrentCulture, Get(nameof(OfflineStatus)), updateTime);

    public static string UpdatedStatus(string updateTime) =>
        string.Format(CultureInfo.CurrentCulture, Get(nameof(UpdatedStatus)), updateTime);

    public static string ErrorStatus => Get(nameof(ErrorStatus));

    private static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
