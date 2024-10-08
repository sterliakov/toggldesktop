﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using TogglDesktop.Diagnostics;
using TogglDesktop.Services;

// ReSharper disable InconsistentNaming

namespace TogglDesktop
{
public static partial class Toggl
{
    #region Data types

    public class TimelineEventView
    {
        public string Title;
        public string Filename;
        public Int64 Duration;
        public string DurationString;
        public bool Header;
        // references subevents
        public List<TimelineEventView> SubEvents;

        public TimelineEventView(TogglTimelineEventView eventView)
        {
            Title = eventView.Title;
            Filename = eventView.Filename;
            Duration = eventView.Duration;
            DurationString = eventView.DurationString;
            Header = eventView.Header;
            SubEvents = marshalList<TimelineEventView, TogglTimelineEventView>(eventView.Event, n => n.Next, t => new TimelineEventView(t));
        }
    }

    public class TimelineChunkView
    {
        public UInt64 Started;
        public UInt64 Ended;
        public string StartTimeString;
        public string EndTimeString;
        public List<TimelineEventView> Events;

        public TimelineChunkView(TogglTimelineChunkView chunk)
        {
            Started = chunk.Started;
            Ended = chunk.Ended;
            StartTimeString = chunk.StartTimeString;
            EndTimeString = chunk.EndTimeString;
            Events = marshalList<TimelineEventView, TogglTimelineEventView>(chunk.FirstEvent, n => n.Next, t => new TimelineEventView(t));
        }

        public override string ToString()
        {
            return EndTimeString;
        }
    }

    #endregion

    #region constants and static fields

    public const string Project = "project";
    public const string Duration = "duration";
    public const string Description = "description";

    public const string TagSeparator = "\t";

    private static readonly DateTimeOffset UnixEpoch =
        new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static IntPtr ctx = IntPtr.Zero;

    private static MainWindow mainWindow;

    // User can override some parameters when running the app
    public static string ScriptPath;
    public static string DatabasePath;
    public static string LogPath;

    public static readonly string WritableAppDirPath =
        Path.Combine(
            Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "TogglDesktop");

    public static readonly string UpdatesPath = Path.Combine(WritableAppDirPath, "updates");

    static Toggl()
    {
        UpdateService = new UpdateService(Toggl.IsUpdateCheckDisabled(), Toggl.UpdatesPath);
    }

    public static UpdateService UpdateService { get; }

#if TOGGL_PRODUCTION_BUILD
    public static string Env = "production";
#else
    public static string Env = "development";
#endif

    #endregion

    #region enums

    public enum OnlineState
    {
        Online = kOnlineStateOnline,
        NoNetwork = kOnlineStateNoNetwork,
        BackendDown = kOnlineStateBackendDown
    }

    public enum SyncState
    {
        Idle = kSyncStateIdle,
        Syncing = kSyncStateWork
    }

    public enum DownloadStatus
    {
        Started = kDownloadStatusStarted,
        Done = kDownloadStatusDone
    }

    #endregion

    #region callback delegates

    public delegate void DisplayApp(
        bool open);

    public delegate void DisplayOverlay(
        Int64 type);

    public delegate void DisplayError(
        string errmsg,
        bool user_error);

    public delegate void DisplaySyncState(
        SyncState state);

    public delegate void DisplayUnsyncedItems(
        Int64 count);

    public delegate void DisplayOnlineState(
        OnlineState state);

    public delegate void DisplayURL(
        string url);

    public delegate void DisplayLogin(
        bool open,
        UInt64 user_id);

    public delegate void DisplayLoginSSO(
        string sso_url);

    public delegate void DisplayReminder(
        string title,
        string informative_text);

    public delegate void DisplayTimeEntryList(
        bool open,
        List<TogglTimeEntryView> list,
        bool show_load_more_button);

    public delegate void DisplayAutocomplete(
        List<TogglAutocompleteView> list);

    public delegate void DisplayViewItems(
        List<TogglGenericView> list);

    public delegate void DisplayTimeEntryEditor(
        bool open,
        TogglTimeEntryView te,
        string focused_field_name);

    public delegate void DisplaySettings(
        bool open,
        TogglSettingsView settings);

    public delegate void DisplayRunningTimerState(
        TogglTimeEntryView te);

    public delegate void DisplayStoppedTimerState();

    public delegate void DisplayIdleNotification(
        string guid,
        string since,
        string duration,
        Int64 started,
        string description);

    public delegate void DisplayAutotrackerRules(
        List<TogglAutotrackerRuleView> rules, string[] terms);

    public delegate void DisplayAutotrackerNotification(
        string projectName, ulong projectId, ulong taskId);

    public delegate void DisplayProjectColors(
        string[] colors, ulong count);

    public delegate void DisplayCountries(
        List<TogglCountryView> list);

    public delegate void DisplayPromotion(
        long id);

    public delegate void DisplayPomodoro(
        string title, string informativeText);

    public delegate void DisplayPomodoroBreak(
        string title, string informativeText);

    public delegate void DisplayInAppNotification(
        string title, string text, string button, string url);

    public delegate void DisplayTimeline(
        bool open, string date,
        List<TimelineChunkView> first,
        List<TogglTimeEntryView> firstTimeEntry,
        ulong startDay,
        ulong endDay);

    public delegate void DisplayTimelineUI(
        bool isEnabled);

    #endregion

    #region api calls

    public static void LoadMore()
    {
        toggl_load_more(ctx);
    }

    public static void Clear()
    {
        toggl_context_clear(ctx);
    }

    public static void ShowApp()
    {
        toggl_show_app(ctx);
    }

    public static void Debug(string text)
    {
        toggl_debug(text);
    }
    #region Debug overloads
    public static void Debug(string text, object arg0)
    {
        toggl_debug(string.Format(text, arg0));
    }
    public static void Debug(string text, object arg0, object arg1)
    {
        toggl_debug(string.Format(text, arg0, arg1));
    }
    public static void Debug(string text, object arg0, object arg1, object arg2)
    {
        toggl_debug(string.Format(text, arg0, arg1, arg2));
    }
    public static void Debug(string text, params object[] args)
    {
        toggl_debug(string.Format(text, args));
    }
    #endregion

    public static bool Signup(string email, string password, long country_id)
    {
        return toggl_signup(ctx, email, password, Convert.ToUInt64(country_id));
    }

    public static bool Login(string email, string password, long country_id)
    {
        return toggl_login(ctx, email, password);
    }

    public static bool GetIdentityProviderSSO(string email)
    {
        return toggl_get_identity_provider_sso(ctx, email);
    }

    public static void LoginSSO(string api_token)
    {
        toggl_login_sso(ctx, api_token);
    }

    public static void SetNeedEnableSSO(string code)
    {
        toggl_set_need_enable_SSO(ctx, code);
    }

    public static void ResetEnableSSO()
    {
        toggl_reset_enable_SSO(ctx);
    }
    
    public static bool GoogleSignup(string access_token, long country_id)
    {
        return toggl_google_signup(ctx, access_token, Convert.ToUInt64(country_id));
    }

    public static bool GoogleLogin(string access_token)
    {
        return toggl_google_login(ctx, access_token);
    }

    public static void PasswordForgot()
    {
        toggl_password_forgot(ctx);
    }

    public static void OpenInBrowser()
    {
        toggl_open_in_browser(ctx);
    }

    public static void AcceptToS()
    {
        toggl_accept_tos(ctx);
    }

    public static void OpenToS()
    {
        toggl_tos(ctx);
    }

    public static void OpenPrivacy()
    {
        toggl_privacy_policy(ctx);
    }

    public static bool SendFeedback(
        string topic,
        string details,
        string filename)
    {
        return toggl_feedback_send(ctx, topic, details, filename);
    }

    public static void ViewTimeEntryList()
    {
        toggl_view_time_entry_list(ctx);
    }

    public static void Edit(
        string guid,
        bool edit_running_time_entry,
        string focused_field_name)
    {
        toggl_edit(ctx, guid, edit_running_time_entry, focused_field_name);
    }

    public static void EditPreferences()
    {
        toggl_edit_preferences(ctx);
    }

    public static bool Continue(string guid)
    {
        OnUserTimeEntryStart();
        return toggl_continue(ctx, guid);
    }

    public static bool ContinueLatest(bool preventOnApp = false)
    {
        OnUserTimeEntryStart();
        return toggl_continue_latest(ctx, preventOnApp);
    }

    public static bool DeleteTimeEntry(string guid)
    {
        return toggl_delete_time_entry(ctx, guid);
    }

    #region changing time entry

    public static bool SetTimeEntryDuration(string guid, string value)
    {
        using (Performance.Measure("changing time entry duration"))
        {
            return toggl_set_time_entry_duration(ctx, guid, value);
        }
    }

    public static bool SetTimeEntryProject(
        string guid,
        UInt64 task_id,
        UInt64 project_id,
        string project_guid)
    {
        using (Performance.Measure("changing time entry project"))
        {
            return toggl_set_time_entry_project(ctx,
                                                guid, task_id, project_id, project_guid);
        }
    }

    public static bool SetTimeEntryStart(string guid, string value)
    {
        using (Performance.Measure("changing time entry start time"))
        {
            return toggl_set_time_entry_start(ctx, guid, value);
        }
    }

    public static bool SetTimeEntryDate(string guid, DateTime value)
    {
        using (Performance.Measure("changing time entry date"))
        {
            return toggl_set_time_entry_date(ctx, guid, UnixFromDateTime(value));
        }
    }

    public static bool SetTimeEntryEnd(string guid, string value)
    {
        using (Performance.Measure("changing time entry end time"))
        {
            return toggl_set_time_entry_end(ctx, guid, value);
        }
    }

    public static bool SetTimeEntryTags(string guid, List<string> tags)
    {
        using (Performance.Measure("changing time entry tags, count: {0}", tags.Count))
        {
            string value = String.Join(TagSeparator, tags);
            return toggl_set_time_entry_tags(ctx, guid, value);
        }
    }

    public static bool SetTimeEntryBillable(string guid, bool billable)
    {
        using (Performance.Measure("changing time entry billable"))
        {
            return toggl_set_time_entry_billable(ctx, guid, billable);
        }
    }

    public static bool SetTimeEntryDescription(string guid, string value)
    {
        using (Performance.Measure("changing time entry description"))
        {
            return toggl_set_time_entry_description(ctx, guid, value);
        }
    }

    public static bool SetTimeEntryStartTimeStamp(string guid, long timeStamp)
    {
        using (Performance.Measure("changing time entry start time stamp"))
        {
            return toggl_set_time_entry_start_timestamp(ctx, guid, timeStamp);
        }
    }

    public static bool SetTimeEntryStartTimeStampWithOption(string guid, long timeStamp, bool keepEndTimeFixed)
    {
        using (Performance.Measure("changing time entry start time stamp"))
        {
            return toggl_set_time_entry_start_timestamp_with_option(ctx, guid, timeStamp, keepEndTimeFixed);
        }
    }

    public static bool SetTimeEntryEndTimeStamp(string guid, long timeStamp)
    {
        using (Performance.Measure("changing time entry end time stamp"))
        {
            return toggl_set_time_entry_end_timestamp(ctx, guid, timeStamp);
        }
    }

    #endregion

    public static bool Stop(bool preventOnApp = false)
    {
        return toggl_stop(ctx, preventOnApp);
    }

    public static bool DiscardTimeAt(string guid, Int64 at, bool split)
    {
        return toggl_discard_time_at(ctx, guid, at, split);
    }

    public static bool DiscardTimeAndContinue(string guid, Int64 at, bool split)
    {
        return toggl_discard_time_and_continue(ctx, guid, at);
    }

    public static bool SetSettings(TogglSettingsView settings)
    {
        if (!toggl_set_settings_use_idle_detection(ctx,
                settings.UseIdleDetection)) {
            return false;
        }

        if (!toggl_set_settings_on_top(ctx,
                                       settings.OnTop)) {
            return false;
        }

        if (!toggl_set_settings_reminder(ctx,
                                         settings.Reminder)) {
            return false;
        }

        if (!toggl_set_settings_idle_minutes(ctx,
                                             (ulong)settings.IdleMinutes)) {
            return false;
        }

        if (!toggl_set_settings_reminder_minutes(ctx,
                (ulong)settings.ReminderMinutes)) {
            return false;
        }

        if (!toggl_set_settings_focus_on_shortcut(ctx,
                settings.FocusOnShortcut)) {
            return false;
        }

        if (!toggl_set_settings_autodetect_proxy(ctx,
                settings.AutodetectProxy))
        {
            return false;
        }

        if (!toggl_set_proxy_settings(
            ctx,
            settings.UseProxy,
            settings.ProxyHost,
            settings.ProxyPort,
            settings.ProxyUsername,
            settings.ProxyPassword))
        {
            return false;
        }
        if (!toggl_set_settings_remind_days(
            ctx,
            settings.RemindMon,
            settings.RemindTue,
            settings.RemindWed,
            settings.RemindThu,
            settings.RemindFri,
            settings.RemindSat,
            settings.RemindSun
                ))
        {
            return false;
        }
        if (!toggl_set_settings_remind_times(
            ctx,
            settings.RemindStarts,
            settings.RemindEnds
                ))
        {
            return false;
        }

        if (!toggl_set_settings_autotrack(ctx, settings.Autotrack))
        {
            return false;
        }

        if (!toggl_set_settings_pomodoro(ctx, settings.Pomodoro))
        {
            return false;
        }

        if (!toggl_set_settings_pomodoro_minutes(ctx, (ulong)settings.PomodoroMinutes))
        {
            return false;
        }

        if (!toggl_set_settings_pomodoro_break(ctx, settings.PomodoroBreak))
        {
            return false;
        }

        if (!toggl_set_settings_pomodoro_break_minutes(ctx, (ulong)settings.PomodoroBreakMinutes))
        {
            return false;
        }

        if (!toggl_set_settings_stop_entry_on_shutdown_sleep(ctx, settings.StopEntryOnShutdownSleep))
        {
            return false;
        }

        if (!toggl_set_settings_color_theme(ctx, settings.ColorTheme))
        {
            return false;
        }

        if (!toggl_set_settings_ignore_cert(ctx, settings.ForceIgnoreCert))
        {
            return false;
        }

        if (!toggl_set_settings_start_autotracker_without_suggestions(ctx, settings.StartAutotrackerWithoutSuggestions))
        {
            return false;
        }

        if (!toggl_set_settings_start_autotracker_while_timer_is_running(ctx, settings.StartAutotrackerWhileTimerIsRunning))
        {
            return false;
        }

        return toggl_timeline_toggle_recording(ctx, settings.RecordTimeline);
    }

    public static bool IsTimelineRecordingEnabled()
    {
        return toggl_timeline_is_recording_enabled(ctx);
    }

    public static string GetTimeOfDayFormat() => toggl_time_of_day_format(ctx);

    public static bool Logout()
    {
        return toggl_logout(ctx);
    }

    public static bool ClearCache()
    {
        return toggl_clear_cache(ctx);
    }

    public static bool SetLoggedInUser(string json) {
        return testing_set_logged_in_user(ctx, json);
    }

    public static string Start(
        string description,
        string duration,
        UInt64 task_id,
        UInt64 project_id,
        string project_guid,
        string tags,
        bool preventOnApp = false)
    {
        OnUserTimeEntryStart();

        return toggl_start(ctx,
                           description,
                           duration,
                           task_id,
                           project_id,
                           project_guid,
                           tags,
                           preventOnApp,
                           0,
                           0);
    }

    public static string AddProject(
        string time_entry_guid,
        UInt64 workspace_id,
        UInt64 client_id,
        string client_guid,
        string project_name,
        bool is_private,
        string color)
    {
        using (Performance.Measure("adding project"))
        {
            return toggl_add_project(ctx,
                                     time_entry_guid,
                                     workspace_id,
                                     client_id,
                                     client_guid,
                                     project_name,
                                     is_private,
                                     color);
        }
    }

    public static string CreateClient(
        UInt64 workspace_id,
        string client_name)
    {
        using (Performance.Measure("adding client"))
        {
            return toggl_create_client(ctx,
                                       workspace_id,
                                       client_name);
        }
    }

    public static bool SetUpdateChannel(string channel)
    {
        return toggl_set_update_channel(ctx, channel);
    }

    public static string UpdateChannel()
    {
        return toggl_get_update_channel(ctx);
    }

    public static string UserEmail()
    {
        return toggl_get_user_email(ctx);
    }

    public static DayOfWeek UserBeginningOfWeek()
    {
        return (DayOfWeek) toggl_get_user_beginning_of_week(ctx);
    }

    public static void FullSync()
    {
        toggl_fullsync(ctx);
    }

    public static void Sync()
    {
        OnManualSync();
        toggl_fullsync(ctx);
    }

    public static void SetSleep()
    {
        toggl_set_sleep(ctx);
    }

    public static void SetLocked()
    {
        toggl_set_locked(ctx);
    }

    public static void SetUnlocked()
    {
        toggl_set_unlocked(ctx);
    }

    public static void SetOSShutdown()
    {
        toggl_os_shutdown(ctx);
    }

    public static void TrackWindowSize(Size size)
    {
        track_window_size(ctx, (ulong)size.Width, (ulong)size.Height);
    }

    public static void SetWake()
    {
        toggl_set_wake(ctx);
    }

    public static void SetIdleSeconds(UInt64 idle_seconds)
    {
        toggl_set_idle_seconds(ctx, idle_seconds);
    }

    public static string FormatDurationInSecondsHHMMSS(Int64 duration_in_seconds)
    {
        return toggl_format_tracking_time_duration(duration_in_seconds);
    }

    public static long AddAutotrackerRule(string terms, ulong projectId, ulong taskId, string startTime = "", string endTime = "", byte daysOfWeek = 0)
    {
        return toggl_autotracker_add_rule(ctx, terms, projectId, taskId, startTime, endTime, daysOfWeek);
    }

    public static bool UpdateAutotrackerRule(long ruleId, string terms, ulong projectId, ulong taskId, string startTime = "", string endTime = "", byte daysOfWeek = 0)
    {
        return toggl_autotracker_update_rule(ctx, ruleId, terms, projectId, taskId, startTime, endTime, daysOfWeek);
    }

    public static bool DeleteAutotrackerRule(long id)
    {
        return toggl_autotracker_delete_rule(ctx, id);
    }

    public static bool SetDefaultProject(ulong projectId, ulong taskId)
    {
        return toggl_set_default_project(ctx, projectId, taskId);
    }

    public static string GetDefaultProjectName()
    {
        return toggl_get_default_project_name(ctx);
    }

    public static ulong GetDefaultProjectId()
    {
        return toggl_get_default_project_id(ctx);
    }

    public static ulong GetDefaultTaskId()
    {
        return toggl_get_default_task_id(ctx);
    }

    public static void GetProjectColors()
    {
        toggl_get_project_colors(ctx);
    }

    public static void GetCountries()
    {
        toggl_get_countries(ctx);
    }

    public static void SetKeepEndTimeFixed(bool b)
    {
        toggl_set_keep_end_time_fixed(ctx, b);
    }

    public static bool GetKeepEndTimeFixed()
    {
        return toggl_get_keep_end_time_fixed(ctx);
    }

    public static void SetMiniTimerX(long x)
    {
        toggl_set_mini_timer_x(ctx, x);
    }

    public static long GetMiniTimerX()
    {
        return toggl_get_mini_timer_x(ctx);
    }

    public static void SetMiniTimerY(long y)
    {
        toggl_set_mini_timer_y(ctx, y);
    }

    public static long GetMiniTimerY()
    {
        return toggl_get_mini_timer_y(ctx);
    }

    public static void SetMiniTimerW(long w)
    {
        toggl_set_mini_timer_w(ctx, w);
    }

    public static long GetMiniTimerW()
    {
        return toggl_get_mini_timer_w(ctx);
    }

    public static void SetMiniTimerVisible(bool visible)
    {
        toggl_set_mini_timer_visible(ctx, visible);
    }

    public static bool GetMiniTimerVisible()
    {
        return toggl_get_mini_timer_visible(ctx);
    }

    public static void TrackClickCloseButtonInAppMessage()
    {
        toggl_iam_click(ctx, 2);
    }

    public static void TrackClickActionButtonInAppMessage()
    {
        toggl_iam_click(ctx, 3);
    }

    public static void TrackCollapseDay()
    {
        track_collapse_day(ctx);
    }

    public static void TrackExpandDay()
    {
        track_expand_day(ctx);
    }

    public static void TrackCollapseAllDays()
    {
        track_collapse_all_days(ctx);
    }

    public static void TrackExpandAllDays()
    {
        track_expand_all_days(ctx);
    }

    public static TogglHsvColor GetAdaptiveHsvColor(TogglRgbColor rgbColor, TogglAdaptiveColor type)
    {
        return toggl_get_adaptive_hsv_color(rgbColor, type);
    }

    public static TogglRgbColor GetAdaptiveRgbColorFromHex(string hexColor, TogglAdaptiveColor type)
    {
        return toggl_get_adaptive_rgb_color_from_hex(hexColor, type);
    }

    #endregion

    #region callback events

    public static event DisplayApp OnApp = delegate { };
    public static event DisplayOverlay OnOverlay = delegate { };
    public static event DisplayError OnError = delegate { };
    public static event DisplayOnlineState OnOnlineState = delegate { };
    public static event DisplayLogin OnLogin = delegate { };
    public static event DisplayLoginSSO OnLoginSSO = delegate { };
    public static event DisplayReminder OnReminder = delegate { };
    public static event DisplayTimeEntryList OnTimeEntryList = delegate { };
    public static event DisplayAutocomplete OnTimeEntryAutocomplete = delegate { };
    public static event DisplayAutocomplete OnMinitimerAutocomplete = delegate { };
    public static event DisplayAutocomplete OnProjectAutocomplete = delegate { };
    public static event DisplayTimeEntryEditor OnTimeEntryEditor = delegate { };
    public static event DisplayViewItems OnWorkspaceSelect = delegate { };
    public static event DisplayViewItems OnClientSelect = delegate { };
    public static event DisplayViewItems OnTags = delegate { };
    public static event DisplaySettings OnSettings = delegate { };
    public static event DisplayURL OnURL = delegate { };
    public static event DisplayIdleNotification OnIdleNotification = delegate { };
    public static event DisplayAutotrackerRules OnAutotrackerRules = delegate { };
    public static event DisplayAutotrackerNotification OnAutotrackerNotification = delegate { };
    public static event DisplaySyncState OnDisplaySyncState = delegate { };
    public static event DisplayUnsyncedItems OnDisplayUnsyncedItems = delegate { };
    public static event DisplayProjectColors OnDisplayProjectColors = delegate { };
    public static event DisplayCountries OnDisplayCountries = delegate { };
    public static event DisplayPromotion OnDisplayPromotion = delegate { };
    public static event DisplayPomodoro OnDisplayPomodoro = delegate { };
    public static event DisplayPomodoroBreak OnDisplayPomodoroBreak = delegate { };
    public static event DisplayInAppNotification OnDisplayInAppNotification = delegate { };
    public static readonly BehaviorSubject<UpdateStatus> OnUpdateDownloadStatus
        = new BehaviorSubject<UpdateStatus>(new UpdateStatus());

    public static readonly BehaviorSubject<List<TimelineChunkView>> TimelineChunks =
        new BehaviorSubject<List<TimelineChunkView>>(new List<TimelineChunkView>());

    public static readonly BehaviorSubject<List<TogglTimeEntryView>> TimelineTimeEntries =
        new BehaviorSubject<List<TogglTimeEntryView>>(new List<TogglTimeEntryView>());
    
    public static readonly BehaviorSubject<DateTime> TimelineSelectedDate = new BehaviorSubject<DateTime>(DateTime.Today);

    public static readonly BehaviorSubject<TogglTimeEntryView?> RunningTimeEntry = new BehaviorSubject<TogglTimeEntryView?>(null);

    public static readonly IObservable<Unit> StoppedTimerState =
        RunningTimeEntry
        .Where(te => te == null)
        .Select(_ => Unit.Default);
    public static readonly IObservable<TogglTimeEntryView> RunningTimerState =
        RunningTimeEntry
            .Where(te => te != null)
            .Select(te => te.Value);
    public static event DisplayTimelineUI OnDisplayTimelineUI = delegate { };
    private static void listenToLibEvents()
    {
        toggl_on_show_app(ctx, open =>
        {
            using (Performance.Measure("Calling OnApp"))
            {
                OnApp(open);
            }
        });

        toggl_on_overlay(ctx, type =>
        {
            using (Performance.Measure("Calling OnOverlay"))
            {
                OnOverlay(type);
            }
        });

        toggl_on_error(ctx, (errmsg, user_error) =>
        {
            using (Performance.Measure("Calling OnError, user_error: {1}, message: {0}", errmsg, user_error))
            {
                OnError(errmsg, user_error);
            }
        });

        toggl_on_sync_state(ctx, state =>
        {
            using (Performance.Measure("Calling OnDisplaySyncState, state: {0}", state))
            {
                OnDisplaySyncState((SyncState)state);
            }
        });
        toggl_on_unsynced_items(ctx, count =>
        {
            using (Performance.Measure("Calling OnDisplayUnsyncedItems, count: {0}", count))
            {
                OnDisplayUnsyncedItems(count);
            }
        });

        toggl_on_online_state(ctx, state =>
        {
            using (Performance.Measure("Calling OnOnlineState, state: {0}", state))
            {
                OnOnlineState((OnlineState)state);
            }
        });

        toggl_on_login(ctx, (open, user_id) =>
        {
            using (Performance.Measure("Calling OnLogin"))
            {
                OnLogin(open, user_id);
            }
        });

        toggl_on_display_login_sso(ctx, sso_url =>
        {
            using (Performance.Measure("Calling OnLoginSSO"))
            {
                OnLoginSSO(sso_url);
            }
        });

        toggl_on_reminder(ctx, (title, informative_text) =>
        {
            using (Performance.Measure("Calling OnReminder, title: {0}", title))
            {
                OnReminder(title, informative_text);
            }
        });

        toggl_on_time_entry_list(ctx, (open, first, show_load_more_button) =>
        {
            using (Performance.Measure("Calling OnTimeEntryList, open: {0}", open))
            {
                OnTimeEntryList(open, convertToTimeEntryList(first), show_load_more_button);
            }
        });

        toggl_on_time_entry_autocomplete(ctx, first =>
        {
            using (Performance.Measure("Calling OnTimeEntryAutocomplete"))
            {
                OnTimeEntryAutocomplete(convertToAutocompleteList(first));
            }
        });

        toggl_on_mini_timer_autocomplete(ctx, first =>
        {
            using (Performance.Measure("Calling OnMinitimerAutocomplete"))
            {
                OnMinitimerAutocomplete(convertToAutocompleteList(first));
            }
        });

        toggl_on_project_autocomplete(ctx, first =>
        {
            using (Performance.Measure("Calling OnProjectAutocomplete"))
            {
                OnProjectAutocomplete(convertToAutocompleteList(first));
            }
        });

        toggl_on_time_entry_editor(ctx, (open, te, focused_field_name) =>
        {
            using (Performance.Measure("Calling OnTimeEntryEditor, focused field: {0}", focused_field_name))
            {
                OnTimeEntryEditor(open, marshalStruct<TogglTimeEntryView>(te), focused_field_name);
            }
        });

        toggl_on_workspace_select(ctx, first =>
        {
            using (Performance.Measure("Calling OnWorkspaceSelect"))
            {
                OnWorkspaceSelect(convertToViewItemList(first));
            }
        });

        toggl_on_client_select(ctx, first =>
        {
            using (Performance.Measure("Calling OnClientSelect"))
            {
                OnClientSelect(convertToViewItemList(first));
            }
        });

        toggl_on_tags(ctx, first =>
        {
            using (Performance.Measure("Calling OnTags"))
            {
                OnTags(convertToViewItemList(first));
            }
        });

        toggl_on_settings(ctx, (open, settings) =>
        {
            using (Performance.Measure("Calling OnSettings"))
            {
                OnSettings(open, marshalStruct<TogglSettingsView>(settings));
            }
        });

        toggl_on_timer_state(ctx, te =>
        {
            if (te == IntPtr.Zero)
            {
                using (Performance.Measure("Calling StoppedTimerState"))
                {
                    RunningTimeEntry.OnNext(null);
                    return;
                }
            }
            using (Performance.Measure("Calling OnRunningTimerState"))
            {
                RunningTimeEntry.OnNext(marshalStruct<TogglTimeEntryView>(te));
            }
        });

        toggl_on_url(ctx, url =>
        {
            using (Performance.Measure("Calling OnURL"))
            {
                OnURL(url);
            }
        });

        toggl_on_idle_notification(ctx, (guid, since, duration, started, description, project, task, projectColor) =>
        {
            using (Performance.Measure("Calling OnIdleNotification"))
            {
                OnIdleNotification(guid, since, duration, started, description);
            }
        });

        toggl_on_autotracker_rules(ctx, (first, count, list) =>
        {
            using (Performance.Measure("Calling OnAutotrackerRules"))
            {
                OnAutotrackerRules(convertToAutotrackerEntryList(first), list);
            }
        });

        toggl_on_autotracker_notification(ctx, (name, project_id, task_id) =>
        {
            using (Performance.Measure("Calling OnAutotrackerNotification"))
            {
                OnAutotrackerNotification(name, project_id, task_id);
            }
        });

        toggl_on_update_download_state(ctx, (version, state) =>
        {
            using (Performance.Measure("Calling OnUpdateDownloadState, v: {0}, state: {1}", version, state))
            {
                OnUpdateDownloadStatus.OnNext(new UpdateStatus(Version.Parse(version), (DownloadStatus)state));
            }
        });

        toggl_on_project_colors(ctx, (colors, count) =>
        {
            using (Performance.Measure("Calling OnProjectColors, count: {0}", count))
            {
                OnDisplayProjectColors(colors, count);
            }
        });

        toggl_on_countries(ctx, (first) =>
        {
            using (Performance.Measure("Calling OnCountries"))
            {
                OnDisplayCountries(convertToCountryList(first));
            }
        });

        toggl_on_promotion(ctx, id =>
        {
            using (Performance.Measure("Calling OnDisplayPromotino, id: {0}", id))
            {
                OnDisplayPromotion(id);
            }
        });

        toggl_on_pomodoro(ctx, (title, text) =>
        {
            using (Performance.Measure("Calling OnDisplayPomodoro"))
            {
                OnDisplayPomodoro(title, text);
            }
        });
        toggl_on_pomodoro_break(ctx, (title, text) =>
        {
            using (Performance.Measure("Calling OnDisplayPomodoroBreak"))
            {
                OnDisplayPomodoroBreak(title, text);
            }
        });
        toggl_on_message(ctx, (title, text, button, url) =>
        {
            using (Performance.Measure("Calling OnDisplayInAppNotification"))
            {
                OnDisplayInAppNotification(title, text, button, url);
            }
        });
        toggl_on_timeline(ctx, (open, date, first, firstTimeEntry, startDay, endDay) =>
        {
            using (Performance.Measure("Calling OnTimeline"))
            {
                TimelineSelectedDate.OnNext(DateTimeFromUnix(startDay));
                var chunks = convertToTimelineChunkList(first);
                var timeEntries = convertToTimeEntryList(firstTimeEntry);
                TimelineChunks.OnNext(chunks);
                TimelineTimeEntries.OnNext(timeEntries);
            }
        });
        toggl_on_timeline_ui_enabled(ctx, isEnabled =>
        {
            using (Performance.Measure("Calling OnDisplayTimelineUI"))
            {
                OnDisplayTimelineUI(isEnabled);
            }
        });
    }

    #endregion

    #region internal ui events

    public delegate void ManualSync();

    public static event ManualSync OnManualSync = delegate { };

    public delegate void UserTimeEntryStart();

    public static event UserTimeEntryStart OnUserTimeEntryStart = delegate { };

    #endregion

    #region startup

    private static void parseCommandlineParams()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Contains("script") && args[i].Contains("path"))
            {
                ScriptPath = args[i + 1];
                Console.WriteLine("ScriptPath = {0}", ScriptPath);
            }
            else if (args[i].Contains("log") && args[i].Contains("path"))
            {
                LogPath = args[i + 1];
                Console.WriteLine("LogPath = {0}", LogPath);
            }
            else if (args[i].Contains("db") && args[i].Contains("path"))
            {
                DatabasePath = args[i + 1];
                Console.WriteLine("DatabasePath = {0}", DatabasePath);
            }
            else if (args[i].Contains("environment"))
            {
                Env = args[i + 1];
                Console.WriteLine("Environment = {0}", Env);
            }
            else if (args[i].Contains("--staging"))
            {
                toggl_set_staging_override(true);
            }
        }
    }

    public static string LogAndDbFileName(string environment)
    {
        return environment == "production" ? "toggldesktop" : $"toggldesktop_{environment}";
    }

    public static void InitialiseLog()
    {
        string path = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "TogglDesktop");
        Directory.CreateDirectory(path);

        if (null == LogPath)
        {
            LogPath = Path.Combine(path, $"{LogAndDbFileName(Env)}.log");
        }
        toggl_set_log_path(LogPath);
        toggl_set_log_level("debug");
    }

    public static bool StartUI(string version)
    {
        parseCommandlineParams();

        ctx = toggl_context_init("windows_native_app", version);

        toggl_set_environment(ctx, Env);

        string cacert_path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "cacert.pem");
        toggl_set_cacert_path(ctx, cacert_path);

        string err = toggl_check_view_struct_size(
            Marshal.SizeOf(new TogglTimeEntryView()),
            Marshal.SizeOf(new TogglAutocompleteView()),
            Marshal.SizeOf(new TogglGenericView()),
            Marshal.SizeOf(new TogglSettingsView()),
            Marshal.SizeOf(new TogglAutotrackerRuleView()));
        if (null != err) {
            throw new System.InvalidOperationException(err);
        }

        listenToLibEvents();

        if (!UpdateService.IsUpdateCheckEnabled)
        {
            toggl_disable_update_check(ctx);
        }

        string path = Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "TogglDesktop");

        // Configure log, db path
        Directory.CreateDirectory(path);

        if (null == DatabasePath)
        {
            DatabasePath = Path.Combine(path, $"{LogAndDbFileName(Env)}.db");
        }

        if (!toggl_set_db_path(ctx, DatabasePath))
        {
            throw new System.Exception("Failed to initialize database at " + DatabasePath);
        }

        toggl_set_update_path(ctx, UpdatesPath);

        // Start pumping UI events
        return toggl_ui_start(ctx);
    }

    public static bool IsUpdateCheckDisabled()
    {
#if TOGGL_ALLOW_UPDATE_CHECK
        return Utils.GetIsUpdateCheckDisabledFromRegistry();
#else
        return true;
#endif
    }
    #endregion

    #region converting data

    #region high level

    private static List<TogglGenericView> convertToViewItemList(IntPtr first)
    {
        return marshalList<TogglGenericView>(first, n => n.Next);
    }

    private static List<TogglAutocompleteView> convertToAutocompleteList(IntPtr first)
    {
        return marshalList<TogglAutocompleteView>(first, n => n.Next);
    }

    private static List<TogglTimeEntryView> convertToTimeEntryList(IntPtr first)
    {
        return marshalList<TogglTimeEntryView>(first, n => n.Next);
    }

    private static List<TogglAutotrackerRuleView> convertToAutotrackerEntryList(IntPtr first)
    {
        return marshalList<TogglAutotrackerRuleView>(first, n => n.Next);
    }

    private static List<TogglCountryView> convertToCountryList(IntPtr first)
    {
        return marshalList<TogglCountryView>(first, n => n.Next);
    }

    private static List<TimelineChunkView> convertToTimelineChunkList(IntPtr first)
    {
        return marshalList<TimelineChunkView, TogglTimelineChunkView>(first,
            n => n.Next, t => new TimelineChunkView(t));
    }

    #endregion

    #region low level

    private static List<T> marshalList<T>(IntPtr node, Func<T, IntPtr> getNext)
    where T : struct
    {
        var list = new List<T>();

        while (node != IntPtr.Zero)
        {
            var t = (T)Marshal.PtrToStructure(node, typeof(T));
            list.Add(t);
            node = getNext(t);
        }

        return list;
    }

    private static T marshalStruct<T>(IntPtr pointer)
    where T : struct
    {
        return (T)Marshal.PtrToStructure(pointer, typeof(T));
    }

    private static List<T> marshalList<T, K>(IntPtr node, Func<K, IntPtr> getNext, Func<K, T> cast)
    {
        var list = new List<T>();

        while (node != IntPtr.Zero)
        {
            var t = (K)Marshal.PtrToStructure(node, typeof(K));
            list.Add(cast(t));
            node = getNext(t);
        }

        return list;
    }
    #endregion

    #endregion

    #region getting/setting global shortcuts

    public static void SetKeyStart(string key)
    {
        toggl_set_key_start(ctx, key);
    }
    public static Key GetKeyStart()
    {
        var keyCode = toggl_get_key_start(ctx);
        return getKey(keyCode);
    }
    public static void SetKeyShow(string key)
    {
        toggl_set_key_show(ctx, key);
    }
    public static Key GetKeyShow()
    {
        var keyCode = toggl_get_key_show(ctx);
        return getKey(keyCode);
    }
    public static void SetKeyModifierShow(ModifierKeys mods)
    {
        toggl_set_key_modifier_show(ctx, mods.ToString());
    }
    public static ModifierKeys GetKeyModifierShow()
    {
        var s = toggl_get_key_modifier_show(ctx);
        if (string.IsNullOrWhiteSpace(s) || !Enum.TryParse(s, true, out ModifierKeys modifierKeys))
            return ModifierKeys.None;
        return modifierKeys;
    }
    public static void SetKeyModifierStart(ModifierKeys mods)
    {
        toggl_set_key_modifier_start(ctx, mods.ToString());
    }
    public static ModifierKeys GetKeyModifierStart()
    {
        var s = toggl_get_key_modifier_start(ctx);
        if (string.IsNullOrWhiteSpace(s) || !Enum.TryParse(s, true, out ModifierKeys modifierKeys))
            return ModifierKeys.None;
        return modifierKeys;
    }

    private static Key getKey(string keyCode)
    {
        if (string.IsNullOrEmpty(keyCode) || !Enum.TryParse(keyCode, out Key key))
        {
            return Key.None;
        }

        return key;
    }

    #endregion

    #region getting/setting window settings

    public static void SetWindowMaximized(bool maximised)
    {
        toggl_set_window_maximized(ctx, maximised);
    }
    public static bool GetWindowMaximized()
    {
        return toggl_get_window_maximized(ctx);
    }
    public static void SetWindowMinimized(bool minimized)
    {
        toggl_set_window_minimized(ctx, minimized);
    }
    public static bool GetWindowMinimized()
    {
        return toggl_get_window_minimized(ctx);
    }

    public static void SetEditViewWidth(long width)
    {
        toggl_set_window_edit_size_width(ctx, width);
    }
    public static long GetEditViewWidth()
    {
        return toggl_get_window_edit_size_width(ctx);
    }

    public static bool SetWindowSettings(
        long x, long y, long h, long w)
    {
        return toggl_set_window_settings(ctx, x, y, h, w);
    }


    public static bool WindowSettings(
        ref long x, ref long y, ref long h, ref long w)
    {
        return toggl_window_settings(ctx, ref x, ref y, ref h, ref w);
    }

    #endregion

    #region various

    public static void PrepareShutdown()
    {
        mainWindow.PrepareShutdown(true);
    }

    public static void SetManualMode(bool manualMode)
    {
        toggl_set_settings_manual_mode(ctx, manualMode);
    }

    public static DateTime DateTimeFromUnix(UInt64 unix_seconds)
    {
        return UnixEpoch.AddSeconds(unix_seconds).ToLocalTime().DateTime;
    }

    public static Int64 UnixFromDateTime(DateTime value)
    {
        var span = new DateTimeOffset(value) - UnixEpoch;
        return (Int64)span.TotalSeconds;
    }

    public static void NewError(string errmsg, bool user_error)
    {
        OnError(errmsg, user_error);
    }

    public static void OpenInBrowser(string url)
    {
        OnURL(url);
    }

    public static void ShowErrorAndNotify(string errmsg, Exception ex)
    {
        NewError(errmsg, true);
        BugsnagService.NotifyBugsnag(ex);
    }

    public static bool AskToDeleteEntry(string guid)
    {
        var result = MessageBox.Show(mainWindow, "Deleted time entries cannot be restored.", "Delete time entry?",
                                     MessageBoxButton.OKCancel, "Delete entry");

        if (result == MessageBoxResult.OK)
        {
            return DeleteTimeEntry(guid);
        }
        return false;
    }

    public static void RegisterMainWindow(MainWindow window)
    {
        if (mainWindow != null)
            throw new Exception("Can only register main window once!");

        mainWindow = window;
    }

    #endregion

    #region timeline ui

    public static bool SetActiveTab(byte tab)
    {
        return toggl_set_settings_active_tab(ctx, tab);
    }

    public static byte GetActiveTab()
    {
        return toggl_get_active_tab(ctx);
    }

    public static bool SetTimelineRecordingEnabled(bool recordTimeline)
    {
        return toggl_timeline_toggle_recording(ctx, recordTimeline);
    }

    public static void SetViewTimelineDay(long timestamp)
    {
        toggl_view_timeline_set_day(ctx, timestamp);
    }

    public static void ViewTimelineCurrentDay()
    {
        toggl_view_timeline_current_day(ctx);
    }

    public static void ViewTimelinePreviousDay()
    {
        toggl_view_timeline_prev_day(ctx);
    }

    public static void ViewTimelineNextDay()
    {
        toggl_view_timeline_next_day(ctx);
    }

    public static void ViewTimelineData()
    {
        toggl_view_timeline_data(ctx);
    }

    public static string CreateEmptyTimeEntry(ulong started, ulong ended)
    {
        return toggl_create_empty_time_entry(ctx, started, ended);
    }
    #endregion


    public static void ToggleEntriesGroup(string groupName)
    {
        toggl_toggle_entries_group(ctx, groupName);
    }

    public static byte DaysOfWeekIntoByte(bool sun, bool mon, bool tue, bool wed, bool thu, bool fri, bool sat)
    {
        return toggl_days_of_week_into_uint8(sun, mon, tue, wed, thu, fri, sat);
    }
}
}
