using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;
using AndroidX.Work;
using Plugin.MauiMTAdmob;

namespace PasswordSave
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // ✅ MAUI 10 — habilitar Edge-to-Edge explícitamente
            // 👇 nuevo — inicializar MTAdmob antes de todo lo demás
            CrossMauiMTAdmob.Current.Init(
                this,
                "ca-app-pub-8239660797465347~5623512633", // ver punto 4
                license: null,
#if DEBUG
                debugMode: true
#else
                debugMode: false
#endif
            );

        }
    }
}
