#if ANDROID
using Android.App;
using Android.Content;
using Android.App.Job;
#elif IOS || MACCATALYST
using UserNotifications;
#endif

namespace aplicacionvandex;

// Traduce un aviso del radar a una notificación del teléfono cuando la plataforma lo permite.
public static class NotificacionesVantex
{
    // Pide el permiso que corresponde al sistema. Windows usa el diálogo de prueba de la pantalla.
    public static async Task<bool> PedirPermiso()
    {
#if ANDROID
        return !OperatingSystem.IsAndroidVersionAtLeast(33)
            || await Permissions.RequestAsync<PermisoAvisos>() == PermissionStatus.Granted;
#elif IOS || MACCATALYST
        var resultado = new TaskCompletionSource<bool>();
        UNUserNotificationCenter.Current.RequestAuthorization(
            UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound,
            (permitido, _) => resultado.TrySetResult(permitido));
        return await resultado.Task;
#else
        return await Task.FromResult(false);
#endif
    }

    // Envía el título y el detalle al sistema de notificaciones. Devuelve si pudo solicitar la publicación.
    public static bool Mostrar(string titulo, string texto)
    {
#if ANDROID
        var contexto = Android.App.Application.Context;
        var administrador = contexto.GetSystemService(Context.NotificationService) as NotificationManager;
        if (administrador is null
            || OperatingSystem.IsAndroidVersionAtLeast(24) && !administrador.AreNotificationsEnabled())
            return false;

        const string canal = "vantex-radar";
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
            administrador.CreateNotificationChannel(new NotificationChannel(canal, "Alertas de Vantex", NotificationImportance.Default));
        // Al tocar un aviso en Android, el sistema abre la actividad principal de Vantex.
        var inicio = new Intent(contexto, typeof(MainActivity));
        inicio.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);
        var opciones = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23)) opciones |= PendingIntentFlags.Immutable;
        var abrir = PendingIntent.GetActivity(contexto, 10, inicio, opciones);
        var mensaje = OperatingSystem.IsAndroidVersionAtLeast(26)
            ? new Notification.Builder(contexto, canal)
            : new Notification.Builder(contexto);
        mensaje.SetContentTitle(titulo)
            .SetContentText(texto)
            .SetSmallIcon(Resource.Drawable.ic_vantex_alerta)
            .SetAutoCancel(true)
            .SetContentIntent(abrir);
        // Usa el reloj para que avisos distintos se puedan leer en la bandeja.
        int id = Interlocked.Increment(ref siguienteAviso);
        administrador.Notify(id, mensaje.Build());
        return true;
#elif IOS || MACCATALYST
        var contenido = new UNMutableNotificationContent { Title = titulo, Body = texto, Sound = UNNotificationSound.Default };
        var solicitud = UNNotificationRequest.FromIdentifier(Guid.NewGuid().ToString(), contenido, null);
        UNUserNotificationCenter.Current.AddNotificationRequest(solicitud, _ => { });
        return true;
#else
        return false;
#endif
    }

    // Activa o cancela el trabajo periódico que Android ejecuta aun cuando la app no está visible.
    public static bool Programar(bool activar)
    {
#if ANDROID
        var contexto = Android.App.Application.Context;
        var programador = contexto.GetSystemService(Context.JobSchedulerService) as JobScheduler;
        if (programador is null) return false;

        const int id = 2711;
        if (!activar)
        {
            programador.Cancel(id);
            return true;
        }

        // Android decide el instante exacto. Se conserva la tarea al reiniciar el dispositivo.
        var componente = new ComponentName(contexto, Java.Lang.Class.FromType(typeof(RadarJobService)));
        using var constructor = new JobInfo.Builder(id, componente);
        constructor.SetRequiredNetworkType(NetworkType.Any);
        constructor.SetPersisted(true);
        constructor.SetPeriodic(15 * 60 * 1000L);
        var trabajo = constructor.Build();
        return trabajo is not null && programador.Schedule(trabajo) == JobScheduler.ResultSuccess;
#else
        return false;
#endif
    }

#if ANDROID
    private static int siguienteAviso = (int)(DateTime.UtcNow.Ticks % int.MaxValue);

    public sealed class PermisoAvisos : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions
            => OperatingSystem.IsAndroidVersionAtLeast(33)
                ? [(Android.Manifest.Permission.PostNotifications, true)] : [];
    }
#endif
}
